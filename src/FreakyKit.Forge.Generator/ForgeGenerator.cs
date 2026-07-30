using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FreakyKit.Forge.Diagnostics;
using FreakyKit.Forge.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace FreakyKit.Forge.Generator;

/// <summary>
/// Incremental source generator for FreakyKit.Forge.
/// Generates partial method implementations for all valid forge methods.
/// Stops generation entirely on any Error diagnostic — no partial output on errors.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ForgeGenerator : IIncrementalGenerator
{
    // Depth limits for flattening and expression nesting
    private const int FlatteningDiagnosticThreshold = 3;
    private const int ExpressionNestingWarningThreshold = 4;
    private const int ExpressionNestingErrorThreshold = 7;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Pipeline: find static partial classes decorated with [Forge]
        var forgeClasses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "FreakyKit.Forge.ForgeAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax cds &&
                    cds.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                    cds.Modifiers.Any(SyntaxKind.PartialKeyword),
                transform: static (ctx, ct) => ExtractForgeClass(ctx, ct))
            .Where(static model => model is not null)
            .Select(static (model, _) => model!);

        context.RegisterSourceOutput(forgeClasses, static (spc, model) =>
        {
            // Emit all diagnostics first
            foreach (var diag in model.Diagnostics)
                spc.ReportDiagnostic(diag);

            // Only generate source if there are no errors
            if (model.HasErrors) return;
            if (model.ClassModel is null) return;

            var source = GenerateSource(model.ClassModel, spc.CancellationToken);
            spc.AddSource($"{model.ClassModel.FullyQualifiedName.Replace('.', '_').Replace('<', '_').Replace('>', '_')}.Forge.g.cs", SourceText.From(source, Encoding.UTF8));
        });

        // Validation pipeline: Find classes with [ForgeUses] but missing [Forge]
        var forgeUsesWithoutForge = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "FreakyKit.Forge.ForgeUsesAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => DetectForgeUsesMissingForge(ctx, ct))
            .Where(static diag => diag is not null)
            .Select(static (diag, _) => diag!);

        context.RegisterSourceOutput(forgeUsesWithoutForge, static (spc, diag) =>
        {
            spc.ReportDiagnostic(diag);
        });

        // Validation pipeline: Find methods with [ForgeMethod] but missing [Forge] on containing class
        var forgeMethodWithoutForge = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "FreakyKit.Forge.ForgeMethodAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, ct) => DetectForgeMethodWithoutForge(ctx, ct))
            .Where(static diag => diag is not null)
            .Select(static (diag, _) => diag!);

        context.RegisterSourceOutput(forgeMethodWithoutForge, static (spc, diag) =>
        {
            spc.ReportDiagnostic(diag);
        });

        // Validation pipeline: Find methods with [ForgeConverter] but missing [Forge] on containing class
        var forgeConverterWithoutForge = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "FreakyKit.Forge.ForgeConverterAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, ct) => DetectForgeConverterWithoutForge(ctx, ct))
            .Where(static diag => diag is not null)
            .Select(static (diag, _) => diag!);

        context.RegisterSourceOutput(forgeConverterWithoutForge, static (spc, diag) =>
        {
            spc.ReportDiagnostic(diag);
        });

        // Validation pipeline: Find members with [ForgeMap] on non-destination types
        var forgeMapOnSourceMember = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "FreakyKit.Forge.ForgeMapAttribute",
                predicate: static (node, _) => node is PropertyDeclarationSyntax or FieldDeclarationSyntax,
                transform: static (ctx, ct) => DetectForgeMapOnSourceMember(ctx, ct))
            .Where(static diag => diag is not null)
            .Select(static (diag, _) => diag!);

        context.RegisterSourceOutput(forgeMapOnSourceMember, static (spc, diag) =>
        {
            spc.ReportDiagnostic(diag);
        });

        // Validation pipeline: Find members with [ForgeIgnore] on non-destination types
        var forgeIgnoreOnSourceMember = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "FreakyKit.Forge.ForgeIgnoreAttribute",
                predicate: static (node, _) => node is PropertyDeclarationSyntax or FieldDeclarationSyntax,
                transform: static (ctx, ct) => DetectForgeIgnoreOnSourceMember(ctx, ct))
            .Where(static diag => diag is not null)
            .Select(static (diag, _) => diag!);

        context.RegisterSourceOutput(forgeIgnoreOnSourceMember, static (spc, diag) =>
        {
            spc.ReportDiagnostic(diag);
        });
    }

    // ─── Utilities ────────────────────────────────────────────────────────────

    /// <summary>
    /// Safely get a line number from a method's location. Handles null locations and exceptions.
    /// </summary>
    private static int GetSafeLineNumber(IMethodSymbol? method)
    {
        if (method?.Locations.Length == 0) return 0;
        try
        {
            var location = method?.Locations[0];
            if (location == null) return 0;
            return location.GetLineSpan().StartLinePosition.Line + 1;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Safely get the first location from a method. Returns null if unavailable.
    /// </summary>
    private static Location? GetSafeLocation(IMethodSymbol? method)
    {
        return method?.Locations.FirstOrDefault();
    }

    /// <summary>
    /// Build a collection expression with the specified fallback value, avoiding string replacement issues.
    /// Reconstructs the expression from components rather than post-processing with Replace.
    /// </summary>
    private static string BuildCollectionExpressionWithFallback(
        string sourceAccessor,
        string? elementForgeMethod,
        string materializer,
        string fallback)
    {
        string selectPart;
        if (elementForgeMethod != null)
            selectPart = $"{sourceAccessor}.Select(x => {elementForgeMethod}(x))";
        else
            selectPart = sourceAccessor;

        return $"{sourceAccessor} != null ? {selectPart}{materializer} : {fallback}";
    }

    /// <summary>
    /// Validate NullFallback preconditions and emit diagnostics for conflicts.
    /// Returns true if a fatal error (FKF315) was found and assignment should be skipped.
    /// </summary>
    private static bool ValidateNullFallbackConflicts(
        bool memberIgnoreIfNull,
        int nullFallbackInt,
        string destMemberName,
        ITypeSymbol srcType,
        IMethodSymbol method,
        List<Diagnostic> diagnostics)
    {
        // FKF315: Error if both IgnoreIfNull and NullFallback are set
        if (memberIgnoreIfNull && nullFallbackInt != 0)
        {
            diagnostics.Add(Diagnostic.Create(
                ForgeDiagnostics.IgnoreIfNullAndNullFallbackConflict,
                GetSafeLocation(method),
                destMemberName));
            return true;
        }

        // FKF314: Warning if NullFallback on value type
        if (nullFallbackInt != 0 && !srcType.IsReferenceType)
        {
            diagnostics.Add(Diagnostic.Create(
                ForgeDiagnostics.NullFallbackOnValueType,
                GetSafeLocation(method),
                destMemberName));
        }

        return false;
    }

    // ─── Extraction ───────────────────────────────────────────────────────────

    private static ForgeClassResult ExtractForgeClass(
        GeneratorAttributeSyntaxContext ctx,
        System.Threading.CancellationToken ct)
    {
        var type = ctx.TargetSymbol as INamedTypeSymbol;
        if (type is null) return ForgeClassResult.Empty;

        var diagnostics = new List<Diagnostic>();

        var forgeClassAttr = type.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeAttribute");

        if (forgeClassAttr is null) return ForgeClassResult.Empty;

        var mode = GetForgeMode(forgeClassAttr);
        var includePrivate = GetBoolNamedArg(forgeClassAttr, "ShouldIncludePrivate");
        // Default is true; only false if explicitly set
        var generateExtensionMethods = forgeClassAttr.NamedArguments
            .FirstOrDefault(a => a.Key == "GenerateExtensionMethods").Value.Value is bool val
            ? val
            : true;

        // Collect all candidate static partial methods
        var allMethods = type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic && m.IsPartialDefinition)
            .ToList();

        var forgeMethods = new List<IMethodSymbol>();
        var overloadCandidates = new Dictionary<string, List<IMethodSymbol>>();

        foreach (var method in allMethods)
        {
            bool hasForgeAttr = HasForgeAttribute(method);
            bool isCandidate = IsForgeMethodShape(method);

            if (!isCandidate && !hasForgeAttr) continue;

            // Explicit mode: ignore non-attributed candidates (analyzer handles FKF002)
            if (mode == GeneratorForgeMode.Explicit && !hasForgeAttr) continue;

            // Skip hook/helper methods that match forge shapes but are not intended to be forges
            // (OnBeforeXxx and OnAfterXxx void methods are hooks called by forge methods, not forge methods themselves)
            // Non-void methods like OnAfterSummary are eligible forge methods
            if ((method.Name.StartsWith("OnBefore") || method.Name.StartsWith("OnAfter")) && method.ReturnsVoid)
                continue;

            // Private filter (analyzer handles FKF010)
            if (method.DeclaredAccessibility == Accessibility.Private && !includePrivate) continue;

            // Shape filter
            if (!isCandidate) continue;

            // Body check (analyzer handles FKF020)
            if (HasImplementationBody(method, ct))
            {
                diagnostics.Add(Diagnostic.Create(
                    ForgeDiagnostics.ForgeMethodDeclaresBody,
                    GetSafeLocation(method),
                    method.Name));
                continue;
            }

            if (!overloadCandidates.TryGetValue(method.Name, out var bucket))
            {
                bucket = new List<IMethodSymbol>();
                overloadCandidates[method.Name] = bucket;
            }
            bucket.Add(method);
        }

        // FKF030: overload detection
        foreach (var kvp in overloadCandidates)
        {
            if (kvp.Value.Count > 1)
            {
                foreach (var m in kvp.Value)
                {
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.ForgeMethodNameOverloaded,
                        GetSafeLocation(m),
                        kvp.Key,
                        type.Name));
                }
            }
            else
            {
                forgeMethods.Add(kvp.Value[0]);
            }
        }

        // If any errors so far, stop
        bool hasErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
        if (hasErrors)
            return new ForgeClassResult(null, diagnostics, hasErrors: true);

        // Extract and validate [ForgeUses] early so we can use it during method extraction
        var (includedForgeClasses, forgeUsesDiags) = ExtractAndValidateForgeUses(type, ctx.SemanticModel.Compilation, diagnostics);
        diagnostics.AddRange(forgeUsesDiags);
        var forgeUsesErrors = forgeUsesDiags.Any(d => d.Severity == DiagnosticSeverity.Error);
        if (forgeUsesErrors)
            return new ForgeClassResult(null, diagnostics, hasErrors: true);

        // Extract each forge method model
        var methodModels = new List<ForgeMethodModel>();
        foreach (var method in forgeMethods)
        {
            var (methodModel, methodDiags) = ExtractForgeMethod(method, type, ctx.SemanticModel.Compilation, ct, includedForgeClasses);
            diagnostics.AddRange(methodDiags);

            if (methodDiags.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                hasErrors = true;
                // Collect all method errors before stopping
            }
            else if (methodModel != null)
            {
                methodModels.Add(methodModel);
            }
        }

        if (hasErrors)
            return new ForgeClassResult(null, diagnostics, hasErrors: true);

        // Detect circular nested forge before expression inlining
        DetectCircularNestedForge(methodModels, forgeMethods, diagnostics);
        var circularErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error && d.Id == "FKF301");
        if (circularErrors)
            return new ForgeClassResult(null, diagnostics, hasErrors: true);

        // Phase 5 (expression projections): resolve nested-forge inlining.
        // Walks each GenerateExpression method's assignments and replaces nested-forge markers
        // with inlined expression bodies. Detects cycles (FKF507) and emits info for deep nesting (FKF508).
        ResolveExpressionInlining(methodModels, diagnostics, ct);
        var inliningErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error && d.Id == "FKF507");
        if (inliningErrors)
            return new ForgeClassResult(null, diagnostics, hasErrors: true);

        var ns = type.ContainingNamespace?.IsGlobalNamespace == true
            ? string.Empty
            : type.ContainingNamespace?.ToDisplayString() ?? string.Empty;

        var classAccessibility = AccessibilityToString(type.DeclaredAccessibility);

        // Build containing type chain (outermost first) for nested classes
        var containingTypes = BuildContainingTypeChain(type);

        var classModel = new ForgeClassModel(
            @namespace: ns,
            className: type.Name,
            accessibility: classAccessibility,
            fullyQualifiedName: type.ToDisplayString(),
            hasErrors: false,
            methods: methodModels,
            containingTypes: containingTypes,
            generateExtensionMethods: generateExtensionMethods,
            includedForgeClasses: includedForgeClasses);

        return new ForgeClassResult(classModel, diagnostics, hasErrors: false);
    }

    /// <summary>
    /// Extracts and validates a single forge method, building its model for code generation.
    /// Pipeline: shape detection → member discovery → constructor selection → assignment extraction
    /// → expression property generation → validation. Emits diagnostics for shape violations, member
    /// conflicts, and type mismatches. Returns null model on critical errors; diagnostics are always populated.
    /// </summary>
    private static (ForgeMethodModel? Model, List<Diagnostic> Diagnostics) ExtractForgeMethod(
        IMethodSymbol method,
        INamedTypeSymbol forgeClass,
        Microsoft.CodeAnalysis.Compilation compilation,
        System.Threading.CancellationToken ct,
        IReadOnlyList<string>? includedForgeClasses = null)
    {
        var diagnostics = new List<Diagnostic>();

        // Detect update vs create shape
        bool isUpdate = IsUpdateMethodShape(method);
        var methodKind = isUpdate ? ForgeMethodKind.Update : ForgeMethodKind.Create;

        var srcParamName = method.Parameters[0].Name;

        // ── Dictionary projection detection ───────────────────────────────────
        if (!isUpdate)
        {
            var rawSrc = method.Parameters[0].Type;
            var rawDest = method.ReturnType;
            if (GetDictionaryKeyValueTypes(rawSrc, out var srcDictKey, out var srcDictVal) &&
                GetDictionaryKeyValueTypes(rawDest, out var destDictKey, out var destDictVal))
            {
                return ExtractDictionaryProjectMethod(method, forgeClass, rawSrc, rawDest,
                    srcDictKey!, srcDictVal!, destDictKey!, destDictVal!, srcParamName, diagnostics, compilation, includedForgeClasses);
            }
        }

        // ── Collection projection detection (before INamedTypeSymbol cast) ────
        // Handles both INamedTypeSymbol (List<T>) and IArrayTypeSymbol (T[]) source/dest.
        if (!isUpdate)
        {
            var rawSrc = method.Parameters[0].Type;
            var rawDest = method.ReturnType;
            var srcElemType = GetCollectionElementType(rawSrc);
            var destElemType = GetCollectionElementType(rawDest);
            if (srcElemType != null && destElemType != null)
            {
                return ExtractCollectionProjectMethod(method, forgeClass, rawSrc, rawDest,
                    srcElemType, destElemType, srcParamName, diagnostics, compilation, includedForgeClasses);
            }
        }

        // ── Dictionary mapping detection (dict ↔ object) ────────────────────────
        if (!isUpdate)
        {
            var rawSrc = method.Parameters[0].Type;
            var rawDest = method.ReturnType;
            bool srcIsDict = GetDictionaryKeyValueTypes(rawSrc, out var srcDictKey, out var srcDictVal);
            bool destIsDict = GetDictionaryKeyValueTypes(rawDest, out var destDictKey, out var destDictVal);

            // Dict → Object
            if (srcIsDict && !destIsDict && rawDest is INamedTypeSymbol destObj)
            {
                if (srcDictKey?.ToDisplayString() != "string")
                {
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.DictionaryKeyTypeNotString,
                        GetSafeLocation(method),
                        rawSrc.ToDisplayString(), srcDictKey?.ToDisplayString() ?? "unknown"));
                    return (null, diagnostics);
                }
                return ExtractDictionaryToObjectMethod(method, forgeClass, rawSrc, destObj,
                    srcDictVal!, srcParamName, diagnostics, compilation, includedForgeClasses);
            }

            // Object → Dict
            if (!srcIsDict && destIsDict && rawSrc is INamedTypeSymbol srcObj)
            {
                if (destDictKey?.ToDisplayString() != "string")
                {
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.DictionaryKeyTypeNotString,
                        GetSafeLocation(method),
                        rawDest.ToDisplayString(), destDictKey?.ToDisplayString() ?? "unknown"));
                    return (null, diagnostics);
                }
                return ExtractObjectToDictionaryMethod(method, forgeClass, srcObj, rawDest,
                    destDictVal!, srcParamName, diagnostics, compilation, includedForgeClasses);
            }
        }

        INamedTypeSymbol? sourceType;
        INamedTypeSymbol? destType;
        string destParameterName;

        if (isUpdate)
        {
            sourceType = method.Parameters[0].Type as INamedTypeSymbol;
            destType = method.Parameters[1].Type as INamedTypeSymbol;
            destParameterName = method.Parameters[1].Name;

            // FKF040: info about update mode
            diagnostics.Add(Diagnostic.Create(
                ForgeDiagnostics.UpdateModeActivated,
                GetSafeLocation(method),
                method.Name));
        }
        else
        {
            sourceType = method.Parameters[0].Type as INamedTypeSymbol;
            destType = method.ReturnType as INamedTypeSymbol;
            destParameterName = "";
        }

        if (sourceType is null || destType is null)
            return (null, diagnostics);

        var forgeAttr = GetForgeAttribute(method);
        bool includeFields = forgeAttr != null && GetBoolNamedArg(forgeAttr, "ShouldIncludeFields");
        bool allowNested = forgeAttr != null && GetBoolNamedArg(forgeAttr, "AllowNestedForging");
        bool allowFlattening = forgeAttr != null && GetBoolNamedArg(forgeAttr, "AllowFlattening");
        bool methodIgnoreIfNull = forgeAttr != null && GetForgePolicyAsBoolean(forgeAttr, "IgnoreIfNull");
        bool generateExpression = forgeAttr != null && GetBoolNamedArg(forgeAttr, "GenerateExpression");
        bool methodShareReference = forgeAttr != null && GetForgePolicyAsBoolean(forgeAttr, "ShareReference");
        int enumMappingStrategy = GetEnumMappingStrategy(forgeAttr);

        // FKF504: GenerateExpression is incompatible with update method shape
        if (generateExpression && isUpdate)
        {
            diagnostics.Add(Diagnostic.Create(
                ForgeDiagnostics.ExpressionIncompatibleWithUpdate,
                GetSafeLocation(method),
                method.Name));
            generateExpression = false;
        }

        if (includeFields)
        {
            diagnostics.Add(Diagnostic.Create(
                ForgeDiagnostics.FieldsEnabled,
                GetSafeLocation(method),
                method.Name));
        }

        // Collect source members
        var forgeAssembly = forgeClass.ContainingAssembly;
        var sourceMembers = CollectMembers(sourceType, includeFields, method, diagnostics, isSourceSide: true, forgeAssembly: forgeAssembly);

        // Collect dest members (no FKF400 for dest — only source triggers it)
        var destMembers = CollectMembers(destType, includeFields, null, null, isSourceSide: false, forgeAssembly: forgeAssembly);

        // Determine construction (skip for update methods)
        ConstructionModel construction;
        if (isUpdate)
        {
            construction = new ConstructionModel(ConstructionKind.None, new List<ConstructorArgModel>());

            // FKF041: check that dest type has at least one settable member
            bool hasSettable = false;
            foreach (var kvp in destMembers)
            {
                if (!IsReadOnlyMember(destType, kvp.Key))
                {
                    hasSettable = true;
                    break;
                }
            }
            if (!hasSettable)
            {
                diagnostics.Add(Diagnostic.Create(
                    ForgeDiagnostics.UpdateDestinationNoSettableMembers,
                    GetSafeLocation(method),
                    method.Name,
                    destType.Name));
                return (null, diagnostics);
            }
        }
        else
        {
            var (ctorConstruction, ctorDiags) = DetermineConstruction(destType, sourceMembers, method, srcParamName, sourceType);
            construction = ctorConstruction;
            diagnostics.AddRange(ctorDiags);

            if (ctorDiags.Any(d => d.Severity == DiagnosticSeverity.Error))
                return (null, diagnostics);
        }

        // Determine member assignments
        var assignments = new List<MemberAssignmentModel>();
        var matchedSourceKeys = new HashSet<string>();
        bool hasTypeMismatch = false;

        // Members used in constructor args should not be reassigned
        var constructorUsedKeys = new HashSet<string>(
            construction.ConstructorArgs.Select(a => a.ParameterName.ToLowerInvariant()));

        foreach (var destKvp in destMembers)
        {
            var key = destKvp.Key;
            var destMember = destKvp.Value;

            // Skip if used in constructor
            if (constructorUsedKeys.Contains(key) &&
                construction.Kind == ConstructionKind.Parameterized)
                continue;

            // Skip read-only properties unless set via constructor
            if (IsReadOnlyMember(destType, key))
                continue;

            // Check if this is an init-only property (needs object initializer syntax)
            // Init-only properties cannot be assigned in update methods
            bool initOnly = IsInitOnlyMember(destType, key);
            if (initOnly && isUpdate)
                continue;

            if (!sourceMembers.TryGetValue(key, out var srcMember))
            {
                // Try flattening: dest "AddressCity" → source "Address.City"
                bool depthLimitExceeded = false;
                bool ambiguousFlattening = false;
                if (allowFlattening && TryResolveFlattenedMapping(sourceType, key, destMember.Type, srcParamName, out var flattenExpr, out var flatteningDepth, out var flatteningPath, out depthLimitExceeded, out ambiguousFlattening))
                {
                    // FKF530: Ambiguous flattening detected
                    if (ambiguousFlattening)
                    {
                        // Note: The message needs the longest matching prefix and the full key
                        diagnostics.Add(Diagnostic.Create(
                            ForgeDiagnostics.AmbiguousFlatteningAutoResolved,
                            GetSafeLocation(method),
                            destMember.Name,
                            key,
                            flatteningPath?.Split('.').Last() ?? "unknown"));
                    }

                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.FlattenedMapping,
                        GetSafeLocation(method),
                        destMember.Name,
                        sourceType.Name,
                        flattenExpr.Replace($"{srcParamName}.", "")));

                    // FKF531: Emit info diagnostic if flattening depth >= threshold
                    if (flatteningDepth >= FlatteningDiagnosticThreshold)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            ForgeDiagnostics.DeepFlatteningDetected,
                            GetSafeLocation(method),
                            destMember.Name,
                            flatteningDepth + 1,
                            flatteningPath ?? flattenExpr.Replace($"{srcParamName}.", "")));
                    }

                    // If the flattened expression has nullable intermediates (contains `?.`)
                    // but the destination type is non-nullable, wrap with null coalescing
                    string finalFlattenExpr = flattenExpr;
                    if (flattenExpr.Contains("?.") && destMember.Type.NullableAnnotation == NullableAnnotation.NotAnnotated)
                    {
                        // Coalesce to default value for the destination type
                        finalFlattenExpr = $"({flattenExpr}) ?? default!";
                    }

                    // Expression-tree form: convert null-conditional (`source.Address?.City`) to a
                    // ternary (`source.Address == null ? null : source.Address.City`) because `?.`
                    // isn't allowed in expression-tree lambdas. For non-nullable destinations, coalesce to default.
                    // Value-type intermediates have no `?.` and can be emitted as-is.
                    // Process all `?.` operators, not just the first one.
                    string exprFlatten = flattenExpr;
                    bool isNonNullableDest = destMember.Type.NullableAnnotation == NullableAnnotation.NotAnnotated;

                    while (exprFlatten.Contains("?."))
                    {
                        var qIdx = exprFlatten.IndexOf("?.");
                        var prefix = exprFlatten.Substring(0, qIdx);
                        var suffix = exprFlatten.Substring(qIdx + 2);

                        // Only add parentheses if the prefix contains a ternary (from previous iteration)
                        bool needsParens = prefix.Contains("?");
                        string prefixExpr = needsParens ? $"({prefix})" : prefix;

                        if (isNonNullableDest)
                        {
                            exprFlatten = $"{prefixExpr} == null ? default! : {prefixExpr}.{suffix} ?? default!";
                        }
                        else
                        {
                            exprFlatten = $"{prefixExpr} == null ? null : {prefixExpr}.{suffix}";
                        }
                    }

                    assignments.Add(new MemberAssignmentModel(
                        destMemberName: destMember.Name,
                        sourceExpression: finalFlattenExpr,
                        isInitOnly: initOnly,
                        expressionAssignment: exprFlatten));
                    continue;
                }
                else if (allowFlattening && depthLimitExceeded)
                {
                    // FKF532: Flattening depth limit exceeded
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.FlatteningDepthLimitExceeded,
                        GetSafeLocation(method),
                        destMember.Name));
                }

                // FKF100: handled by analyzer — generator just skips
                continue;
            }

            matchedSourceKeys.Add(key);

            // Determine IgnoreIfNull: per-member overrides method-level.
            // Precedence: dest-side explicit > src-side explicit > method-level > default (false)
            // srcSymbolForNull and destSymbolForNull may be null (unmatched members), but are explicitly null-checked below.
            var srcSymbolForNull = sourceType.GetMembers().FirstOrDefault(m => m.Name == srcMember.Name);
            var destSymbolForNull = destType.GetMembers().FirstOrDefault(m => m.Name == destMember.Name);
            bool? memberIgnoreIfNullExplicit = destSymbolForNull != null ? GetForgeIgnoreIfNull(destSymbolForNull) : null;
            if (memberIgnoreIfNullExplicit == null && srcSymbolForNull != null)
                memberIgnoreIfNullExplicit = GetForgeIgnoreIfNull(srcSymbolForNull);
            bool memberIgnoreIfNull = memberIgnoreIfNullExplicit ?? methodIgnoreIfNull;
            string? nullCheckExpr = memberIgnoreIfNull ? $"{srcParamName}.{srcMember.Name}" : null;

            // Determine IgnoreIfDefault and Condition
            bool memberIgnoreIfDefault = (srcSymbolForNull != null && GetForgeIgnoreIfDefault(srcSymbolForNull))
                || (destSymbolForNull != null && GetForgeIgnoreIfDefault(destSymbolForNull));
            string? conditionMethodName = GetForgeConditionMethod(srcSymbolForNull) ?? GetForgeConditionMethod(destSymbolForNull);

            // Validate condition method if specified
            if (conditionMethodName != null)
            {
                var conditionMethod = ResolveConditionMethod(
                    forgeClass, conditionMethodName, compilation, includedForgeClasses,
                    sourceType, out var qualifiedConditionMethodName, out var shadowedBy);

                if (shadowedBy != null)
                {
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.ShadowedConditionMethod,
                        GetSafeLocation(method),
                        destMember.Name,
                        conditionMethodName,
                        qualifiedConditionMethodName ?? conditionMethodName,
                        shadowedBy));
                }

                if (conditionMethod == null)
                {
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.ConditionMethodNotFound,
                        GetSafeLocation(method),
                        destMember.Name,
                        conditionMethodName));
                    conditionMethodName = null;
                }
                else if (!conditionMethod.IsStatic || conditionMethod.Parameters.Length != 1 || conditionMethod.Parameters[0].Type.ToDisplayString() != sourceType.ToDisplayString() || conditionMethod.ReturnType.ToDisplayString() != "bool")
                {
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.InvalidConditionMethodSignature,
                        GetSafeLocation(method),
                        destMember.Name,
                        conditionMethodName));
                    conditionMethodName = null;
                }
                else if (conditionMethod.DeclaredAccessibility != Accessibility.Public && conditionMethod.DeclaredAccessibility != Accessibility.Internal)
                {
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.ConditionMethodNotAccessible,
                        GetSafeLocation(method),
                        destMember.Name,
                        conditionMethodName));
                    conditionMethodName = null;
                }
                else
                {
                    conditionMethodName = qualifiedConditionMethodName;
                }
            }

            // Init-only (and required) members can only be set inside the constructor's object
            // initializer — there's no way to wrap that in a runtime `if`. Rather than silently
            // dropping the member (IgnoreIfNull) or silently ignoring the guard (IgnoreIfDefault/
            // Condition), emit FKF316 and fall back to a plain, unconditional assignment.
            if (initOnly)
            {
                if (memberIgnoreIfNull)
                {
                    diagnostics.Add(Diagnostic.Create(ForgeDiagnostics.GuardOnInitOnlyMember, GetSafeLocation(method), destMember.Name, "IgnoreIfNull"));
                    memberIgnoreIfNull = false;
                    nullCheckExpr = null;
                }
                if (memberIgnoreIfDefault)
                {
                    diagnostics.Add(Diagnostic.Create(ForgeDiagnostics.GuardOnInitOnlyMember, GetSafeLocation(method), destMember.Name, "IgnoreIfDefault"));
                    memberIgnoreIfDefault = false;
                }
                if (conditionMethodName != null)
                {
                    diagnostics.Add(Diagnostic.Create(ForgeDiagnostics.GuardOnInitOnlyMember, GetSafeLocation(method), destMember.Name, "Condition"));
                    conditionMethodName = null;
                }
            }

            // IgnoreIfDefault and Condition have no expression-tree equivalent (same as IgnoreIfNull):
            // an EF Core .Select() projection can't express a runtime guard around a member assignment.
            // Members using either are excluded from the generated expression and FKF506 is emitted so
            // the divergence between the imperative method and the expression property is never silent.
            bool excludeFromExpression = memberIgnoreIfNull || memberIgnoreIfDefault || conditionMethodName != null;
            if (generateExpression && excludeFromExpression)
            {
                var reason = conditionMethodName != null
                    ? "Condition has no equivalent in expression trees"
                    : memberIgnoreIfDefault
                        ? "IgnoreIfDefault has no equivalent in expression trees"
                        : "IgnoreIfNull has no equivalent in expression trees";
                diagnostics.Add(Diagnostic.Create(
                    ForgeDiagnostics.ExpressionMemberExcluded,
                    GetSafeLocation(method),
                    destMember.Name,
                    reason));
            }

            if (srcMember.Type.ToDisplayString() == destMember.Type.ToDisplayString())
            {
                // Exact type match. By default same-type members are direct reference assignments,
                // EXCEPT mutable same-type collections which deep-copy by default (so DTO and source
                // own independent collection instances). The ShareReference flag flips that back.

                // Resolve ShareReference precedence: dest-side > source-side > method-level > default(false).
                // When both source and dest are explicitly set with conflicting values, the dest wins
                // and FKF313 is emitted.
                var srcShareRef = GetForgeMapShareReference(srcSymbolForNull);
                var destShareRef = GetForgeMapShareReference(destSymbolForNull);

                if (srcShareRef.HasValue && destShareRef.HasValue && srcShareRef.Value != destShareRef.Value)
                {
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.ShareReferenceConflict,
                        GetSafeLocation(method),
                        destMember.Name,
                        srcShareRef.Value ? "true" : "false",
                        destShareRef.Value ? "true" : "false"));
                }

                // Effective ShareReference: per-member (dest > src) wins; otherwise method-level.
                bool effectiveShareReference = destShareRef ?? srcShareRef ?? methodShareReference;

                bool isMutableCollection = IsMutableSameTypeCollection(srcMember.Type);
                bool sourceIsRefType = srcMember.Type.IsReferenceType;

                string srcAccessor = $"{srcParamName}.{srcMember.Name}";
                string sourceExpr;
                string? exprAssign;

                if (isMutableCollection && !effectiveShareReference && !memberIgnoreIfNull)
                {
                    // Default for mutable collections: deep-copy via constructor (or .ToArray() for arrays)
                    var copyExpr = BuildSameTypeCollectionCopyExpression(srcMember.Type, srcAccessor);
                    sourceExpr = sourceIsRefType
                        ? $"{srcAccessor} != null ? {copyExpr} : null"
                        : copyExpr;
                    // Expression mode: the same expression is translatable, unless excluded (Condition/IgnoreIfDefault)
                    exprAssign = excludeFromExpression ? null : sourceExpr;
                }
                else
                {
                    // Reference share (or value type, or IgnoreIfNull) — direct assignment
                    sourceExpr = srcAccessor;

                    // FKF311: warn when a mutable same-type collection is reference-shared
                    if (isMutableCollection && effectiveShareReference)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            ForgeDiagnostics.SameTypeCollectionShared,
                            GetSafeLocation(method),
                            destMember.Name));
                    }
                    // FKF312: warn when a same-type mutable reference type (custom class, not collection)
                    // is reference-shared. Always emitted regardless of flag — there's no way to deep-copy
                    // custom classes via Forge today (that's what AllowNestedForging + distinct DTO types is for).
                    else if (!isMutableCollection
                        && sourceIsRefType
                        && srcMember.Type.SpecialType != SpecialType.System_String
                        && srcMember.Type.TypeKind != TypeKind.Enum
                        && !IsImmutableArrayType(srcMember.Type)
                        && !IsImmutableListType(srcMember.Type)
                        && !IsImmutableHashSetType(srcMember.Type))
                    {
                        diagnostics.Add(Diagnostic.Create(
                            ForgeDiagnostics.SameTypeReferenceShared,
                            GetSafeLocation(method),
                            destMember.Name,
                            srcMember.Type.ToDisplayString()));
                    }

                    exprAssign = excludeFromExpression ? null : srcAccessor;
                }

                assignments.Add(new MemberAssignmentModel(
                    destMemberName: destMember.Name,
                    sourceExpression: sourceExpr,
                    ignoreIfNull: memberIgnoreIfNull,
                    nullCheckExpression: nullCheckExpr,
                    isInitOnly: initOnly,
                    expressionAssignment: exprAssign,
                    ignoreIfDefault: memberIgnoreIfDefault,
                    conditionMethodName: conditionMethodName,
                    sourceMemberName: srcMember.Name,
                    sourceMemberType: srcMember.Type.ToDisplayString()));
            }
            else if (TryResolveNullableMapping(srcMember.Type, destMember.Type, out var nullableKind))
            {
                // Nullable-compatible types
                var paramName = srcParamName;
                var srcSymbol = srcSymbolForNull;
                var destSymbol = destSymbolForNull;
                var defaultValue = (srcSymbol != null ? GetForgeDefaultValue(srcSymbol) : null)
                    ?? (destSymbol != null ? GetForgeDefaultValue(destSymbol) : null);

                string sourceExpr;
                string expressionExpr;
                if (nullableKind == NullableConversionKind.UnwrapValue && defaultValue != null)
                {
                    // Use ?? defaultValue for safe fallback (works in both imperative and expression trees)
                    var literal = FormatLiteral(defaultValue);
                    sourceExpr = $"{paramName}.{srcMember.Name} ?? {literal}";
                    expressionExpr = $"{paramName}.{srcMember.Name} ?? {literal}";
                }
                else if (nullableKind == NullableConversionKind.UnwrapValue)
                {
                    sourceExpr = $"{paramName}.{srcMember.Name}.Value";
                    // Expression-tree mode: prefer GetValueOrDefault() over .Value to avoid
                    // InvalidOperationException at runtime if the expression is .Compile()'d
                    // and invoked against a null source. EF Core translates both identically.
                    expressionExpr = $"{paramName}.{srcMember.Name}.GetValueOrDefault()";
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.NullableValueTypeMapping,
                        GetSafeLocation(method),
                        key,
                        srcMember.Type.ToDisplayString(),
                        destMember.Type.ToDisplayString()));
                }
                else
                {
                    sourceExpr = $"{paramName}.{srcMember.Name}";
                    expressionExpr = $"{paramName}.{srcMember.Name}";
                }

                assignments.Add(new MemberAssignmentModel(
                    destMemberName: destMember.Name,
                    sourceExpression: sourceExpr,
                    ignoreIfNull: memberIgnoreIfNull,
                    nullCheckExpression: nullCheckExpr,
                    isInitOnly: initOnly,
                    expressionAssignment: excludeFromExpression ? null : expressionExpr,
                    ignoreIfDefault: memberIgnoreIfDefault,
                    conditionMethodName: conditionMethodName,
                    sourceMemberName: srcMember.Name,
                    sourceMemberType: srcMember.Type.ToDisplayString()));
            }
            else if (srcMember.Type.TypeKind == TypeKind.Enum && destMember.Type.TypeKind == TypeKind.Enum)
            {
                // Enum-to-enum mapping
                var paramName = srcParamName;

                if (enumMappingStrategy == 1) // ByName
                {
                    // Imperative: switch expression. Expression-tree mode: chained ternary
                    // (switch expressions are not allowed in expression-tree lambdas).
                    var srcEnumType = (INamedTypeSymbol)srcMember.Type;
                    var destEnumType = (INamedTypeSymbol)destMember.Type;
                    var destMemberNames = new HashSet<string>(
                        destEnumType.GetMembers().OfType<IFieldSymbol>()
                            .Where(f => f.HasConstantValue)
                            .Select(f => f.Name));

                    var srcEnumMembers = srcEnumType.GetMembers().OfType<IFieldSymbol>()
                        .Where(f => f.HasConstantValue)
                        .ToList();

                    var switchArms = new List<string>();
                    var ternaryArms = new List<string>();
                    var srcAccess = $"{paramName}.{srcMember.Name}";

                    foreach (var srcField in srcEnumMembers)
                    {
                        if (destMemberNames.Contains(srcField.Name))
                        {
                            switchArms.Add($"{srcEnumType.Name}.{srcField.Name} => {destEnumType.Name}.{srcField.Name}");
                            ternaryArms.Add($"{srcAccess} == {srcEnumType.Name}.{srcField.Name} ? {destEnumType.Name}.{srcField.Name}");
                        }
                        else
                        {
                            // FKF212: source enum member missing in destination
                            diagnostics.Add(Diagnostic.Create(
                                ForgeDiagnostics.EnumMemberMissing,
                                GetSafeLocation(method),
                                srcField.Name,
                                srcEnumType.Name,
                                destEnumType.Name));
                            switchArms.Add($"{srcEnumType.Name}.{srcField.Name} => throw new InvalidOperationException(\"No mapping for {srcEnumType.Name}.{srcField.Name}\")");
                            // Expression-tree mode can't throw; fall through to default arm instead.
                            // (The imperative method preserves the throw-on-missing semantics.)
                        }
                    }

                    switchArms.Add($"_ => throw new InvalidOperationException($\"Unknown enum value: {{{srcAccess}}}\")");
                    var switchExpr = $"{srcAccess} switch {{ {string.Join(", ", switchArms)} }}";

                    // Expression-tree form: nested ternary with default fallback.
                    // Requires at least one mapped arm; if every source member is missing,
                    // we still emit `default(Dest)` so the expression is well-formed.
                    string ternaryExpr;
                    if (ternaryArms.Count == 0)
                    {
                        ternaryExpr = $"default({destEnumType.Name})";
                    }
                    else
                    {
                        ternaryExpr = string.Join(" : ", ternaryArms) + $" : default({destEnumType.Name})";
                    }

                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.EnumNameMapping,
                        GetSafeLocation(method),
                        key,
                        srcMember.Type.ToDisplayString(),
                        destMember.Type.ToDisplayString()));

                    assignments.Add(new MemberAssignmentModel(
                        destMemberName: destMember.Name,
                        sourceExpression: switchExpr,
                        ignoreIfNull: memberIgnoreIfNull,
                        nullCheckExpression: nullCheckExpr,
                        isInitOnly: initOnly,
                        expressionAssignment: excludeFromExpression ? null : ternaryExpr,
                        ignoreIfDefault: memberIgnoreIfDefault,
                        conditionMethodName: conditionMethodName,
                        sourceMemberName: srcMember.Name,
                        sourceMemberType: srcMember.Type.ToDisplayString()));
                }
                else // Cast (default)
                {
                    var destEnumType = (INamedTypeSymbol)destMember.Type;
                    var castExpr = $"({destEnumType.Name}){paramName}.{srcMember.Name}";

                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.EnumCastMapping,
                        GetSafeLocation(method),
                        key,
                        srcMember.Type.ToDisplayString(),
                        destMember.Type.ToDisplayString()));

                    assignments.Add(new MemberAssignmentModel(
                        destMemberName: destMember.Name,
                        sourceExpression: castExpr,
                        ignoreIfNull: memberIgnoreIfNull,
                        nullCheckExpression: nullCheckExpr,
                        isInitOnly: initOnly,
                        expressionAssignment: excludeFromExpression ? null : castExpr,
                        ignoreIfDefault: memberIgnoreIfDefault,
                        conditionMethodName: conditionMethodName,
                        sourceMemberName: srcMember.Name,
                        sourceMemberType: srcMember.Type.ToDisplayString()));
                }
            }
            else if (TryResolveEnumStringMapping(srcMember.Type, destMember.Type, srcParamName, srcMember.Name, srcSymbolForNull, destSymbolForNull, out var enumStringExpr))
            {
                diagnostics.Add(Diagnostic.Create(
                    ForgeDiagnostics.EnumStringMapping,
                    GetSafeLocation(method),
                    key,
                    srcMember.Type.ToDisplayString(),
                    destMember.Type.ToDisplayString()));

                assignments.Add(new MemberAssignmentModel(
                    destMemberName: destMember.Name,
                    sourceExpression: enumStringExpr,
                    ignoreIfNull: memberIgnoreIfNull,
                    nullCheckExpression: nullCheckExpr,
                    isInitOnly: initOnly,
                    expressionAssignment: excludeFromExpression ? null : enumStringExpr,
                    ignoreIfDefault: memberIgnoreIfDefault,
                    conditionMethodName: conditionMethodName,
                    sourceMemberName: srcMember.Name,
                    sourceMemberType: srcMember.Type.ToDisplayString()));
            }
            else if (TryResolveDictionaryMapping(srcMember.Type, destMember.Type, forgeClass, allowNested, srcParamName, srcMember.Name, out var dictExpr, compilation, includedForgeClasses, diagnostics))
            {
                assignments.Add(new MemberAssignmentModel(
                    destMemberName: destMember.Name,
                    sourceExpression: dictExpr,
                    ignoreIfNull: memberIgnoreIfNull,
                    nullCheckExpression: nullCheckExpr,
                    isInitOnly: initOnly,
                    ignoreIfDefault: memberIgnoreIfDefault,
                    conditionMethodName: conditionMethodName,
                    sourceMemberName: srcMember.Name,
                    sourceMemberType: srcMember.Type.ToDisplayString()));
            }
            else if (TryResolveCollectionMapping(srcMember.Type, destMember.Type, forgeClass, allowNested, srcParamName, srcMember.Name, out var collectionExpr, out var collectionInfo, compilation, includedForgeClasses, diagnostics))
            {
                diagnostics.Add(Diagnostic.Create(
                    ForgeDiagnostics.CollectionMapping,
                    GetSafeLocation(method),
                    key,
                    srcMember.Type.ToDisplayString(),
                    destMember.Type.ToDisplayString()));

                var nullFallbackInt = GetForgeMapNullFallback(destSymbolForNull);

                if (ValidateNullFallbackConflicts(memberIgnoreIfNull, nullFallbackInt, destMember.Name, srcMember.Type, method, diagnostics))
                {
                    hasTypeMismatch = true;
                    continue;
                }

                // Apply NullFallback to collection if source is reference type
                // Generate the correct fallback from the start instead of string replacement
                if (srcMember.Type.IsReferenceType && nullFallbackInt == 1 && collectionInfo != null) // DefaultConstruct
                {
                    // Generate typed fallback matching the destination collection shape
                    var destElem = GetCollectionElementType(destMember.Type);
                    var elemName = destElem?.Name ?? "object";
                    string typedFallback = collectionInfo.DestinationSuffix switch
                    {
                        ".ToArray()" => $"Array.Empty<{elemName}>()",
                        ".ToHashSet()" => $"new HashSet<{elemName}>()",
                        ".ToImmutableArray()" => $"ImmutableArray<{elemName}>.Empty",
                        ".ToImmutableList()" => $"ImmutableList<{elemName}>.Empty",
                        ".ToImmutableHashSet()" => $"ImmutableHashSet<{elemName}>.Empty",
                        ".ToList().AsReadOnly()" => $"new List<{elemName}>().AsReadOnly()",
                        _ => $"new List<{elemName}>()"  // default to List<T> if suffix is unrecognized
                    };
                    collectionExpr = BuildCollectionExpressionWithFallback(
                        collectionInfo.SourceAccessor!,
                        collectionInfo.ElementForgeMethod,
                        collectionInfo.DestinationSuffix,
                        fallback: typedFallback);
                }

                // Expression-mode translatability rules:
                //  - Materializer must be .ToList() or .ToArray() (others not translated by EF)
                //  - IgnoreIfNull semantics have no expression-tree equivalent
                string? exprAssign = null;
                bool needsInlining = false;
                if (collectionInfo != null && collectionInfo.ExpressionMaterializer != null && !excludeFromExpression)
                {
                    if (collectionInfo.SameElementType)
                    {
                        // Same element type: the imperative expression is already translatable as-is.
                        exprAssign = collectionExpr;
                    }
                    else
                    {
                        // Different element type: defer to post-pass to inline the element body.
                        needsInlining = true;
                    }
                }
                // IgnoreIfNull/IgnoreIfDefault/Condition already reported centrally above.
                // Only report the materializer-specific reason here.
                else if (generateExpression && !excludeFromExpression)
                {
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.ExpressionMemberExcluded,
                        GetSafeLocation(method),
                        destMember.Name,
                        "non-translatable collection materializer"));
                }

                assignments.Add(new MemberAssignmentModel(
                    destMemberName: destMember.Name,
                    sourceExpression: collectionExpr,
                    ignoreIfNull: memberIgnoreIfNull,
                    nullCheckExpression: nullCheckExpr,
                    isInitOnly: initOnly,
                    expressionAssignment: exprAssign,
                    collectionElementForgeMethod: needsInlining ? collectionInfo?.ElementForgeMethod : null,
                    collectionSourceAccessor: needsInlining ? collectionInfo?.SourceAccessor : null,
                    collectionMaterializer: needsInlining ? collectionInfo?.ExpressionMaterializer : null,
                    collectionSourceIsRefType: needsInlining && collectionInfo != null && collectionInfo.SourceIsRefType,
                    nestedForgeNullFallback: nullFallbackInt,
                    ignoreIfDefault: memberIgnoreIfDefault,
                    conditionMethodName: conditionMethodName,
                    sourceMemberName: srcMember.Name,
                    sourceMemberType: srcMember.Type.ToDisplayString()));
            }
            else if (FindConverterMethod(forgeClass, srcMember.Type, destMember.Type, out var converterName, compilation, includedForgeClasses))
            {
                // Type converter found
                diagnostics.Add(Diagnostic.Create(
                    ForgeDiagnostics.ConverterUsed,
                    GetSafeLocation(method),
                    key, converterName!,
                    srcMember.Type.ToDisplayString(),
                    destMember.Type.ToDisplayString()));

                // Custom converter calls are not translatable by EF: user-defined static methods
                // have no SQL equivalent. Exclude from the expression property and emit FKF506.
                // IgnoreIfNull/IgnoreIfDefault/Condition already reported centrally above.
                if (generateExpression && !excludeFromExpression)
                {
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.ExpressionMemberExcluded,
                        GetSafeLocation(method),
                        destMember.Name,
                        $"custom converter '{converterName}' is not translatable to SQL"));
                }

                assignments.Add(new MemberAssignmentModel(
                    destMemberName: destMember.Name,
                    sourceExpression: $"{converterName}({srcParamName}.{srcMember.Name})",
                    ignoreIfNull: memberIgnoreIfNull,
                    nullCheckExpression: nullCheckExpr,
                    isInitOnly: initOnly,
                    ignoreIfDefault: memberIgnoreIfDefault,
                    conditionMethodName: conditionMethodName,
                    sourceMemberName: srcMember.Name,
                    sourceMemberType: srcMember.Type.ToDisplayString()));
            }
            else if (TryImplicitConversion(compilation, srcMember.Type, destMember.Type, out var isLossy))
            {
                // Implicit conversion available
                if (isLossy)
                {
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.LossyImplicitConversion,
                        GetSafeLocation(method),
                        key,
                        srcMember.Type.ToDisplayString(),
                        destMember.Type.ToDisplayString()));
                }

                // Direct assignment with implicit conversion
                var srcAccessor = $"{srcParamName}.{srcMember.Name}";
                assignments.Add(new MemberAssignmentModel(
                    destMemberName: destMember.Name,
                    sourceExpression: srcAccessor,
                    ignoreIfNull: memberIgnoreIfNull,
                    nullCheckExpression: nullCheckExpr,
                    isInitOnly: initOnly,
                    expressionAssignment: excludeFromExpression ? null : srcAccessor,
                    ignoreIfDefault: memberIgnoreIfDefault,
                    conditionMethodName: conditionMethodName,
                    sourceMemberName: srcMember.Name,
                    sourceMemberType: srcMember.Type.ToDisplayString()));
            }
            else
            {
                bool nestedForgeExists = FindNestedForgeMethod(forgeClass, srcMember.Type, destMember.Type, out var nestedMethodName, compilation, includedForgeClasses, diagnostics, destMember.Name);

                if (nestedForgeExists && allowNested && nestedMethodName != null)
                {
                    var srcAccess = $"{srcParamName}.{srcMember.Name}";
                    var nullFallbackInt = GetForgeMapNullFallback(destSymbolForNull);

                    if (ValidateNullFallbackConflicts(memberIgnoreIfNull, nullFallbackInt, destMember.Name, srcMember.Type, method, diagnostics))
                    {
                        hasTypeMismatch = true;
                        continue;
                    }

                    string nestedExpr;
                    // Null-safe nested access: if source member is a reference type, guard against null
                    if (srcMember.Type.IsReferenceType)
                    {
                        string fallbackExpr;
                        if (nullFallbackInt == 1) // DefaultConstruct
                        {
                            fallbackExpr = $"new {destMember.Type.Name}()";
                        }
                        else // 0 = Null (default)
                        {
                            fallbackExpr = "null";
                        }
                        nestedExpr = $"{srcAccess} != null ? {nestedMethodName}({srcAccess}) : {fallbackExpr}";
                    }
                    else
                    {
                        nestedExpr = $"{nestedMethodName}({srcAccess})";
                    }

                    // For expression mode, defer the actual inlining to codegen time when the
                    // nested method's emittable assignments are available. The model carries the
                    // metadata; GenerateExpressionProperty resolves it.
                    assignments.Add(new MemberAssignmentModel(
                        destMemberName: destMember.Name,
                        sourceExpression: nestedExpr,
                        ignoreIfNull: memberIgnoreIfNull,
                        nullCheckExpression: nullCheckExpr,
                        isInitOnly: initOnly,
                        nestedForgeMethodName: nestedMethodName,
                        nestedForgeSourceAccessor: srcAccess,
                        nestedForgeSourceIsRefType: srcMember.Type.IsReferenceType,
                        nestedForgeNullFallback: nullFallbackInt,
                        ignoreIfDefault: memberIgnoreIfDefault,
                        conditionMethodName: conditionMethodName,
                        sourceMemberName: srcMember.Name,
                        sourceMemberType: srcMember.Type.ToDisplayString()));
                }
                else if (!nestedForgeExists)
                {
                    // FKF200: incompatible types, no forge conversion available — block generation
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.IncompatibleMemberTypes,
                        GetSafeLocation(method),
                        key,
                        srcMember.Type.ToDisplayString(),
                        destMember.Type.ToDisplayString()));
                    hasTypeMismatch = true;
                }
                // else: nestedForgeExists but !allowNested → FKF300 reported by analyzer, generator skips
            }
        }

        if (hasTypeMismatch)
            return (null, diagnostics);

        // Detect before/after hooks
        string? beforeHookName = null;
        string? afterHookName = null;
        var beforeName = $"OnBefore{method.Name}";
        var afterName = $"OnAfter{method.Name}";

        foreach (var m in forgeClass.GetMembers().OfType<IMethodSymbol>())
        {
            if (m.IsStatic && m.IsPartialDefinition && m.ReturnsVoid && m.Name == beforeName &&
                m.Parameters.Length == 1 &&
                m.Parameters[0].RefKind == RefKind.None &&
                m.Parameters[0].Type.ToDisplayString() == sourceType.ToDisplayString())
            {
                beforeHookName = beforeName;
                diagnostics.Add(Diagnostic.Create(
                    ForgeDiagnostics.BeforeHookDetected,
                    GetSafeLocation(method),
                    beforeName, method.Name));
            }
            if (m.IsStatic && m.IsPartialDefinition && m.ReturnsVoid && m.Name == afterName &&
                m.Parameters.Length == 2 &&
                m.Parameters[0].RefKind == RefKind.None &&
                m.Parameters[1].RefKind == RefKind.None &&
                m.Parameters[0].Type.ToDisplayString() == sourceType.ToDisplayString() &&
                m.Parameters[1].Type.ToDisplayString() == destType.ToDisplayString())
            {
                afterHookName = afterName;
                diagnostics.Add(Diagnostic.Create(
                    ForgeDiagnostics.AfterHookDetected,
                    GetSafeLocation(method),
                    afterName, method.Name));
            }
        }

        // FKF505: hooks are not invoked from the generated expression property
        if (generateExpression && (beforeHookName != null || afterHookName != null))
        {
            diagnostics.Add(Diagnostic.Create(
                ForgeDiagnostics.ExpressionIgnoresHooks,
                GetSafeLocation(method),
                method.Name));
        }

        var accessibility = AccessibilityToString(method.DeclaredAccessibility);
        var sourceLocation = GetSafeLocation(method);
        var lineSpan = sourceLocation?.GetLineSpan();

        var methodModel = new ForgeMethodModel(
            methodName: method.Name,
            accessibility: accessibility,
            sourceTypeFqn: sourceType.ToDisplayString(),
            sourceTypeShortName: sourceType.Name,
            sourceParameterName: srcParamName,
            destTypeFqn: destType.ToDisplayString(),
            destTypeShortName: destType.Name,
            construction: construction,
            assignments: assignments,
            nestedMethods: [],
            methodKind: methodKind,
            destParameterName: destParameterName,
            beforeHookName: beforeHookName,
            afterHookName: afterHookName,
            sourceFilePath: lineSpan?.Path,
            sourceLineNumber: (lineSpan?.StartLinePosition.Line ?? -1) + 1,
            generateExpression: generateExpression);

        return (methodModel, diagnostics);
    }

    private static (ForgeMethodModel? Model, List<Diagnostic> Diagnostics) ExtractCollectionProjectMethod(
        IMethodSymbol method,
        INamedTypeSymbol forgeClass,
        ITypeSymbol sourceCollType,
        ITypeSymbol destCollType,
        ITypeSymbol srcElemType,
        ITypeSymbol destElemType,
        string srcParamName,
        List<Diagnostic> diagnostics,
        Microsoft.CodeAnalysis.Compilation? compilation = null,
        IReadOnlyList<string>? includedForgeClasses = null)
    {
        var accessibility = AccessibilityToString(method.DeclaredAccessibility);
        var srcShort = BuildShortTypeName(sourceCollType);
        var destShort = BuildShortTypeName(destCollType);

        string elementTransform;
        var srcElemDisplay = srcElemType.ToDisplayString();
        var destElemDisplay = destElemType.ToDisplayString();

        if (srcElemDisplay == destElemDisplay)
        {
            // Same element type — identity projection
            elementTransform = "x => x";
        }
        else
        {
            // Try to find a forge method that converts srcElem → destElem
            if (FindNestedForgeMethod(forgeClass, srcElemType, destElemType, out var nestedName, compilation, includedForgeClasses, diagnostics, method.Name) && nestedName != null)
            {
                elementTransform = $"x => {nestedName}(x)";
            }
            // Try to find a [ForgeConverter] method
            else if (FindConverterMethod(forgeClass, srcElemType, destElemType, out var converterName, compilation, includedForgeClasses) && converterName != null)
            {
                elementTransform = $"x => {converterName}(x)";
            }
            else
            {
                diagnostics.Add(Diagnostic.Create(
                    ForgeDiagnostics.IncompatibleMemberTypes,
                    GetSafeLocation(method),
                    method.Name,
                    srcElemDisplay,
                    destElemDisplay));
                return (null, diagnostics);
            }
        }

        // Determine materialization suffix for the destination collection type
        string suffix;
        if (IsImmutableArrayType(destCollType))
            suffix = ".ToImmutableArray()";
        else if (IsImmutableListType(destCollType))
            suffix = ".ToImmutableList()";
        else if (IsImmutableHashSetType(destCollType))
            suffix = ".ToImmutableHashSet()";
        else if (IsReadOnlyCollectionType(destCollType))
            suffix = ".ToList().AsReadOnly()";
        else if (destCollType is IArrayTypeSymbol || destCollType.OriginalDefinition.ToDisplayString() == "T[]"
                 || (destCollType.Name == "Array"))
            suffix = ".ToArray()";
        else if (IsHashSetType(destCollType))
            suffix = ".ToHashSet()";
        else
            suffix = ".ToList()";

        string projExpr = elementTransform == "x => x"
            ? $"{srcParamName}{suffix}"  // direct materialisation (no transform needed)
            : $"{srcParamName}.Select({elementTransform}){suffix}";

        // Null-safe guard when source collection is a reference type
        bool srcIsRefType = sourceCollType.IsReferenceType;
        string fullExpr;
        if (srcIsRefType)
        {
            // ImmutableArray<T> is a struct — use default for null case
            if (IsImmutableArrayType(destCollType))
                fullExpr = $"{srcParamName} != null ? {projExpr} : default";
            else
                fullExpr = $"{srcParamName} != null ? {projExpr} : null";
        }
        else
        {
            fullExpr = projExpr;
        }

        var location = GetSafeLocation(method);
        var lineNumber = GetSafeLineNumber(method);

        var model = new ForgeMethodModel(
            methodName: method.Name,
            accessibility: accessibility,
            sourceTypeFqn: sourceCollType.ToDisplayString(),
            sourceTypeShortName: srcShort,
            sourceParameterName: srcParamName,
            destTypeFqn: destCollType.ToDisplayString(),
            destTypeShortName: destShort,
            construction: new ConstructionModel(ConstructionKind.Parameterless, new System.Collections.Generic.List<ConstructorArgModel>()),
            assignments: new System.Collections.Generic.List<MemberAssignmentModel>(),
            nestedMethods: new System.Collections.Generic.List<ForgeMethodModel>(),
            methodKind: ForgeMethodKind.CollectionProject,
            sourceFilePath: location?.SourceTree?.FilePath,
            sourceLineNumber: lineNumber,
            collectionProjectExpression: fullExpr);

        return (model, diagnostics);
    }

    private static (ForgeMethodModel? Model, List<Diagnostic> Diagnostics) ExtractDictionaryProjectMethod(
        IMethodSymbol method,
        INamedTypeSymbol forgeClass,
        ITypeSymbol sourceDictType,
        ITypeSymbol destDictType,
        ITypeSymbol srcKeyType,
        ITypeSymbol srcValType,
        ITypeSymbol destKeyType,
        ITypeSymbol destValType,
        string srcParamName,
        List<Diagnostic> diagnostics,
        Microsoft.CodeAnalysis.Compilation? compilation = null,
        IReadOnlyList<string>? includedForgeClasses = null)
    {
        var accessibility = AccessibilityToString(method.DeclaredAccessibility);
        var srcShort = BuildShortTypeName(sourceDictType);
        var destShort = BuildShortTypeName(destDictType);
        var concreteDictShort = GetConcreteDictShortName(destDictType, destKeyType, destValType);

        if (srcKeyType.ToDisplayString() != destKeyType.ToDisplayString())
        {
            diagnostics.Add(Diagnostic.Create(
                ForgeDiagnostics.IncompatibleMemberTypes,
                GetSafeLocation(method),
                method.Name,
                srcKeyType.ToDisplayString(),
                destKeyType.ToDisplayString()));
            return (null, diagnostics);
        }

        string valueTransform;
        var srcValDisplay = srcValType.ToDisplayString();
        var destValDisplay = destValType.ToDisplayString();

        if (srcValDisplay == destValDisplay)
        {
            valueTransform = "";
        }
        else if (FindNestedForgeMethod(forgeClass, srcValType, destValType, out var nestedName, compilation, includedForgeClasses, diagnostics, method.Name) && nestedName != null)
        {
            valueTransform = $"{nestedName}(__kvp.Value)";
        }
        else if (FindConverterMethod(forgeClass, srcValType, destValType, out var converterName, compilation, includedForgeClasses) && converterName != null)
        {
            valueTransform = $"{converterName}(__kvp.Value)";
        }
        else
        {
            diagnostics.Add(Diagnostic.Create(
                ForgeDiagnostics.IncompatibleMemberTypes,
                GetSafeLocation(method),
                method.Name,
                srcValDisplay,
                destValDisplay));
            return (null, diagnostics);
        }

        var location = GetSafeLocation(method);
        var lineNumber = GetSafeLineNumber(method);

        var model = new ForgeMethodModel(
            methodName: method.Name,
            accessibility: accessibility,
            sourceTypeFqn: sourceDictType.ToDisplayString(),
            sourceTypeShortName: srcShort,
            sourceParameterName: srcParamName,
            destTypeFqn: destDictType.ToDisplayString(),
            destTypeShortName: destShort,
            construction: new ConstructionModel(ConstructionKind.Parameterless, new System.Collections.Generic.List<ConstructorArgModel>()),
            assignments: new System.Collections.Generic.List<MemberAssignmentModel>(),
            nestedMethods: new System.Collections.Generic.List<ForgeMethodModel>(),
            methodKind: ForgeMethodKind.DictionaryProject,
            sourceFilePath: location?.SourceTree?.FilePath,
            sourceLineNumber: lineNumber,
            collectionProjectExpression: valueTransform,
            concreteDictInstantiationName: concreteDictShort);

        return (model, diagnostics);
    }

    private static (ForgeMethodModel? Model, List<Diagnostic> Diagnostics) ExtractDictionaryToObjectMethod(
        IMethodSymbol method,
        INamedTypeSymbol forgeClass,
        ITypeSymbol sourceDictType,
        INamedTypeSymbol destType,
        ITypeSymbol srcDictVal,
        string srcParamName,
        List<Diagnostic> diagnostics,
        Microsoft.CodeAnalysis.Compilation? compilation = null,
        IReadOnlyList<string>? includedForgeClasses = null)
    {
        var accessibility = AccessibilityToString(method.DeclaredAccessibility);
        var srcShort = BuildShortTypeName(sourceDictType);
        var destShort = destType.Name;

        // Get destination members (properties/fields)
        var destMembers = CollectMembers(destType, includeFields: false, null, null, isSourceSide: false, forgeAssembly: forgeClass.ContainingAssembly);

        // Get dictionary mapping policies
        var dictAttr = GetForgeDictionaryAttribute(method);
        var (keyCasing, missingKey, nullValue) = GetDictionaryPolicies(dictAttr);

        // FKF702: Check if ReturnNull policy is used on non-nullable members
        if (missingKey == 3) // ReturnNull
        {
            foreach (var kvp in destMembers)
            {
                var memberType = kvp.Value.Type;
                // Check if type is nullable: either reference type or value type with nullable annotation
                var isNullable = memberType.IsReferenceType || memberType.NullableAnnotation == NullableAnnotation.Annotated;
                if (!isNullable)
                {
                    // Non-nullable type (both value types and reference types)
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.ReturnNullOnNonNullableType,
                        GetSafeLocation(method),
                        kvp.Value.Name,
                        memberType.ToDisplayString()));
                }
            }
        }

        // FKF701: Check if dictionary value type is supported (primitives, object, enums)
        if (!IsSupportedDictionaryValueType(srcDictVal))
        {
            diagnostics.Add(Diagnostic.Create(
                ForgeDiagnostics.UnsupportedDictionaryValueType,
                GetSafeLocation(method),
                srcDictVal.ToDisplayString()));
        }

        var location = GetSafeLocation(method);
        var lineNumber = GetSafeLineNumber(method);

        // For now, store dest member info in the assignments list even though we'll generate code differently
        // This allows GenerateMethodBody to access the destination member info
        var assignments = new System.Collections.Generic.List<MemberAssignmentModel>();
        foreach (var kvp in destMembers)
        {
            var propertyName = kvp.Value.Name; // Use actual property name, not lowercased key
            var propertyType = kvp.Value.Type.ToDisplayString(); // Store type for casting during code generation
            assignments.Add(new MemberAssignmentModel(
                destMemberName: propertyName,
                sourceExpression: $"dict[\"{propertyName}\"]", // Placeholder; actual code generation in GenerateMethodBody
                isInitOnly: IsInitOnlyMember(destType, propertyName),
                sourceMemberType: propertyType)); // Store type info for casting
        }

        var dictValueTypeString = srcDictVal?.ToDisplayString();
        var model = new ForgeMethodModel(
            methodName: method.Name,
            accessibility: accessibility,
            sourceTypeFqn: sourceDictType.ToDisplayString(),
            sourceTypeShortName: srcShort,
            sourceParameterName: srcParamName,
            destTypeFqn: destType.ToDisplayString(),
            destTypeShortName: destShort,
            construction: new ConstructionModel(ConstructionKind.Parameterless, new System.Collections.Generic.List<ConstructorArgModel>()),
            assignments: assignments,
            nestedMethods: new System.Collections.Generic.List<ForgeMethodModel>(),
            methodKind: ForgeMethodKind.DictionaryToObject,
            sourceFilePath: location?.SourceTree?.FilePath,
            sourceLineNumber: lineNumber,
            dictKeyCasingPolicy: keyCasing,
            dictMissingKeyPolicy: missingKey,
            dictNullValuePolicy: nullValue,
            dictValueType: dictValueTypeString);

        return (model, diagnostics);
    }

    private static (ForgeMethodModel? Model, List<Diagnostic> Diagnostics) ExtractObjectToDictionaryMethod(
        IMethodSymbol method,
        INamedTypeSymbol forgeClass,
        INamedTypeSymbol sourceType,
        ITypeSymbol destDictType,
        ITypeSymbol destDictVal,
        string srcParamName,
        List<Diagnostic> diagnostics,
        Microsoft.CodeAnalysis.Compilation? compilation = null,
        IReadOnlyList<string>? includedForgeClasses = null)
    {
        var accessibility = AccessibilityToString(method.DeclaredAccessibility);
        var srcShort = sourceType.Name;
        var destShort = BuildShortTypeName(destDictType);

        // Get source members (properties/fields)
        var sourceMembers = CollectMembers(sourceType, includeFields: false, method, null, isSourceSide: true, forgeAssembly: forgeClass.ContainingAssembly);

        // Get dictionary mapping policies
        var dictAttr = GetForgeDictionaryAttribute(method);
        var (keyCasing, missingKey, nullValue) = GetDictionaryPolicies(dictAttr);

        var location = GetSafeLocation(method);
        var lineNumber = GetSafeLineNumber(method);

        // Store source member info in the assignments list
        var assignments = new System.Collections.Generic.List<MemberAssignmentModel>();
        foreach (var kvp in sourceMembers)
        {
            var propertyName = kvp.Value.Name; // Use actual property name, not lowercased key
            assignments.Add(new MemberAssignmentModel(
                destMemberName: propertyName, // Using destMemberName to store the property name as the dict key
                sourceExpression: $"{srcParamName}.{propertyName}", // Property accessor
                isInitOnly: false));
        }

        var model = new ForgeMethodModel(
            methodName: method.Name,
            accessibility: accessibility,
            sourceTypeFqn: sourceType.ToDisplayString(),
            sourceTypeShortName: srcShort,
            sourceParameterName: srcParamName,
            destTypeFqn: destDictType.ToDisplayString(),
            destTypeShortName: destShort,
            construction: new ConstructionModel(ConstructionKind.Parameterless, new System.Collections.Generic.List<ConstructorArgModel>()),
            assignments: assignments,
            nestedMethods: new System.Collections.Generic.List<ForgeMethodModel>(),
            methodKind: ForgeMethodKind.ObjectToDictionary,
            sourceFilePath: location?.SourceTree?.FilePath,
            sourceLineNumber: lineNumber,
            dictKeyCasingPolicy: keyCasing,
            dictMissingKeyPolicy: missingKey,
            dictNullValuePolicy: nullValue);

        return (model, diagnostics);
    }

    private static string ApplyKeyCase(string propertyName, int keyCasingPolicy)
    {
        return keyCasingPolicy switch
        {
            0 => propertyName, // Exact
            1 => propertyName.ToLowerInvariant(), // IgnoreCase
            2 => ToCamelCase(propertyName), // CamelCase
            3 => ToSnakeCase(propertyName), // SnakeCase
            _ => propertyName
        };
    }

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
            return str;
        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }

    private static string ToSnakeCase(string str)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < str.Length; i++)
        {
            if (char.IsUpper(str[i]) && i > 0)
                sb.Append('_');
            sb.Append(char.ToLowerInvariant(str[i]));
        }
        return sb.ToString();
    }

    private static string? GenerateParseExpression(string targetType, string stringVarName)
    {
        // For string dictionary values, generate appropriate parsing code
        return targetType switch
        {
            // Primitives
            "int" or "System.Int32" => $"int.Parse({stringVarName})",
            "long" or "System.Int64" => $"long.Parse({stringVarName})",
            "short" or "System.Int16" => $"short.Parse({stringVarName})",
            "byte" or "System.Byte" => $"byte.Parse({stringVarName})",
            "uint" or "System.UInt32" => $"uint.Parse({stringVarName})",
            "ulong" or "System.UInt64" => $"ulong.Parse({stringVarName})",
            "ushort" or "System.UInt16" => $"ushort.Parse({stringVarName})",
            "sbyte" or "System.SByte" => $"sbyte.Parse({stringVarName})",
            "double" or "System.Double" => $"double.Parse({stringVarName}, System.Globalization.CultureInfo.InvariantCulture)",
            "float" or "System.Single" => $"float.Parse({stringVarName}, System.Globalization.CultureInfo.InvariantCulture)",
            "decimal" or "System.Decimal" => $"decimal.Parse({stringVarName}, System.Globalization.CultureInfo.InvariantCulture)",
            "bool" or "System.Boolean" => $"bool.Parse({stringVarName})",
            "string" or "System.String" => stringVarName,
            // DateTime
            "System.DateTime" => $"System.DateTime.Parse({stringVarName}, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None)",
            // Guid
            "System.Guid" => $"System.Guid.Parse({stringVarName})",
            // For enums and other types, unsupported
            _ => null
        };
    }

    private static string GetCSharpKeyword(ITypeSymbol t) => t.SpecialType switch
    {
        SpecialType.System_Boolean => "bool",
        SpecialType.System_Byte => "byte",
        SpecialType.System_SByte => "sbyte",
        SpecialType.System_Int16 => "short",
        SpecialType.System_UInt16 => "ushort",
        SpecialType.System_Int32 => "int",
        SpecialType.System_UInt32 => "uint",
        SpecialType.System_Int64 => "long",
        SpecialType.System_UInt64 => "ulong",
        SpecialType.System_Single => "float",
        SpecialType.System_Double => "double",
        SpecialType.System_Decimal => "decimal",
        SpecialType.System_Char => "char",
        SpecialType.System_String => "string",
        SpecialType.System_Object => "object",
        _ => BuildShortTypeName(t)
    };

    /// <summary>Builds a short, unqualified name for a type, handling arrays and generic collections.</summary>
    private static string BuildShortTypeName(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol arr)
            return $"{BuildShortTypeName(arr.ElementType)}[]";
        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            var args = string.Join(", ", named.TypeArguments.Select(BuildShortTypeName));
            return $"{named.Name}<{args}>";
        }
        if (type.SpecialType != SpecialType.None)
        {
            return type.SpecialType switch
            {
                SpecialType.System_Boolean => "bool",
                SpecialType.System_Byte => "byte",
                SpecialType.System_SByte => "sbyte",
                SpecialType.System_Int16 => "short",
                SpecialType.System_UInt16 => "ushort",
                SpecialType.System_Int32 => "int",
                SpecialType.System_UInt32 => "uint",
                SpecialType.System_Int64 => "long",
                SpecialType.System_UInt64 => "ulong",
                SpecialType.System_Single => "float",
                SpecialType.System_Double => "double",
                SpecialType.System_Decimal => "decimal",
                SpecialType.System_Char => "char",
                SpecialType.System_String => "string",
                SpecialType.System_Object => "object",
                _ => type.Name
            };
        }
        return type.Name;
    }

    // ─── Circular nested forge detection ───────────────────────────────────────

    /// <summary>
    /// Detects circular nested forge dependencies and emits FKF301 errors.
    /// Only checks methods that use nested forging (have assignments with NestedForgeMethodName set).
    /// </summary>
    private static void DetectCircularNestedForge(
        List<ForgeMethodModel> methodModels,
        List<IMethodSymbol> originalMethods,
        List<Diagnostic> diagnostics)
    {
        var methodSymbolLookup = originalMethods.ToDictionary(m => m.Name, m => m);

        // Build adjacency graph: method name -> set of method names it calls via nested forge
        var graph = new Dictionary<string, HashSet<string>>();
        foreach (var method in methodModels)
        {
            var callees = new HashSet<string>();
            foreach (var assignment in method.Assignments)
            {
                if (assignment.NestedForgeMethodName != null)
                    callees.Add(assignment.NestedForgeMethodName);
                if (assignment.CollectionElementForgeMethod != null)
                    callees.Add(assignment.CollectionElementForgeMethod);
            }
            if (callees.Count > 0)
                graph[method.MethodName] = callees;
        }

        var cyclesFound = new HashSet<string>();

        // DFS from each node in the graph to detect cycles
        foreach (var startMethod in graph.Keys.ToList())
        {
            if (cyclesFound.Contains(startMethod)) continue;

            var recursionStack = new HashSet<string>();
            var path = new List<string>();

            if (DetectCycleDfs(startMethod, graph, recursionStack, path, out var cycle))
            {
                var cycleStr = string.Join(" → ", cycle);
                var methodSym = methodSymbolLookup.TryGetValue(cycle[0], out var sym) ? sym : null;

                diagnostics.Add(Diagnostic.Create(
                    ForgeDiagnostics.CircularNestedForge,
                    GetSafeLocation(methodSym),
                    cycleStr));

                // Mark all methods in the cycle as found to avoid duplicate reports
                foreach (var m in cycle)
                    cyclesFound.Add(m);
            }
        }
    }

    /// <summary>
    /// DFS helper to detect cycles in the nested forge graph.
    /// Uses recursion stack to detect back edges (cycles).
    /// Returns true if a cycle is found, with the cycle nodes in order.
    /// </summary>
    private static bool DetectCycleDfs(
        string node,
        Dictionary<string, HashSet<string>> graph,
        HashSet<string> recursionStack,
        List<string> path,
        out List<string> cycle)
    {
        cycle = new List<string>();

        if (recursionStack.Contains(node))
        {
            // Cycle detected: build the cycle from node to current position
            var cycleStart = path.IndexOf(node);
            if (cycleStart >= 0)
                cycle = new List<string>(path.Skip(cycleStart));
            cycle.Add(node);
            return true;
        }

        recursionStack.Add(node);
        path.Add(node);

        if (graph.TryGetValue(node, out var callees))
        {
            foreach (var callee in callees)
            {
                if (DetectCycleDfs(callee, graph, recursionStack, path, out var foundCycle))
                {
                    cycle = foundCycle;
                    recursionStack.Remove(node);
                    path.RemoveAt(path.Count - 1);
                    return true;
                }
            }
        }

        recursionStack.Remove(node);
        path.RemoveAt(path.Count - 1);
        return false;
    }

    // ─── Expression inlining (Phase 5: nested forging) ────────────────────────

    /// <summary>
    /// Post-extraction pass that resolves nested-forge inlining for every method with
    /// <see cref="ForgeMethodModel.GenerateExpression"/> = true. Replaces each nested
    /// assignment's <see cref="MemberAssignmentModel.ExpressionAssignment"/> with the
    /// fully inlined expression body. Cycles are caught by DetectCircularNestedForge before
    /// this phase, so this pass can assume the graph is acyclic. Depth &gt; 5 → FKF508 (info).
    /// </summary>
    private static void ResolveExpressionInlining(
        List<ForgeMethodModel> methodModels,
        List<Diagnostic> diagnostics,
        System.Threading.CancellationToken ct)
    {
        var lookup = methodModels.ToDictionary(m => m.MethodName, m => m);

        for (int methodIndex = 0; methodIndex < methodModels.Count; methodIndex++)
        {
            var method = methodModels[methodIndex];
            if (!method.GenerateExpression) continue;
            ct.ThrowIfCancellationRequested();

            int maxDepth = 0;
            var updatedAssignments = new List<MemberAssignmentModel>();
            bool anyUpdated = false;

            for (int i = 0; i < method.Assignments.Count; i++)
            {
                var assignment = method.Assignments[i];
                var updated = assignment;

                // IgnoreIfNull/IgnoreIfDefault/Condition guard a runtime assignment that has no
                // expression-tree equivalent — exclude the member instead of silently inlining an
                // unconditional nested-forge call that diverges from the imperative method.
                bool excludedByGuard = assignment.IgnoreIfNull || assignment.IgnoreIfDefault || assignment.ConditionMethodName != null;

                // Plain nested-forge member: inline the nested expression body directly.
                if (excludedByGuard && (assignment.NestedForgeMethodName != null || assignment.CollectionElementForgeMethod != null))
                {
                    updated = assignment.WithExpressionAssignment(null);
                    anyUpdated = true;
                    // Only emit FKF506 here for IgnoreIfNull — IgnoreIfDefault and Condition
                    // are already reported during extraction (line ~776), so emitting again
                    // would produce duplicates.
                    if (assignment.IgnoreIfNull && !assignment.IgnoreIfDefault && assignment.ConditionMethodName == null)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            ForgeDiagnostics.ExpressionMemberExcluded,
                            location: null,
                            assignment.DestMemberName,
                            "IgnoreIfNull has no equivalent in expression trees"));
                    }
                }
                else if (assignment.NestedForgeMethodName != null && assignment.NestedForgeSourceAccessor != null)
                {
                    var visited = new HashSet<string> { method.MethodName };
                    var inlined = InlineNestedExpression(
                        assignment.NestedForgeMethodName,
                        assignment.NestedForgeSourceAccessor,
                        assignment.NestedForgeSourceIsRefType,
                        lookup,
                        visited,
                        depth: 1,
                        diagnostics: diagnostics,
                        outerMethodName: method.MethodName,
                        maxDepth: ref maxDepth);

                    updated = assignment.WithExpressionAssignment(inlined);
                    anyUpdated = true;
                }
                // Collection with nested-forge element conversion: inline the per-element body
                // into a .Select(x => ...) lambda, then apply the materializer.
                else if (assignment.CollectionElementForgeMethod != null
                    && assignment.CollectionSourceAccessor != null
                    && assignment.CollectionMaterializer != null)
                {
                    var visited = new HashSet<string> { method.MethodName };
                    // Pass "x" as the outer accessor — the nested method's source param is substituted
                    // with the lambda variable. sourceIsRefType=false: elements inside a Select are
                    // not null-guarded individually (the outer collection guard handles bulk-null).
                    var elementBody = InlineNestedExpression(
                        assignment.CollectionElementForgeMethod,
                        outerAccessor: "x",
                        sourceIsRefType: false,
                        lookup,
                        visited,
                        depth: 1,
                        diagnostics: diagnostics,
                        outerMethodName: method.MethodName,
                        maxDepth: ref maxDepth);

                    if (elementBody == null)
                    {
                        updated = assignment.WithExpressionAssignment(null);
                        anyUpdated = true;
                    }
                    else
                    {
                        var selectExpr = $"{assignment.CollectionSourceAccessor}.Select(x => {elementBody}){assignment.CollectionMaterializer}";
                        if (assignment.CollectionSourceIsRefType)
                        {
                            updated = assignment.WithExpressionAssignment(
                                $"{assignment.CollectionSourceAccessor} == null ? null : {selectExpr}");
                        }
                        else
                        {
                            updated = assignment.WithExpressionAssignment(selectExpr);
                        }
                        anyUpdated = true;
                    }
                }

                updatedAssignments.Add(updated);
            }

            if (maxDepth >= ExpressionNestingErrorThreshold)
            {
                diagnostics.Add(Diagnostic.Create(
                    ForgeDiagnostics.ExpressionNestingDepthLimitExceeded,
                    location: null,
                    method.MethodName,
                    maxDepth));
            }
            else if (maxDepth > ExpressionNestingWarningThreshold)
            {
                diagnostics.Add(Diagnostic.Create(
                    ForgeDiagnostics.ExpressionDeepNesting,
                    location: null,
                    method.MethodName,
                    maxDepth));
            }

            if (anyUpdated)
            {
                methodModels[methodIndex] = new ForgeMethodModel(
                    method.MethodName,
                    method.Accessibility,
                    method.SourceTypeFqn,
                    method.SourceTypeShortName,
                    method.SourceParameterName,
                    method.DestTypeFqn,
                    method.DestTypeShortName,
                    method.Construction,
                    updatedAssignments,
                    method.NestedMethods,
                    method.MethodKind,
                    method.DestParameterName,
                    method.BeforeHookName,
                    method.AfterHookName,
                    method.SourceFilePath,
                    method.SourceLineNumber,
                    method.CollectionProjectExpression,
                    method.ConcreteDictInstantiationName,
                    method.GenerateExpression,
                    method.ExpressionPropertyName);
            }
        }
    }

    /// <summary>
    /// Recursively inlines a nested forge method's expression body into the outer expression.
    /// Substitutes the nested method's source parameter name with the outer accessor expression.
    /// Returns null if a cycle is detected, the nested method is untranslatable, or no members survive.
    /// </summary>
    private static string? InlineNestedExpression(
        string nestedMethodName,
        string outerAccessor,
        bool sourceIsRefType,
        Dictionary<string, ForgeMethodModel> lookup,
        HashSet<string> visitedChain,
        int depth,
        List<Diagnostic> diagnostics,
        string outerMethodName,
        ref int maxDepth)
    {
        if (!lookup.TryGetValue(nestedMethodName, out var nested))
            return null;

        if (nested.MethodKind != ForgeMethodKind.Create) return null;

        // Track maximum depth reached before checking limit
        if (depth > maxDepth) maxDepth = depth;

        // Prevent exceeding expression nesting depth limit
        if (depth >= ExpressionNestingErrorThreshold)
            return null;

        visitedChain.Add(nestedMethodName);
        try
        {
            // Constructor args (parameterized ctor only — parameterless emits "")
            string ctorArgs = "";
            if (nested.Construction.Kind == ConstructionKind.Parameterized)
            {
                if (nested.Construction.ConstructorArgs.Any(a => a.ExpressionAssignment == null))
                    return null;
                ctorArgs = string.Join(", ",
                    nested.Construction.ConstructorArgs.Select(a =>
                        SubstituteParam(a.ExpressionAssignment!, nested.SourceParameterName, outerAccessor)));
            }
            else if (nested.Construction.Kind != ConstructionKind.Parameterless)
            {
                return null;
            }

            // Property initializer body
            var ctorParamNames = new HashSet<string>(
                nested.Construction.ConstructorArgs.Select(a => a.ParameterName.ToLowerInvariant()));

            var bodyEntries = new List<string>();
            foreach (var a in nested.Assignments)
            {
                if (ctorParamNames.Contains(a.DestMemberName.ToLowerInvariant()))
                    continue;

                string? piece;
                if (a.NestedForgeMethodName != null && a.NestedForgeSourceAccessor != null)
                {
                    var subAccessor = SubstituteParam(a.NestedForgeSourceAccessor, nested.SourceParameterName, outerAccessor);
                    piece = InlineNestedExpression(
                        a.NestedForgeMethodName,
                        subAccessor,
                        a.NestedForgeSourceIsRefType,
                        lookup,
                        visitedChain,
                        depth + 1,
                        diagnostics,
                        outerMethodName,
                        ref maxDepth);
                }
                else if (a.ExpressionAssignment != null)
                {
                    piece = SubstituteParam(a.ExpressionAssignment, nested.SourceParameterName, outerAccessor);
                }
                else
                {
                    continue;
                }

                if (piece == null) continue;
                bodyEntries.Add($"{a.DestMemberName} = {piece}");
            }

            string newExpr;
            if (bodyEntries.Count == 0 && ctorArgs == "")
                newExpr = $"new {nested.DestTypeShortName}()";
            else if (bodyEntries.Count == 0)
                newExpr = $"new {nested.DestTypeShortName}({ctorArgs})";
            else
                newExpr = $"new {nested.DestTypeShortName}({ctorArgs}) {{ {string.Join(", ", bodyEntries)} }}";

            if (sourceIsRefType)
                newExpr = $"{outerAccessor} == null ? null : {newExpr}";

            return newExpr;
        }
        finally
        {
            visitedChain.Remove(nestedMethodName);
        }
    }

    /// <summary>
    /// Word-boundary substitution of a parameter identifier with a replacement expression.
    /// Used to rewrite a nested forge method's body to refer to the outer source accessor.
    /// SECURITY NOTE: Uses regex escaping for safety, but is inherently fragile for string-based code generation.
    /// Consider migrating to expression-tree or SyntaxFactory APIs for future robustness.
    /// </summary>
    internal static string SubstituteParam(string expr, string oldParam, string newAccessor)
    {
        return Regex.Replace(expr, $@"\b{Regex.Escape(oldParam)}\b", newAccessor.Replace("$", "$$"));
    }

    /// <summary>
    /// Returns a concrete Dictionary&lt;K,V&gt; name suitable for "new" expressions.
    /// When the type is IDictionary or IReadOnlyDictionary, maps to the concrete Dictionary&lt;K,V&gt;;
    /// otherwise delegates to <see cref="BuildShortTypeName"/>.
    /// </summary>
    private static string GetConcreteDictShortName(ITypeSymbol dictType, ITypeSymbol keyType, ITypeSymbol valType)
    {
        var def = (dictType as INamedTypeSymbol)?.OriginalDefinition.ToDisplayString();
        if (def == "System.Collections.Generic.IDictionary<TKey, TValue>" ||
            def == "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>")
            return $"Dictionary<{GetCSharpKeyword(keyType)}, {GetCSharpKeyword(valType)}>";
        return BuildShortTypeName(dictType);
    }

    /// <summary>
    /// Determines the best constructor for the destination type and resolves parameter mappings.
    /// Selection priority: parameterless → single viable parameterized → error.
    /// Parameters are matched against source members using [ForgeMap] overrides where provided,
    /// with automatic nullable T → Nullable&lt;T&gt; handling. Emits diagnostics for constructor
    /// ambiguity, missing parameters, and type mismatches.
    /// </summary>
    private static (ConstructionModel Construction, List<Diagnostic> Diagnostics) DetermineConstruction(
        INamedTypeSymbol destType,
        Dictionary<string, (ITypeSymbol Type, string Name, bool IsField)> sourceMembers,
        IMethodSymbol forgeMethod,
        string srcParamName,
        INamedTypeSymbol sourceType)
    {
        var diagnostics = new List<Diagnostic>();
        var sourceName = sourceType.Name;

        // Filter to constructors accessible from generated code (internal or public)
        var viableCtors = destType.InstanceConstructors
            .Where(c => c.DeclaredAccessibility >= Accessibility.Internal)
            .ToList();

        if (viableCtors.Count == 0)
        {
            diagnostics.Add(Diagnostic.Create(
                ForgeDiagnostics.NoViableConstructor,
                GetSafeLocation(forgeMethod),
                destType.Name,
                sourceName));
            return (new ConstructionModel(ConstructionKind.Parameterless, new List<ConstructorArgModel>()), diagnostics);
        }

        // Prefer parameterless
        var parameterlessCtor = viableCtors.FirstOrDefault(c => c.Parameters.Length == 0);
        if (parameterlessCtor != null)
        {
            return (new ConstructionModel(ConstructionKind.Parameterless, new List<ConstructorArgModel>()), diagnostics);
        }

        // Find viable parameterized constructors
        var viable = new List<(IMethodSymbol Ctor, List<ConstructorArgModel> Args)>();

        foreach (var ctor in viableCtors)
        {
            var args = new List<ConstructorArgModel>();
            bool allSatisfied = true;

            foreach (var param in ctor.Parameters)
            {
                // Validate parameter type accessibility — must be accessible from generated code
                if (param.Type.DeclaredAccessibility < Accessibility.Internal)
                {
                    allSatisfied = false;
                    break;
                }

                // Check [ForgeMap] on the constructor parameter first, then fall back to param name
                var forgeMapName = GetForgeMapName(param);
                var key = (forgeMapName ?? param.Name).ToLowerInvariant();
                if (sourceMembers.TryGetValue(key, out var src))
                {
                    if (src.Type.ToDisplayString() == param.Type.ToDisplayString())
                    {
                        var accessor = $"{srcParamName}.{src.Name}";
                        args.Add(new ConstructorArgModel(param.Name, accessor, expressionAssignment: accessor));
                    }
                    else if (TryResolveNullableMapping(src.Type, param.Type, out var nk))
                    {
                        var srcSymbol = sourceType.GetMembers().FirstOrDefault(m => m.Name == src.Name);
                        var defaultVal = srcSymbol != null ? GetForgeDefaultValue(srcSymbol) : null;
                        string expr;
                        string exprMode;
                        if (nk == NullableConversionKind.UnwrapValue && defaultVal != null)
                        {
                            var lit = FormatLiteral(defaultVal);
                            expr = $"{srcParamName}.{src.Name} ?? {lit}";
                            exprMode = expr;
                        }
                        else if (nk == NullableConversionKind.UnwrapValue)
                        {
                            expr = $"{srcParamName}.{src.Name}.Value";
                            exprMode = $"{srcParamName}.{src.Name}.GetValueOrDefault()";
                        }
                        else
                        {
                            expr = $"{srcParamName}.{src.Name}";
                            exprMode = expr;
                        }
                        args.Add(new ConstructorArgModel(param.Name, expr, expressionAssignment: exprMode));
                    }
                    else
                    {
                        allSatisfied = false;
                        break;
                    }
                }
                else
                {
                    allSatisfied = false;
                    break;
                }
            }

            if (allSatisfied)
                viable.Add((ctor, args));
        }

        if (viable.Count > 1)
        {
            diagnostics.Add(Diagnostic.Create(
                ForgeDiagnostics.ConstructorAmbiguity,
                GetSafeLocation(forgeMethod),
                destType.Name));
            return (new ConstructionModel(ConstructionKind.Parameterless, new List<ConstructorArgModel>()), diagnostics);
        }

        if (viable.Count == 1)
        {
            return (new ConstructionModel(ConstructionKind.Parameterized, viable[0].Args), diagnostics);
        }

        // No viable: try single constructor with FKF501
        if (viableCtors.Count == 1)
        {
            var ctor = viableCtors[0];
            foreach (var param in ctor.Parameters)
            {
                // Validate parameter type accessibility
                if (param.Type.DeclaredAccessibility < Accessibility.Internal)
                {
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.NoViableConstructor,
                        GetSafeLocation(forgeMethod),
                        destType.Name,
                        sourceName));
                    return (new ConstructionModel(ConstructionKind.Parameterless, new List<ConstructorArgModel>()), diagnostics);
                }

                var forgeMapName501 = GetForgeMapName(param);
                var key = (forgeMapName501 ?? param.Name).ToLowerInvariant();
                var typesMatch = sourceMembers.TryGetValue(key, out var src) &&
                    (src.Type.ToDisplayString() == param.Type.ToDisplayString() ||
                     TryResolveNullableMapping(src.Type, param.Type, out _));
                if (!typesMatch)
                {
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.MissingConstructorParameter,
                        GetSafeLocation(forgeMethod),
                        param.Name,
                        destType.Name,
                        sourceName));
                }
            }
        }
        else
        {
            diagnostics.Add(Diagnostic.Create(
                ForgeDiagnostics.NoViableConstructor,
                GetSafeLocation(forgeMethod),
                destType.Name,
                sourceName));
        }

        return (new ConstructionModel(ConstructionKind.Parameterless, new List<ConstructorArgModel>()), diagnostics);
    }

    // ─── Source Generation ────────────────────────────────────────────────────

    private static string GenerateSource(ForgeClassModel model, System.Threading.CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.CodeDom.Compiler;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Collections.Immutable;");
        sb.AppendLine("using System.Diagnostics;");
        sb.AppendLine("using System.Linq;");
        if (model.Methods.Any(m => m.GenerateExpression))
            sb.AppendLine("using System.Linq.Expressions;");
        sb.AppendLine();

        bool hasNamespace = !string.IsNullOrEmpty(model.Namespace);
        if (hasNamespace)
        {
            sb.AppendLine($"namespace {model.Namespace}");
            sb.AppendLine("{");
        }

        // Base indent: "    " when inside a namespace, "" otherwise
        var baseIndent = hasNamespace ? "    " : "";

        // Emit containing type declarations (for nested classes)
        foreach (var containingType in model.ContainingTypes)
        {
            sb.AppendLine($"{baseIndent}partial {containingType.Keyword} {containingType.Name}");
            sb.AppendLine($"{baseIndent}{{");
            baseIndent += "    ";
        }

        sb.AppendLine($"{baseIndent}{model.Accessibility} static partial class {model.ClassName}");
        sb.AppendLine($"{baseIndent}{{");

        var methodIndent = baseIndent + "    ";
        foreach (var method in model.Methods)
        {
            ct.ThrowIfCancellationRequested();
            GenerateMethodBody(sb, method, indent: methodIndent);
        }

        sb.AppendLine($"{baseIndent}}}");

        // Generate extension methods if enabled (and not in a nested class)
        if (model.GenerateExtensionMethods && model.ContainingTypes.Count == 0)
        {
            sb.AppendLine();
            sb.AppendLine($"{baseIndent}/// <summary>Extension method forwarders for <see cref=\"{model.ClassName}\"/>. Auto-generated by FreakyKit.Forge.</summary>");
            sb.AppendLine($"{baseIndent}public static class {model.ClassName}Extensions");
            sb.AppendLine($"{baseIndent}{{");

            foreach (var method in model.Methods)
            {
                ct.ThrowIfCancellationRequested();
                // Skip extension method generation for private methods — extension methods must be public
                if (method.Accessibility != "private")
                {
                    GenerateExtensionMethod(sb, method, model.ClassName, indent: methodIndent);
                }
            }

            sb.AppendLine($"{baseIndent}}}");
        }

        // Close containing type declarations (innermost first)
        for (int i = model.ContainingTypes.Count - 1; i >= 0; i--)
        {
            baseIndent = baseIndent.Substring(4);
            sb.AppendLine($"{baseIndent}}}");
        }

        if (hasNamespace)
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates the partial method implementation body. Handles three method kinds:
    /// CollectionProject (single LINQ expression), DictionaryProject (foreach-based transformation),
    /// and Create/Update (member assignments with constructor initialization). Includes XML doc,
    /// [GeneratedCode], [DebuggerStepThrough] attributes, and #line directives for debugging.
    /// </summary>
    private static void GenerateMethodBody(StringBuilder sb, ForgeMethodModel method, string indent)
    {
        if (method.MethodKind == ForgeMethodKind.CollectionProject)
        {
            sb.AppendLine($"{indent}/// <summary>Projects each element of <paramref name=\"{method.SourceParameterName}\"/> to <see cref=\"{method.DestTypeShortName}\"/>. Auto-generated by FreakyKit.Forge.</summary>");
            sb.AppendLine($"{indent}[GeneratedCode(\"FreakyKit.Forge.Generator\", \"1.0.0\")]");
            sb.AppendLine($"{indent}[DebuggerStepThrough]");
            if (!string.IsNullOrEmpty(method.SourceFilePath) && method.SourceLineNumber > 0)
                sb.AppendLine($"{indent}#line {method.SourceLineNumber} \"{method.SourceFilePath?.Replace("\\", "\\\\")}\"");
            sb.AppendLine($"{indent}{method.Accessibility} static partial {method.DestTypeShortName} {method.MethodName}({method.SourceTypeShortName} {method.SourceParameterName})");
            sb.AppendLine($"{indent}#line default");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    return {method.CollectionProjectExpression};");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
            return;
        }

        if (method.MethodKind == ForgeMethodKind.DictionaryProject)
        {
            sb.AppendLine($"{indent}/// <summary>Projects each value of <paramref name=\"{method.SourceParameterName}\"/> to <see cref=\"{method.DestTypeShortName}\"/>. Auto-generated by FreakyKit.Forge.</summary>");
            sb.AppendLine($"{indent}[GeneratedCode(\"FreakyKit.Forge.Generator\", \"1.0.0\")]");
            sb.AppendLine($"{indent}[DebuggerStepThrough]");
            if (!string.IsNullOrEmpty(method.SourceFilePath) && method.SourceLineNumber > 0)
                sb.AppendLine($"{indent}#line {method.SourceLineNumber} \"{method.SourceFilePath?.Replace("\\", "\\\\")}\"");
            sb.AppendLine($"{indent}{method.Accessibility} static partial {method.DestTypeShortName} {method.MethodName}({method.SourceTypeShortName} {method.SourceParameterName})");
            sb.AppendLine($"{indent}#line default");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    if ({method.SourceParameterName} == null) return null;");
            var valueTransform = method.CollectionProjectExpression;
            var concreteDictType = method.ConcreteDictInstantiationName ?? method.DestTypeShortName;
            if (string.IsNullOrEmpty(valueTransform))
            {
                sb.AppendLine($"{indent}    return new {concreteDictType}({method.SourceParameterName});");
            }
            else
            {
                sb.AppendLine($"{indent}    var __result = new {concreteDictType}({method.SourceParameterName}.Count);");
                sb.AppendLine($"{indent}    foreach (var __kvp in {method.SourceParameterName})");
                sb.AppendLine($"{indent}        __result[__kvp.Key] = {valueTransform};");
                sb.AppendLine($"{indent}    return __result;");
            }
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
            return;
        }

        if (method.MethodKind == ForgeMethodKind.DictionaryToObject)
        {
            sb.AppendLine($"{indent}/// <summary>Converts a dictionary to <see cref=\"{method.DestTypeShortName}\"/>. Auto-generated by FreakyKit.Forge.</summary>");
            sb.AppendLine($"{indent}[GeneratedCode(\"FreakyKit.Forge.Generator\", \"1.0.0\")]");
            sb.AppendLine($"{indent}[DebuggerStepThrough]");
            if (!string.IsNullOrEmpty(method.SourceFilePath) && method.SourceLineNumber > 0)
                sb.AppendLine($"{indent}#line {method.SourceLineNumber} \"{method.SourceFilePath?.Replace("\\", "\\\\")}\"");
            sb.AppendLine($"{indent}{method.Accessibility} static partial {method.DestTypeShortName} {method.MethodName}({method.SourceTypeShortName} {method.SourceParameterName})");
            sb.AppendLine($"{indent}#line default");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    if ({method.SourceParameterName} == null) return null;");
            sb.AppendLine($"{indent}    var __result = new {method.DestTypeShortName}();");

            // For each assignment (destination member), generate dict lookup code
            var isStringDict = method.DictValueType == "string" || method.DictValueType == "System.String";
            foreach (var assignment in method.Assignments)
            {
                var dictKey = ApplyKeyCase(assignment.DestMemberName, method.DictKeyCasingPolicy);
                var varName = $"__val_{assignment.DestMemberName}";
                var keyCasing = method.DictKeyCasingPolicy;
                var missingKeyPolicy = method.DictMissingKeyPolicy;
                var propType = assignment.SourceMemberType;

                // For string dicts, try to parse; for object dicts, cast
                string assignExpr;
                if (isStringDict && !string.IsNullOrEmpty(propType) && propType != "string" && propType != "System.String")
                {
                    // Try to generate parse expression
                    var parseExpr = GenerateParseExpression(propType, varName);
                    assignExpr = parseExpr ?? varName; // Fallback to direct assignment if no parser
                }
                else
                {
                    // Object dict or string type - use cast
                    assignExpr = !string.IsNullOrEmpty(propType)
                        ? $"({propType}){varName}"
                        : varName;
                }

                if (keyCasing == 0) // Exact
                {
                    // Exact match: TryGetValue with exact key
                    if (missingKeyPolicy == 0) // Throw
                    {
                        // Throw if key not found
                        sb.AppendLine($"{indent}    if (!{method.SourceParameterName}.TryGetValue(\"{dictKey}\", out var {varName}))");
                        sb.AppendLine($"{indent}        throw new KeyNotFoundException(\"{dictKey}\");");
                        sb.AppendLine($"{indent}    __result.{assignment.DestMemberName} = {assignExpr};");
                    }
                    else if (missingKeyPolicy == 1) // UseDefault
                    {
                        // Use default if key not found
                        sb.AppendLine($"{indent}    if ({method.SourceParameterName}.TryGetValue(\"{dictKey}\", out var {varName}))");
                        sb.AppendLine($"{indent}        __result.{assignment.DestMemberName} = {assignExpr};");
                        // else: leave at default (already initialized)
                    }
                    else if (missingKeyPolicy == 2) // Skip
                    {
                        // Skip if key not found (same as UseDefault for Exact match)
                        sb.AppendLine($"{indent}    if ({method.SourceParameterName}.TryGetValue(\"{dictKey}\", out var {varName}))");
                        sb.AppendLine($"{indent}        __result.{assignment.DestMemberName} = {assignExpr};");
                    }
                    else if (missingKeyPolicy == 3) // ReturnNull
                    {
                        // Assign null if key not found (or use default if non-nullable)
                        var isNullable = propType?.EndsWith("?") == true || propType == "object";
                        if (isNullable)
                        {
                            sb.AppendLine($"{indent}    __result.{assignment.DestMemberName} = {method.SourceParameterName}.TryGetValue(\"{dictKey}\", out var {varName}) ? {assignExpr} : null;");
                        }
                        else
                        {
                            // For non-nullable, use default
                            sb.AppendLine($"{indent}    if ({method.SourceParameterName}.TryGetValue(\"{dictKey}\", out var {varName}))");
                            sb.AppendLine($"{indent}        __result.{assignment.DestMemberName} = {assignExpr};");
                        }
                    }
                }
                else // IgnoreCase, CamelCase, or SnakeCase
                {
                    // Case-insensitive or transformed key matching
                    sb.AppendLine($"{indent}    var __key_{assignment.DestMemberName} = {method.SourceParameterName}.Keys.FirstOrDefault(k => string.Equals(k, \"{dictKey}\", StringComparison.OrdinalIgnoreCase));");

                    if (missingKeyPolicy == 0) // Throw
                    {
                        sb.AppendLine($"{indent}    if (__key_{assignment.DestMemberName} == null)");
                        sb.AppendLine($"{indent}        throw new KeyNotFoundException(\"{dictKey}\");");
                        sb.AppendLine($"{indent}    {method.SourceParameterName}.TryGetValue(__key_{assignment.DestMemberName}, out var {varName});");
                        sb.AppendLine($"{indent}    __result.{assignment.DestMemberName} = {assignExpr};");
                    }
                    else if (missingKeyPolicy == 1 || missingKeyPolicy == 2) // UseDefault or Skip
                    {
                        sb.AppendLine($"{indent}    if (__key_{assignment.DestMemberName} != null && {method.SourceParameterName}.TryGetValue(__key_{assignment.DestMemberName}, out var {varName}))");
                        sb.AppendLine($"{indent}        __result.{assignment.DestMemberName} = {assignExpr};");
                    }
                    else if (missingKeyPolicy == 3) // ReturnNull
                    {
                        var isNullable = propType?.EndsWith("?") == true || propType == "object";
                        if (isNullable)
                        {
                            sb.AppendLine($"{indent}    if (__key_{assignment.DestMemberName} != null && {method.SourceParameterName}.TryGetValue(__key_{assignment.DestMemberName}, out var {varName}))");
                            sb.AppendLine($"{indent}        __result.{assignment.DestMemberName} = {assignExpr};");
                            sb.AppendLine($"{indent}    else");
                            sb.AppendLine($"{indent}        __result.{assignment.DestMemberName} = null;");
                        }
                        else
                        {
                            sb.AppendLine($"{indent}    if (__key_{assignment.DestMemberName} != null && {method.SourceParameterName}.TryGetValue(__key_{assignment.DestMemberName}, out var {varName}))");
                            sb.AppendLine($"{indent}        __result.{assignment.DestMemberName} = {assignExpr};");
                        }
                    }
                }
            }

            sb.AppendLine($"{indent}    return __result;");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
            return;
        }

        if (method.MethodKind == ForgeMethodKind.ObjectToDictionary)
        {
            sb.AppendLine($"{indent}/// <summary>Converts <paramref name=\"{method.SourceParameterName}\"/> to a dictionary. Auto-generated by FreakyKit.Forge.</summary>");
            sb.AppendLine($"{indent}[GeneratedCode(\"FreakyKit.Forge.Generator\", \"1.0.0\")]");
            sb.AppendLine($"{indent}[DebuggerStepThrough]");
            if (!string.IsNullOrEmpty(method.SourceFilePath) && method.SourceLineNumber > 0)
                sb.AppendLine($"{indent}#line {method.SourceLineNumber} \"{method.SourceFilePath?.Replace("\\", "\\\\")}\"");
            sb.AppendLine($"{indent}{method.Accessibility} static partial {method.DestTypeShortName} {method.MethodName}({method.SourceTypeShortName} {method.SourceParameterName})");
            sb.AppendLine($"{indent}#line default");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    if ({method.SourceParameterName} == null) return null;");
            sb.AppendLine($"{indent}    var __result = new {method.DestTypeShortName}();");

            // For each source member, add it to the dictionary
            foreach (var assignment in method.Assignments)
            {
                var dictKey = ApplyKeyCase(assignment.DestMemberName, method.DictKeyCasingPolicy);
                var nullValuePolicy = method.DictNullValuePolicy;

                if (nullValuePolicy == 0) // Include
                {
                    // Always include, even null
                    sb.AppendLine($"{indent}    __result[\"{dictKey}\"] = {assignment.SourceExpression};");
                }
                else if (nullValuePolicy == 1) // Skip
                {
                    // Skip if null
                    sb.AppendLine($"{indent}    var __val_{assignment.DestMemberName} = {assignment.SourceExpression};");
                    sb.AppendLine($"{indent}    if (__val_{assignment.DestMemberName} != null)");
                    sb.AppendLine($"{indent}        __result[\"{dictKey}\"] = __val_{assignment.DestMemberName};");
                }
            }

            sb.AppendLine($"{indent}    return __result;");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
            return;
        }

        // XML doc comment
        if (method.MethodKind == ForgeMethodKind.Update)
            sb.AppendLine($"{indent}/// <summary>Updates <paramref name=\"{method.DestParameterName}\"/> from <paramref name=\"{method.SourceParameterName}\"/>. Auto-generated by FreakyKit.Forge.</summary>");
        else
            sb.AppendLine($"{indent}/// <summary>Maps <see cref=\"{method.SourceTypeShortName}\"/> to <see cref=\"{method.DestTypeShortName}\"/>. Auto-generated by FreakyKit.Forge.</summary>");

        // Attributes
        sb.AppendLine($"{indent}[GeneratedCode(\"FreakyKit.Forge.Generator\", \"1.0.0\")]");
        sb.AppendLine($"{indent}[DebuggerStepThrough]");

        // #line directive
        if (!string.IsNullOrEmpty(method.SourceFilePath) && method.SourceLineNumber > 0)
            sb.AppendLine($"{indent}#line {method.SourceLineNumber} \"{method.SourceFilePath?.Replace("\\", "\\\\")}\"");

        if (method.MethodKind == ForgeMethodKind.Update)
        {
            // Update method: void return, 2 parameters (source, dest)
            sb.AppendLine($"{indent}{method.Accessibility} static partial void {method.MethodName}({method.SourceTypeShortName} {method.SourceParameterName}, {method.DestTypeShortName} {method.DestParameterName})");
            sb.AppendLine($"{indent}#line default");
            sb.AppendLine($"{indent}{{");

            // Before hook
            if (method.BeforeHookName != null)
                sb.AppendLine($"{indent}    {method.BeforeHookName}({method.SourceParameterName});");

            // Property assignments — assign to the dest parameter directly
            foreach (var assignment in method.Assignments)
            {
                string condition = "";

                if (assignment.IgnoreIfNull && assignment.NullCheckExpression != null)
                {
                    condition = $"{assignment.NullCheckExpression} != null";
                }

                if (assignment.IgnoreIfDefault && assignment.SourceMemberType != null)
                {
                    var srcAccessor = $"{method.SourceParameterName}.{assignment.SourceMemberName}";
                    var defaultCheck = $"!EqualityComparer<{assignment.SourceMemberType}>.Default.Equals({srcAccessor}, default)";
                    condition = condition != "" ? $"{condition} && {defaultCheck}" : defaultCheck;
                }

                if (assignment.ConditionMethodName != null)
                {
                    var methodCall = $"{assignment.ConditionMethodName}({method.SourceParameterName})";
                    condition = condition != "" ? $"{condition} && {methodCall}" : methodCall;
                }

                if (condition != "")
                {
                    sb.AppendLine($"{indent}    if ({condition}) {method.DestParameterName}.{assignment.DestMemberName} = {assignment.SourceExpression};");
                }
                else
                {
                    sb.AppendLine($"{indent}    {method.DestParameterName}.{assignment.DestMemberName} = {assignment.SourceExpression};");
                }
            }

            // After hook
            if (method.AfterHookName != null)
                sb.AppendLine($"{indent}    {method.AfterHookName}({method.SourceParameterName}, {method.DestParameterName});");

            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }
        else
        {
            // Create method: non-void return, 1 parameter
            // Use short names: the generated file is placed in the same namespace as the forge class,
            // so both source and dest types are accessible by their unqualified names.

            sb.AppendLine($"{indent}{method.Accessibility} static partial {method.DestTypeShortName} {method.MethodName}({method.SourceTypeShortName} {method.SourceParameterName})");
            sb.AppendLine($"{indent}#line default");
            sb.AppendLine($"{indent}{{");

            // Before hook
            if (method.BeforeHookName != null)
                sb.AppendLine($"{indent}    {method.BeforeHookName}({method.SourceParameterName});");

            // Separate init-only assignments (must go in object initializer) from regular assignments.
            // IgnoreIfNull/IgnoreIfDefault/Condition are rejected on init-only members at extraction
            // time (FKF316), so no init-only assignment here carries a guard.
            var initOnlyAssignments = method.Assignments.Where(a => a.IsInitOnly).ToList();
            var regularAssignments = method.Assignments.Where(a => !a.IsInitOnly).ToList();

            // Construction with optional object initializer for init-only properties
            string ctorArgs = "";
            if (method.Construction.Kind == ConstructionKind.Parameterized)
                ctorArgs = string.Join(", ", method.Construction.ConstructorArgs.Select(a => a.SourceExpression));

            if (initOnlyAssignments.Count > 0)
            {
                // Object initializer syntax: new Dest(args) { InitProp = expr, ... };
                sb.Append($"{indent}    var __result = new {method.DestTypeShortName}({ctorArgs})");
                sb.AppendLine();
                sb.AppendLine($"{indent}    {{");
                for (int i = 0; i < initOnlyAssignments.Count; i++)
                {
                    var a = initOnlyAssignments[i];
                    var comma = i < initOnlyAssignments.Count - 1 ? "," : "";
                    sb.AppendLine($"{indent}        {a.DestMemberName} = {a.SourceExpression}{comma}");
                }
                sb.AppendLine($"{indent}    }};");
            }
            else if (method.Construction.Kind == ConstructionKind.Parameterless)
            {
                sb.AppendLine($"{indent}    var __result = new {method.DestTypeShortName}();");
            }
            else if (method.Construction.Kind == ConstructionKind.Parameterized)
            {
                sb.AppendLine($"{indent}    var __result = new {method.DestTypeShortName}({ctorArgs});");
            }

            // Regular property assignments (non-init-only)
            foreach (var assignment in regularAssignments)
            {
                string condition = "";

                if (assignment.IgnoreIfNull && assignment.NullCheckExpression != null)
                {
                    condition = $"{assignment.NullCheckExpression} != null";
                }

                if (assignment.IgnoreIfDefault && assignment.SourceMemberType != null)
                {
                    var srcAccessor = $"{method.SourceParameterName}.{assignment.SourceMemberName}";
                    var defaultCheck = $"!EqualityComparer<{assignment.SourceMemberType}>.Default.Equals({srcAccessor}, default)";
                    condition = condition != "" ? $"{condition} && {defaultCheck}" : defaultCheck;
                }

                if (assignment.ConditionMethodName != null)
                {
                    var methodCall = $"{assignment.ConditionMethodName}({method.SourceParameterName})";
                    condition = condition != "" ? $"{condition} && {methodCall}" : methodCall;
                }

                if (condition != "")
                {
                    sb.AppendLine($"{indent}    if ({condition}) __result.{assignment.DestMemberName} = {assignment.SourceExpression};");
                }
                else
                {
                    sb.AppendLine($"{indent}    __result.{assignment.DestMemberName} = {assignment.SourceExpression};");
                }
            }

            // After hook
            if (method.AfterHookName != null)
                sb.AppendLine($"{indent}    {method.AfterHookName}({method.SourceParameterName}, __result);");

            sb.AppendLine($"{indent}    return __result;");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();

            // Expression projection property (Phase 1: parameterless ctor + same-type assignments only)
            if (method.GenerateExpression)
                GenerateExpressionProperty(sb, method, indent);
        }
    }

    /// <summary>
    /// Emits a static <c>Expression&lt;Func&lt;TSrc, TDest&gt;&gt;</c> property alongside the imperative
    /// method body. Supports parameterless and parameterized constructors. Members without a
    /// translatable <see cref="MemberAssignmentModel.ExpressionAssignment"/> are silently omitted;
    /// the property is suppressed entirely when construction cannot be expressed translatably or when
    /// no members qualify (and there are no constructor args).
    /// </summary>
    private static void GenerateExpressionProperty(StringBuilder sb, ForgeMethodModel method, string indent)
    {
        // Update methods can never produce an expression (no return value).
        if (method.MethodKind != ForgeMethodKind.Create)
            return;

        // Parameterized constructors are translatable only if every arg has a translatable form.
        // If any constructor arg lacks ExpressionAssignment, suppress the expression property
        // (we can't partially construct).
        string ctorArgs = "";
        if (method.Construction.Kind == ConstructionKind.Parameterized)
        {
            if (method.Construction.ConstructorArgs.Any(a => a.ExpressionAssignment == null))
                return;
            ctorArgs = string.Join(", ", method.Construction.ConstructorArgs.Select(a => a.ExpressionAssignment));
        }
        else if (method.Construction.Kind != ConstructionKind.Parameterless)
        {
            // Defensive: any other construction kind (e.g. ConstructionKind.None for update methods)
            // is not expressible.
            return;
        }

        // Property assignments that ARE expression-translatable
        var emittable = method.Assignments
            .Where(a => a.ExpressionAssignment != null)
            .ToList();

        // Property assignments used as constructor args are not re-emitted in the initializer
        // (matches the imperative behavior).
        var ctorParamNames = new HashSet<string>(
            method.Construction.ConstructorArgs.Select(a => a.ParameterName.ToLowerInvariant()));
        emittable = emittable.Where(a => !ctorParamNames.Contains(a.DestMemberName.ToLowerInvariant())).ToList();

        // If construction is parameterless and there are no member assignments, the expression is just
        // `new Dest()` — emit it (still useful), unless there are also no constructor args (genuinely empty).
        bool isParameterless = method.Construction.Kind == ConstructionKind.Parameterless;
        if (isParameterless && emittable.Count == 0)
            return;

        sb.AppendLine($"{indent}/// <summary>Expression-tree projection of <see cref=\"{method.MethodName}\"/>, usable with <c>IQueryable.Select</c>. Auto-generated by FreakyKit.Forge.</summary>");
        sb.AppendLine($"{indent}[GeneratedCode(\"FreakyKit.Forge.Generator\", \"1.0.0\")]");
        sb.AppendLine($"{indent}{method.Accessibility} static Expression<Func<{method.SourceTypeShortName}, {method.DestTypeShortName}>> {method.ExpressionPropertyName} {{ get; }} =");
        if (emittable.Count == 0)
        {
            // Parameterized ctor with no extra property assignments — single-line form
            sb.AppendLine($"{indent}    {method.SourceParameterName} => new {method.DestTypeShortName}({ctorArgs});");
        }
        else
        {
            sb.AppendLine($"{indent}    {method.SourceParameterName} => new {method.DestTypeShortName}({ctorArgs})");
            sb.AppendLine($"{indent}    {{");
            for (int i = 0; i < emittable.Count; i++)
            {
                var a = emittable[i];
                var comma = i < emittable.Count - 1 ? "," : "";
                sb.AppendLine($"{indent}        {a.DestMemberName} = {a.ExpressionAssignment}{comma}");
            }
            sb.AppendLine($"{indent}    }};");
        }
        sb.AppendLine();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the member is accessible from a static, non-derived context in the forge assembly.
    /// </summary>
    private static bool IsMemberAccessibleFromStaticContext(ISymbol member, IAssemblySymbol? forgeAssembly)
    {
        switch (member.DeclaredAccessibility)
        {
            case Accessibility.Public:
                return true;
            case Accessibility.Internal:
            case Accessibility.ProtectedOrInternal:
                return forgeAssembly == null || SymbolEqualityComparer.Default.Equals(member.ContainingAssembly, forgeAssembly);
            case Accessibility.Private:
            case Accessibility.Protected:
            case Accessibility.ProtectedAndInternal:
            default:
                return false;
        }
    }

    private static Dictionary<string, (ITypeSymbol Type, string Name, bool IsField)> CollectMembers(
        INamedTypeSymbol type,
        bool includeFields,
        IMethodSymbol? forgeMethod,
        List<Diagnostic>? diagnostics,
        bool isSourceSide = true,
        IAssemblySymbol? forgeAssembly = null)
    {
        var result = new Dictionary<string, (ITypeSymbol, string, bool)>();

        for (var currentType = type; currentType != null; currentType = currentType.BaseType)
        {
            foreach (var member in currentType.GetMembers())
            {
                if (member.IsStatic) continue;
                if (!IsMemberAccessibleFromStaticContext(member, forgeAssembly)) continue;

                if (member is IPropertySymbol prop)
                {
                    if (prop.IsIndexer) continue;
                    if (isSourceSide && (prop.GetMethod == null || !IsMemberAccessibleFromStaticContext(prop.GetMethod, forgeAssembly))) continue;
                    if (!isSourceSide && prop.SetMethod != null && !IsMemberAccessibleFromStaticContext(prop.SetMethod, forgeAssembly)) continue;
                    if (ShouldIgnoreMember(prop, isSourceSide)) continue;
                    var mapName = GetForgeMapName(prop);
                    var key = (mapName ?? prop.Name).ToLowerInvariant();
                    if (result.ContainsKey(key))
                    {
                        if (currentType.Equals(type, SymbolEqualityComparer.Default) && forgeMethod != null && diagnostics != null)
                        {
                            diagnostics.Add(Diagnostic.Create(
                                ForgeDiagnostics.DuplicateForgeMapTarget,
                                GetSafeLocation(forgeMethod),
                                key, prop.Name, type.Name));
                        }
                    }
                    else
                    {
                        result[key] = (prop.Type, prop.Name, false);
                    }
                }
                else if (member is IFieldSymbol field)
                {
                    if (ShouldIgnoreMember(field, isSourceSide)) continue;
                    if (!includeFields)
                    {
                        if (currentType.Equals(type, SymbolEqualityComparer.Default) && forgeMethod != null && diagnostics != null)
                        {
                            diagnostics.Add(Diagnostic.Create(
                                ForgeDiagnostics.FieldIgnored,
                                GetSafeLocation(forgeMethod),
                                field.Name,
                                type.Name));
                        }
                        continue;
                    }

                    var mapName = GetForgeMapName(field);
                    var key = (mapName ?? field.Name).ToLowerInvariant();
                    if (result.ContainsKey(key))
                    {
                        if (currentType.Equals(type, SymbolEqualityComparer.Default) && forgeMethod != null && diagnostics != null)
                        {
                            diagnostics.Add(Diagnostic.Create(
                                ForgeDiagnostics.DuplicateForgeMapTarget,
                                GetSafeLocation(forgeMethod),
                                key, field.Name, type.Name));
                        }
                    }
                    else
                    {
                        result[key] = (field.Type, field.Name, true);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Result of a flattening resolution attempt.
    /// </summary>
    private sealed class FlatteningResult
    {
        public string? Expression { get; set; }
        public int Depth { get; set; }
        public string? ResolvedPath { get; set; }  // e.g. "Address.Coords.Latitude" for diagnostics
    }

    private static bool TryResolveFlattenedMapping(
        INamedTypeSymbol sourceType,
        string destKeyLower,
        ITypeSymbol destMemberType,
        string sourceParamName,
        out string flattenExpression,
        out int flatteningDepth,
        out string? flatteningPath,
        out bool depthLimitExceeded,
        out bool ambiguousFlatteningDetected)
    {
        flattenExpression = "";
        flatteningDepth = 0;
        flatteningPath = null;
        depthLimitExceeded = false;
        ambiguousFlatteningDetected = false;

        // Recursively search all nested levels
        var result = TryResolveFlattenedMappingRecursive(
            sourceType,
            destKeyLower,
            destMemberType,
            sourceParamName,
            currentAccess: "",
            intermediateChain: new List<ITypeSymbol>(),
            depth: 0,
            pathParts: new List<string>(),
            depthExceeded: out depthLimitExceeded,
            ambiguousMatch: out ambiguousFlatteningDetected);

        if (result?.Expression != null)
        {
            flattenExpression = result.Expression;
            flatteningDepth = result.Depth;
            flatteningPath = result.ResolvedPath;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Recursively searches for a flattened mapping at any depth.
    /// Returns a FlatteningResult with the full access expression (e.g., "source.Address?.Coords?.Latitude") if found, null otherwise.
    /// Tracks depth to detect deep nesting and avoid unbounded recursion.
    /// </summary>
    private static FlatteningResult? TryResolveFlattenedMappingRecursive(
        INamedTypeSymbol currentType,
        string remainingKeyLower,
        ITypeSymbol destMemberType,
        string sourceParamName,
        string currentAccess,
        List<ITypeSymbol> intermediateChain,
        int depth,
        List<string> pathParts,
        out bool depthExceeded,
        out bool ambiguousMatch)
    {
        const int MaxFlatteningDepth = 10;
        depthExceeded = false;
        ambiguousMatch = false;

        // Safety: prevent infinite recursion on circular types
        if (intermediateChain.Contains(currentType, SymbolEqualityComparer.Default))
            return null;

        // Check depth limit
        if (depth > MaxFlatteningDepth)
        {
            depthExceeded = true;
            return null;
        }

        // Detect potential ambiguous matches at this level
        var prefixMatches = new List<IPropertySymbol>();
        foreach (var member in currentType.GetMembers())
        {
            if (member.IsStatic || member.DeclaredAccessibility == Accessibility.Private)
                continue;

            if (member is not IPropertySymbol prop)
                continue; // Only support properties for flattening

            if (prop.IsIndexer)
                continue;

            var memberNameLower = prop.Name.ToLowerInvariant();
            if (remainingKeyLower.StartsWith(memberNameLower))
                prefixMatches.Add(prop);
        }

        // If multiple prefixes match, it's ambiguous
        if (prefixMatches.Count > 1)
            ambiguousMatch = true;

        // Try each member as a prefix of the remaining key (use longest match first due to sorting)
        foreach (var prop in prefixMatches.OrderByDescending(p => p.Name.Length))
        {
            var memberNameLower = prop.Name.ToLowerInvariant();

            // If the member name exactly matches the entire remaining key
            if (memberNameLower == remainingKeyLower)
            {
                // Check type compatibility
                if (prop.Type.ToDisplayString() == destMemberType.ToDisplayString())
                {
                    // Build the access expression with proper null-safety.
                    // Use ?. if we're accessing a property on a reference type (which can be null),
                    // otherwise use . for value types.
                    var access = string.IsNullOrEmpty(currentAccess)
                        ? $"{sourceParamName}.{prop.Name}"
                        : currentAccess + (currentType.IsReferenceType ? $"?.{prop.Name}" : $".{prop.Name}");

                    var newPathParts = new List<string>(pathParts) { prop.Name };
                    var path = string.Join(".", newPathParts);

                    return new FlatteningResult
                    {
                        Expression = access,
                        Depth = depth,
                        ResolvedPath = path
                    };
                }
                continue;
            }

            // Partial match: the member's name is a prefix of the remaining key
            // Recursively search the nested type for the remainder
            if (prop.Type is INamedTypeSymbol nestedType)
            {
                var remainder = remainingKeyLower.Substring(memberNameLower.Length);
                var nextAccess = string.IsNullOrEmpty(currentAccess)
                    ? $"{sourceParamName}.{prop.Name}"
                    : currentAccess + (currentType.IsReferenceType ? $"?.{prop.Name}" : $".{prop.Name}");

                var nextChain = new List<ITypeSymbol>(intermediateChain) { currentType };
                var nextPathParts = new List<string>(pathParts) { prop.Name };

                var result = TryResolveFlattenedMappingRecursive(
                    nestedType,
                    remainder,
                    destMemberType,
                    sourceParamName,
                    nextAccess,
                    nextChain,
                    depth + 1,
                    nextPathParts,
                    out var nestedDepthExceeded,
                    out var nestedAmbiguous);

                if (nestedDepthExceeded) depthExceeded = true;
                if (nestedAmbiguous) ambiguousMatch = true;
                if (result?.Expression != null)
                    return result;
            }
        }

        return null;
    }

    private static bool IsReadOnlyMember(INamedTypeSymbol type, string keyLower)
    {
        foreach (var member in type.GetMembers())
        {
            if (member.IsStatic) continue;
            if (member.DeclaredAccessibility == Accessibility.Private) continue;

            if (member is IPropertySymbol prop)
            {
                if (prop.IsIndexer) continue;
                var mapName = GetForgeMapName(prop);
                var effectiveKey = (mapName ?? prop.Name).ToLowerInvariant();
                if (effectiveKey == keyLower)
                    return prop.SetMethod == null; // init-only is NOT read-only — handled via object initializer
            }
            else if (member is IFieldSymbol field)
            {
                var mapName = GetForgeMapName(field);
                var effectiveKey = (mapName ?? field.Name).ToLowerInvariant();
                if (effectiveKey == keyLower)
                    return field.IsReadOnly || field.IsConst;
            }
        }
        return false;
    }

    private static bool IsInitOnlyMember(INamedTypeSymbol type, string keyLower)
    {
        foreach (var member in type.GetMembers())
        {
            if (member.IsStatic) continue;
            if (member.DeclaredAccessibility == Accessibility.Private) continue;

            if (member is IPropertySymbol prop)
            {
                if (prop.IsIndexer) continue;
                var mapName = GetForgeMapName(prop);
                var effectiveKey = (mapName ?? prop.Name).ToLowerInvariant();
                if (effectiveKey == keyLower)
                    return prop.SetMethod != null && prop.SetMethod.IsInitOnly;
            }
        }
        return false;
    }

    private static bool FindNestedForgeMethod(
        INamedTypeSymbol forgeClass,
        ITypeSymbol sourceType,
        ITypeSymbol destType,
        out string? methodName,
        Microsoft.CodeAnalysis.Compilation? compilation = null,
        IReadOnlyList<string>? includedForgeClasses = null,
        List<Diagnostic>? diagnostics = null,
        string? memberName = null)
    {
        var sourceDisplay = sourceType.ToDisplayString();
        var destDisplay = destType.ToDisplayString();

        // First search in the current forge class
        foreach (var member in forgeClass.GetMembers())
        {
            if (member is IMethodSymbol m &&
                m.IsStatic &&
                m.Parameters.Length == 1 &&
                m.Parameters[0].Type.ToDisplayString() == sourceDisplay &&
                m.ReturnType.ToDisplayString() == destDisplay)
            {
                // Check if it's a partial method (either declaration or has an implementation part)
                if (m.IsPartialDefinition || m.PartialDefinitionPart != null)
                {
                    methodName = m.Name;
                    return true;
                }
            }
        }

        // If not found and included classes are provided, search them in order
        if (compilation != null && includedForgeClasses != null && includedForgeClasses.Count > 0)
        {
            string? firstMatch = null;
            string? firstMatchFqn = null;
            var shadowedMethods = new List<(string Fqn, string MethodName)>();

            foreach (var includedFqn in includedForgeClasses)
            {
                var includedClass = compilation.GetTypeByMetadataName(includedFqn) as INamedTypeSymbol;
                if (includedClass != null)
                {
                    foreach (var member in includedClass.GetMembers())
                    {
                        if (member is IMethodSymbol m &&
                            m.IsStatic &&
                            m.Parameters.Length == 1 &&
                            m.Parameters[0].Type.ToDisplayString() == sourceDisplay &&
                            m.ReturnType.ToDisplayString() == destDisplay)
                        {
                            // Check if it's a partial method (either declaration or has an implementation part)
                            if (m.IsPartialDefinition || m.PartialDefinitionPart != null)
                            {
                                if (firstMatch == null)
                                {
                                    firstMatch = m.Name;
                                    firstMatchFqn = includedFqn;
                                }
                                else
                                {
                                    shadowedMethods.Add((includedFqn, m.Name));
                                }
                            }
                        }
                    }
                }
            }

            // Emit warnings for shadowed methods
            if (shadowedMethods.Count > 0 && diagnostics != null && memberName != null && firstMatchFqn != null)
            {
                foreach (var (shadowedFqn, shadowedName) in shadowedMethods)
                {
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.ShadowedNestedForgeMethod,
                        forgeClass.Locations[0],
                        memberName,
                        $"{sourceDisplay} → {destDisplay}",
                        $"{firstMatchFqn}.{firstMatch}",
                        $"{shadowedFqn}.{shadowedName}"));
                }
            }

            if (firstMatch != null && firstMatchFqn != null)
            {
                // Qualify the method name with the fully qualified class name for included classes
                var includedClass = compilation.GetTypeByMetadataName(firstMatchFqn);
                if (includedClass != null)
                {
                    // Use fully qualified name to handle cross-namespace scenarios
                    var fullyQualifiedClassName = includedClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    methodName = $"{fullyQualifiedClassName}.{firstMatch}";
                }
                else
                {
                    methodName = firstMatch;
                }
                return true;
            }
        }

        methodName = null;
        return false;
    }

    /// <summary>
    /// Resolves a [ForgeMap] Condition method by name: searches the current forge class first,
    /// then falls back to [ForgeUses]-included classes in declaration order. If the name matches
    /// methods in more than one included class, the first is used and <paramref name="shadowedBy"/>
    /// is populated so the caller can emit a diagnostic — resolution is never silent.
    /// </summary>
    private static bool IsValidConditionMethodSignature(IMethodSymbol m, ITypeSymbol sourceType)
    {
        return m.IsStatic
            && m.Parameters.Length == 1
            && m.Parameters[0].Type.ToDisplayString() == sourceType.ToDisplayString()
            && m.ReturnType.ToDisplayString() == "bool"
            && (m.DeclaredAccessibility == Accessibility.Public || m.DeclaredAccessibility == Accessibility.Internal);
    }

    private static IMethodSymbol? ResolveConditionMethod(
        INamedTypeSymbol forgeClass,
        string conditionMethodName,
        Microsoft.CodeAnalysis.Compilation compilation,
        IReadOnlyList<string>? includedForgeClasses,
        ITypeSymbol sourceType,
        out string? qualifiedMethodName,
        out string? shadowedBy)
    {
        qualifiedMethodName = null;
        shadowedBy = null;

        var localMatch = forgeClass.GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.Name == conditionMethodName && IsValidConditionMethodSignature(m, sourceType));
        if (localMatch != null)
        {
            qualifiedMethodName = conditionMethodName;
            return localMatch;
        }

        // If there's a name match locally but it didn't pass the signature filter, fall through
        // to included classes — a valid method there should still be found.

        if (includedForgeClasses == null || includedForgeClasses.Count == 0)
        {
            // Check if there's a name match with invalid signature to give a better diagnostic
            var invalidLocal = forgeClass.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Name == conditionMethodName);
            return invalidLocal;
        }

        IMethodSymbol? firstMatch = null;

        foreach (var includedFqn in includedForgeClasses)
        {
            var includedClass = compilation.GetTypeByMetadataName(includedFqn);
            if (includedClass == null) continue;

            var candidate = includedClass.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Name == conditionMethodName && IsValidConditionMethodSignature(m, sourceType));
            if (candidate == null) continue;

            if (firstMatch == null)
            {
                firstMatch = candidate;
                qualifiedMethodName = $"{includedClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{conditionMethodName}";
            }
            else
            {
                shadowedBy = $"{includedClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{conditionMethodName}";
            }
        }

        if (firstMatch != null)
            return firstMatch;

        // No valid match anywhere — return first name match (from local or included) for diagnostics
        var fallbackLocal = forgeClass.GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.Name == conditionMethodName);
        if (fallbackLocal != null)
            return fallbackLocal;

        foreach (var includedFqn in includedForgeClasses)
        {
            var includedClass = compilation.GetTypeByMetadataName(includedFqn);
            if (includedClass == null) continue;

            var fallback = includedClass.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Name == conditionMethodName);
            if (fallback != null)
                return fallback;
        }

        return null;
    }

    /// <summary>
    /// Checks if a method has the required shape for a forge mapping method.
    /// Valid shapes: (1) create: static partial method with non-void return and one parameter,
    /// (2) update: static partial method with void return and two parameters. Generic methods are excluded.
    /// </summary>
    private static bool IsForgeMethodShape(IMethodSymbol method)
    {
        if (!method.IsStatic || !method.IsPartialDefinition || method.TypeParameters.Length != 0)
            return false;

        // Create shape: non-void return, 1 parameter
        if (!method.ReturnsVoid && method.Parameters.Length == 1)
            return true;

        // Update shape: void return, 2 parameters
        if (method.ReturnsVoid && method.Parameters.Length == 2)
            return true;

        return false;
    }

    /// <summary>
    /// Checks if a method is an update (in-place modification) method.
    /// Update methods must be static partial, return void, accept two parameters (source and destination),
    /// and not be generic.
    /// </summary>
    private static bool IsUpdateMethodShape(IMethodSymbol method)
    {
        return method.IsStatic &&
               method.IsPartialDefinition &&
               method.ReturnsVoid &&
               method.Parameters.Length == 2 &&
               method.TypeParameters.Length == 0;
    }

    // ─── Converter Helpers ──────────────────────────────────────────────────

    private static bool FindConverterMethod(
        INamedTypeSymbol forgeClass,
        ITypeSymbol sourceType,
        ITypeSymbol destType,
        out string? converterName,
        Microsoft.CodeAnalysis.Compilation? compilation = null,
        IReadOnlyList<string>? includedForgeClasses = null)
    {
        converterName = null;
        var srcDisplay = sourceType.ToDisplayString();
        var destDisplay = destType.ToDisplayString();

        // First search in the current forge class (allow private converters on the same class)
        foreach (var member in forgeClass.GetMembers())
        {
            if (member is IMethodSymbol m &&
                m.IsStatic &&
                !m.ReturnsVoid &&
                m.Parameters.Length == 1 &&
                m.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeConverterAttribute") &&
                m.Parameters[0].Type.ToDisplayString() == srcDisplay &&
                m.ReturnType.ToDisplayString() == destDisplay)
            {
                converterName = m.Name;
                return true;
            }
        }

        // If not found and included classes are provided, search them in order
        if (compilation != null && includedForgeClasses != null && includedForgeClasses.Count > 0)
        {
            foreach (var includedFqn in includedForgeClasses)
            {
                var includedClass = compilation.GetTypeByMetadataName(includedFqn) as INamedTypeSymbol;
                if (includedClass != null)
                {
                    foreach (var member in includedClass.GetMembers())
                    {
                        if (member is IMethodSymbol m &&
                            m.IsStatic &&
                            !m.ReturnsVoid &&
                            m.Parameters.Length == 1 &&
                            IsMemberAccessibleFromStaticContext(m, forgeClass.ContainingAssembly) &&
                            m.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeConverterAttribute") &&
                            m.Parameters[0].Type.ToDisplayString() == srcDisplay &&
                            m.ReturnType.ToDisplayString() == destDisplay)
                        {
                            converterName = m.Name;
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if an implicit conversion exists from sourceType to destType.
    /// Sets isLossy to true if the conversion may lose precision (e.g., float→double).
    /// Returns false if no implicit conversion is available or if the conversion is explicit-only.
    /// </summary>
    private static bool TryImplicitConversion(
        Microsoft.CodeAnalysis.Compilation compilation,
        ITypeSymbol sourceType,
        ITypeSymbol destType,
        out bool isLossy)
    {
        isLossy = false;

        // Get conversion using Roslyn's API
        var conversion = compilation.ClassifyConversion(sourceType, destType);

        // Only allow implicit conversions
        if (!conversion.IsImplicit)
            return false;

        // Determine if the conversion is lossy
        // Lossy conversions are those where precision or data can be lost
        // Examples: float→double→decimal, double→decimal, etc.
        isLossy = IsLossyConversion(sourceType, destType);

        return true;
    }

    /// <summary>
    /// Determines if a conversion may lose precision or data (e.g., float→double).
    /// Must stay in sync with ForgeAnalyzer.IsLossyConversion to ensure consistent FKF203 diagnostics.
    /// </summary>
    private static bool IsLossyConversion(ITypeSymbol sourceType, ITypeSymbol destType)
    {
        var srcName = sourceType.ToDisplayString();
        var destName = destType.ToDisplayString();

        // Note: This method must stay in sync with ForgeAnalyzer.IsLossyConversion
        // to ensure FKF203 diagnostic is produced consistently in both analyzer and generator paths.

        // float→double is considered lossy (precision consideration)
        if (srcName == "float" && destName == "double")
            return true;

        // int/uint→float is lossy (24-bit mantissa limits precision)
        if ((srcName == "int" || srcName == "uint") && destName == "float")
            return true;

        // long/ulong→float/double is lossy (precision loss)
        if ((srcName == "long" || srcName == "ulong") && (destName == "float" || destName == "double"))
            return true;

        return false;
    }

    private static void GenerateExtensionMethod(StringBuilder sb, ForgeMethodModel method, string forgeClassName, string indent)
    {
        if (method.MethodKind == ForgeMethodKind.CollectionProject || method.MethodKind == ForgeMethodKind.DictionaryProject)
            return;

        sb.AppendLine();
        sb.AppendLine($"{indent}/// <summary>Extension method forwarder to <see cref=\"{forgeClassName}.{method.MethodName}\"/>. Auto-generated by FreakyKit.Forge.</summary>");
        sb.AppendLine($"{indent}[GeneratedCode(\"FreakyKit.Forge.Generator\", \"1.0.0\")]");
        sb.AppendLine($"{indent}[DebuggerStepThrough]");

        if (method.MethodKind == ForgeMethodKind.Update)
        {
            sb.AppendLine($"{indent}public static void {method.MethodName}(this {method.SourceTypeShortName} {method.SourceParameterName}, {method.DestTypeShortName} {method.DestParameterName})");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    {forgeClassName}.{method.MethodName}({method.SourceParameterName}, {method.DestParameterName});");
            sb.AppendLine($"{indent}}}");
        }
        else
        {
            sb.AppendLine($"{indent}public static {method.DestTypeShortName} {method.MethodName}(this {method.SourceTypeShortName} {method.SourceParameterName})");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    return {forgeClassName}.{method.MethodName}({method.SourceParameterName});");
            sb.AppendLine($"{indent}}}");
        }
    }

    // ─── Collection Helpers ─────────────────────────────────────────────────

    private static bool TryResolveCollectionMapping(
        ITypeSymbol srcType, ITypeSymbol destType,
        INamedTypeSymbol forgeClass, bool allowNested,
        string sourceParamName, string srcMemberName,
        out string expression,
        out CollectionMappingInfo? info,
        Microsoft.CodeAnalysis.Compilation? compilation = null,
        IReadOnlyList<string>? includedForgeClasses = null,
        List<Diagnostic>? diagnostics = null)
    {
        expression = "";
        info = null;
        var paramName = sourceParamName;

        var srcElem = GetCollectionElementType(srcType);
        var destElem = GetCollectionElementType(destType);
        if (srcElem == null || destElem == null) return false;

        var srcAccessor = $"{paramName}.{srcMemberName}";

        // Determine the LINQ materialization suffix
        string suffix;
        if (destType is IArrayTypeSymbol)
            suffix = ".ToArray()";
        else if (IsHashSetType(destType))
            suffix = ".ToHashSet()";
        else if (IsImmutableArrayType(destType))
            suffix = ".ToImmutableArray()";
        else if (IsImmutableListType(destType))
            suffix = ".ToImmutableList()";
        else if (IsImmutableHashSetType(destType))
            suffix = ".ToImmutableHashSet()";
        else if (IsReadOnlyCollectionType(destType))
            suffix = ".ToList().AsReadOnly()";
        else
            suffix = ".ToList()";

        // Null-safe collection mapping: if source collection is a reference type, guard against null
        bool srcIsRefType = srcType.IsReferenceType;
        var nullFallback = destType.IsValueType ? "default" : "null";

        // EF Core translates .ToList() and .ToArray() reliably. Other materializers don't translate;
        // they exclude from the expression property with FKF506 at the caller.
        bool materializerTranslatable = suffix == ".ToList()" || suffix == ".ToArray()";
        var expressionMaterializer = materializerTranslatable ? suffix : null;

        if (srcElem.ToDisplayString() == destElem.ToDisplayString())
        {
            // Same element type: just materialize. Same C# expression works in both paths.
            if (srcIsRefType)
                expression = $"{srcAccessor} != null ? {srcAccessor}{suffix} : {nullFallback}";
            else
                expression = $"{srcAccessor}{suffix}";

            info = new CollectionMappingInfo(
                elementForgeMethod: null,
                sourceAccessor: srcAccessor,
                expressionMaterializer: expressionMaterializer,
                destinationSuffix: suffix,
                sourceIsRefType: srcIsRefType,
                sameElementType: true);
            return true;
        }

        // Different element types: check for nested forge
        if (allowNested && FindNestedForgeMethod(forgeClass, srcElem, destElem, out var nestedName, compilation, includedForgeClasses, diagnostics, srcMemberName) && nestedName != null)
        {
            if (srcIsRefType)
                expression = $"{srcAccessor} != null ? {srcAccessor}.Select(x => {nestedName}(x)){suffix} : {nullFallback}";
            else
                expression = $"{srcAccessor}.Select(x => {nestedName}(x)){suffix}";

            info = new CollectionMappingInfo(
                elementForgeMethod: nestedName,
                sourceAccessor: srcAccessor,
                expressionMaterializer: expressionMaterializer,
                destinationSuffix: suffix,
                sourceIsRefType: srcIsRefType,
                sameElementType: false);
            return true;
        }

        // Check for [ForgeConverter] method as fallback
        if (FindConverterMethod(forgeClass, srcElem, destElem, out var converterName, compilation, includedForgeClasses) && converterName != null)
        {
            if (srcIsRefType)
                expression = $"{srcAccessor} != null ? {srcAccessor}.Select(x => {converterName}(x)){suffix} : {nullFallback}";
            else
                expression = $"{srcAccessor}.Select(x => {converterName}(x)){suffix}";

            info = new CollectionMappingInfo(
                elementForgeMethod: converterName,
                sourceAccessor: srcAccessor,
                expressionMaterializer: expressionMaterializer,
                destinationSuffix: suffix,
                sourceIsRefType: srcIsRefType,
                sameElementType: false);
            return true;
        }

        return false;
    }

    /// <summary>Metadata about a resolved collection mapping; used by the expression-mode post-pass.</summary>
    private sealed class CollectionMappingInfo
    {
        public string? ElementForgeMethod { get; }
        public string? SourceAccessor { get; }
        public string? ExpressionMaterializer { get; }
        public string DestinationSuffix { get; }
        public bool SourceIsRefType { get; }
        public bool SameElementType { get; }
        public CollectionMappingInfo(string? elementForgeMethod, string? sourceAccessor, string? expressionMaterializer, string destinationSuffix, bool sourceIsRefType, bool sameElementType)
        {
            ElementForgeMethod = elementForgeMethod;
            SourceAccessor = sourceAccessor;
            ExpressionMaterializer = expressionMaterializer;
            DestinationSuffix = destinationSuffix;
            SourceIsRefType = sourceIsRefType;
            SameElementType = sameElementType;
        }
    }

    private static bool GetDictionaryKeyValueTypes(
        ITypeSymbol type,
        out ITypeSymbol? keyType,
        out ITypeSymbol? valueType)
    {
        keyType = null;
        valueType = null;
        if (type is INamedTypeSymbol named && named.IsGenericType && named.TypeArguments.Length == 2)
        {
            var def = named.OriginalDefinition.ToDisplayString();
            if (def == "System.Collections.Generic.Dictionary<TKey, TValue>" ||
                def == "System.Collections.Generic.IDictionary<TKey, TValue>" ||
                def == "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>")
            {
                keyType = named.TypeArguments[0];
                valueType = named.TypeArguments[1];
                return true;
            }
        }
        return false;
    }

    private static bool TryResolveDictionaryMapping(
        ITypeSymbol srcType, ITypeSymbol destType,
        INamedTypeSymbol forgeClass, bool allowNested,
        string sourceParamName, string srcMemberName,
        out string expression,
        Microsoft.CodeAnalysis.Compilation? compilation = null,
        IReadOnlyList<string>? includedForgeClasses = null,
        List<Diagnostic>? diagnostics = null)
    {
        expression = "";
        if (!GetDictionaryKeyValueTypes(srcType, out var srcKey, out var srcVal)) return false;
        if (!GetDictionaryKeyValueTypes(destType, out var destKey, out var destVal)) return false;
        if (srcKey!.ToDisplayString() != destKey!.ToDisplayString()) return false;

        var srcAccessor = $"{sourceParamName}.{srcMemberName}";
        var srcIsRefType = srcType.IsReferenceType;

        if (srcVal!.ToDisplayString() == destVal!.ToDisplayString())
        {
            var expr = $"new {GetConcreteDictShortName(destType, destKey!, destVal)}({srcAccessor})";
            expression = srcIsRefType ? $"{srcAccessor} != null ? {expr} : null" : expr;
            return true;
        }

        if (allowNested && FindNestedForgeMethod(forgeClass, srcVal, destVal!, out var nestedName, compilation, includedForgeClasses, diagnostics, srcMemberName) && nestedName != null)
        {
            var expr = $"{srcAccessor}.ToDictionary(__kvp => __kvp.Key, __kvp => {nestedName}(__kvp.Value))";
            expression = srcIsRefType ? $"{srcAccessor} != null ? {expr} : null" : expr;
            return true;
        }

        // Check for [ForgeConverter] method as fallback
        if (FindConverterMethod(forgeClass, srcVal, destVal!, out var converterName, compilation, includedForgeClasses) && converterName != null)
        {
            var expr = $"{srcAccessor}.ToDictionary(__kvp => __kvp.Key, __kvp => {converterName}(__kvp.Value))";
            expression = srcIsRefType ? $"{srcAccessor} != null ? {expr} : null" : expr;
            return true;
        }

        return false;
    }

    private static ITypeSymbol? GetCollectionElementType(ITypeSymbol type)
    {
        // Array
        if (type is IArrayTypeSymbol arrayType)
            return arrayType.ElementType;

        // Named generic types: List<T>, IList<T>, IEnumerable<T>, ICollection<T>, IReadOnlyList<T>,
        // ImmutableArray<T>, ImmutableList<T>, ReadOnlyCollection<T>, etc.
        if (type is INamedTypeSymbol named && named.IsGenericType && named.TypeArguments.Length == 1)
        {
            var def = named.OriginalDefinition.ToDisplayString();
            if (def.StartsWith("System.Collections.Generic.") ||
                def.StartsWith("System.Collections.Immutable.") ||
                def.StartsWith("System.Collections.ObjectModel.") ||
                def == "System.Collections.Generic.List<T>" ||
                def == "System.Collections.Generic.IList<T>" ||
                def == "System.Collections.Generic.IEnumerable<T>" ||
                def == "System.Collections.Generic.ICollection<T>" ||
                def == "System.Collections.Generic.IReadOnlyList<T>" ||
                def == "System.Collections.Generic.IReadOnlyCollection<T>" ||
                def == "System.Collections.Immutable.ImmutableArray<T>" ||
                def == "System.Collections.Immutable.ImmutableList<T>" ||
                def == "System.Collections.Immutable.IImmutableList<T>" ||
                def == "System.Collections.Immutable.ImmutableHashSet<T>" ||
                def == "System.Collections.Immutable.IImmutableSet<T>" ||
                def == "System.Collections.ObjectModel.ReadOnlyCollection<T>" ||
                def == "System.Collections.ObjectModel.Collection<T>")
            {
                return named.TypeArguments[0];
            }
        }

        // Check if implements IEnumerable<T>
        if (type is INamedTypeSymbol namedType)
        {
            foreach (var iface in namedType.AllInterfaces)
            {
                if (iface.IsGenericType && iface.TypeArguments.Length == 1 &&
                    iface.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>")
                {
                    return iface.TypeArguments[0];
                }
            }
        }

        return null;
    }

    private static bool IsHashSetType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            var def = named.OriginalDefinition.ToDisplayString();
            if (def == "System.Collections.Generic.HashSet<T>" ||
                def == "System.Collections.Generic.ISet<T>")
                return true;
        }
        return false;
    }

    private static bool IsImmutableArrayType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            var def = named.OriginalDefinition.ToDisplayString();
            return def == "System.Collections.Immutable.ImmutableArray<T>";
        }
        return false;
    }

    private static bool IsImmutableListType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            var def = named.OriginalDefinition.ToDisplayString();
            return def == "System.Collections.Immutable.ImmutableList<T>" ||
                   def == "System.Collections.Immutable.IImmutableList<T>";
        }
        return false;
    }

    private static bool IsImmutableHashSetType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            var def = named.OriginalDefinition.ToDisplayString();
            return def == "System.Collections.Immutable.ImmutableHashSet<T>" ||
                   def == "System.Collections.Immutable.IImmutableSet<T>";
        }
        return false;
    }

    private static bool IsReadOnlyCollectionType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            var def = named.OriginalDefinition.ToDisplayString();
            return def == "System.Collections.ObjectModel.ReadOnlyCollection<T>";
        }
        return false;
    }

    /// <summary>
    /// True if the type is a mutable collection (or interface signalling a mutable collection)
    /// such that same-type member assignment should produce a copy-constructor expression by
    /// default rather than direct reference assignment.
    ///
    /// Immutable types (ImmutableArray, ImmutableList, ImmutableHashSet, IImmutableList, IImmutableSet)
    /// return false — sharing references is safe for them.
    /// </summary>
    private static bool IsMutableSameTypeCollection(ITypeSymbol type)
    {
        // Arrays are mutable (elements can be reassigned)
        if (type is IArrayTypeSymbol) return true;

        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            var def = named.OriginalDefinition.ToDisplayString();
            return def switch
            {
                "System.Collections.Generic.List<T>" => true,
                "System.Collections.Generic.IList<T>" => true,
                "System.Collections.Generic.ICollection<T>" => true,
                "System.Collections.Generic.IEnumerable<T>" => true,
                "System.Collections.Generic.IReadOnlyList<T>" => true,
                "System.Collections.Generic.IReadOnlyCollection<T>" => true,
                "System.Collections.Generic.HashSet<T>" => true,
                "System.Collections.Generic.ISet<T>" => true,
                "System.Collections.Generic.Dictionary<TKey, TValue>" => true,
                "System.Collections.Generic.IDictionary<TKey, TValue>" => true,
                "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>" => true,
                "System.Collections.ObjectModel.Collection<T>" => true,
                "System.Collections.ObjectModel.ReadOnlyCollection<T>" => true,
                _ => false,
            };
        }

        return false;
    }

    /// <summary>
    /// Returns the C# expression that copies the value of <paramref name="srcAccessor"/> into a
    /// new instance of the same collection type. Picks the right materializer per type. The
    /// resulting expression assumes the source is non-null; callers are responsible for adding
    /// a null-guard ternary when the source type is a reference type.
    /// </summary>
    private static string BuildSameTypeCollectionCopyExpression(ITypeSymbol type, string srcAccessor)
    {
        // Array: .ToArray() — works for both T[] and any IEnumerable<T> source
        if (type is IArrayTypeSymbol arrayType)
            return $"{srcAccessor}.ToArray()";

        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            var def = named.OriginalDefinition.ToDisplayString();
            var shortName = BuildShortTypeName(type);

            return def switch
            {
                "System.Collections.Generic.List<T>" => $"new {shortName}({srcAccessor})",
                "System.Collections.Generic.IList<T>" => $"new List<{GetCSharpKeyword(named.TypeArguments[0])}>({srcAccessor})",
                "System.Collections.Generic.ICollection<T>" => $"new List<{GetCSharpKeyword(named.TypeArguments[0])}>({srcAccessor})",
                "System.Collections.Generic.IEnumerable<T>" => $"new List<{GetCSharpKeyword(named.TypeArguments[0])}>({srcAccessor})",
                "System.Collections.Generic.IReadOnlyList<T>" => $"new List<{GetCSharpKeyword(named.TypeArguments[0])}>({srcAccessor})",
                "System.Collections.Generic.IReadOnlyCollection<T>" => $"new List<{GetCSharpKeyword(named.TypeArguments[0])}>({srcAccessor})",
                "System.Collections.Generic.HashSet<T>" => $"new {shortName}({srcAccessor})",
                "System.Collections.Generic.ISet<T>" => $"new HashSet<{GetCSharpKeyword(named.TypeArguments[0])}>({srcAccessor})",
                "System.Collections.Generic.Dictionary<TKey, TValue>" => $"new {shortName}({srcAccessor})",
                "System.Collections.Generic.IDictionary<TKey, TValue>" => $"new Dictionary<{GetCSharpKeyword(named.TypeArguments[0])}, {GetCSharpKeyword(named.TypeArguments[1])}>({srcAccessor})",
                "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>" => $"new Dictionary<{GetCSharpKeyword(named.TypeArguments[0])}, {GetCSharpKeyword(named.TypeArguments[1])}>({srcAccessor})",
                "System.Collections.ObjectModel.Collection<T>" => $"new {shortName}(new List<{GetCSharpKeyword(named.TypeArguments[0])}>({srcAccessor}))",
                "System.Collections.ObjectModel.ReadOnlyCollection<T>" => $"new {shortName}(new List<{GetCSharpKeyword(named.TypeArguments[0])}>({srcAccessor}))",
                _ => srcAccessor, // Fallback: should not happen if caller checks IsMutableSameTypeCollection first
            };
        }

        // Fallback: direct assignment (e.g. unknown types)
        return srcAccessor;
    }

    private static bool HasForgeIgnoreAttribute(ISymbol member)
        => ShouldIgnoreMember(member, isSourceSide: true) || ShouldIgnoreMember(member, isSourceSide: false);

    private static bool ShouldIgnoreMember(ISymbol member, bool isSourceSide)
    {
        var attr = member.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeIgnoreAttribute");
        if (attr == null) return false;

        // Read the Side named argument (default = Both = 0)
        var sideArg = attr.NamedArguments.FirstOrDefault(a => a.Key == "Side");
        int side = sideArg.Key != null && sideArg.Value.Value is int sv ? sv : 0;

        // Both(0): always ignore; Source(1): ignore only on source side; Destination(2): ignore only on dest side
        return side == 0
            || (side == 1 && isSourceSide)
            || (side == 2 && !isSourceSide);
    }

    private static string? GetForgeMapName(ISymbol member)
    {
        var attr = member.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeMapAttribute");
        if (attr != null && attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments[0].Value is string name)
            return name;
        return null;
    }

    private static object? GetForgeDefaultValue(ISymbol member)
    {
        var attr = member.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeMapAttribute");
        if (attr == null) return null;
        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg.Key == "DefaultValue" && !namedArg.Value.IsNull)
                return namedArg.Value.Value;
        }
        return null;
    }

    /// <summary>
    /// Reads <c>IgnoreIfNull</c> from <c>[ForgeMap]</c> on the given symbol. Returns:
    ///   - <c>true</c> if the attribute is present AND IgnoreIfNull was explicitly set to True
    ///   - <c>false</c> if the attribute is present AND IgnoreIfNull was explicitly set to False
    ///   - <c>null</c> if the attribute is absent OR IgnoreIfNull was set to Inherit
    /// The null case means "no opinion, inherit from method-level setting."
    /// </summary>
    private static bool? GetForgeIgnoreIfNull(ISymbol member)
    {
        var attr = member.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeMapAttribute");
        if (attr == null) return null;
        return GetForgePolicyValue(attr, "IgnoreIfNull");
    }

    /// <summary>
    /// Reads <c>ShareReference</c> from <c>[ForgeMap]</c> on the given symbol. Returns:
    ///   - <c>true</c> if the attribute is present AND ShareReference was explicitly set to True
    ///   - <c>false</c> if the attribute is present AND ShareReference was explicitly set to False
    ///   - <c>null</c> if the attribute is absent OR ShareReference was set to Inherit
    /// The null case means "no opinion, inherit from method/default."
    /// </summary>
    private static bool? GetForgeMapShareReference(ISymbol? member)
    {
        if (member == null) return null;
        var attr = member.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeMapAttribute");
        if (attr == null) return null;
        return GetForgePolicyValue(attr, "ShareReference");
    }

    /// <summary>
    /// Reads a ForgePolicy-typed property from an attribute and returns:
    ///   - true if value is ForgePolicy.True (1)
    ///   - false if value is ForgePolicy.False (2)
    ///   - null if value is ForgePolicy.Inherit (0) or not found
    /// </summary>
    private static bool? GetForgePolicyValue(AttributeData attr, string propertyName)
    {
        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg.Key == propertyName && namedArg.Value.Value is int policyValue)
            {
                return policyValue switch
                {
                    0 => null,  // Inherit
                    1 => true,  // True
                    2 => false, // False
                    _ => null
                };
            }
        }
        return null;
    }

    private static int GetForgeMapNullFallback(ISymbol? member)
    {
        if (member == null) return 0; // NullFallback.Null
        var attr = member.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeMapAttribute");
        if (attr == null) return 0;
        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg.Key == "NullFallback" && namedArg.Value.Value is int intVal)
                return intVal;
        }
        return 0;
    }

    private static string FormatLiteral(object value)
    {
        return value switch
        {
            string s => $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"",
            bool b => b ? "true" : "false",
            char c => $"'{c}'",
            float f => $"{f}f",
            double d => $"{d}d",
            decimal m => $"{m}m",
            long l => $"{l}L",
            ulong ul => $"{ul}UL",
            Enum e => $"{e.GetType().Name}.{e}",
            _ => value.ToString()
        };
    }

    private static bool HasForgeAttribute(IMethodSymbol method)
    {
        return method.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeMethodAttribute");
    }

    private static AttributeData? GetForgeAttribute(IMethodSymbol method)
    {
        return method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeMethodAttribute");
    }

    private static AttributeData? GetForgeDictionaryAttribute(IMethodSymbol method)
    {
        return method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeDictionaryAttribute");
    }

    private static (int KeyCasing, int MissingKey, int NullValue) GetDictionaryPolicies(AttributeData? attr)
    {
        int keyCasing = 0; // KeyCasingPolicy.Exact
        int missingKey = 0; // MissingKeyPolicy.Throw
        int nullValue = 0; // NullValuePolicy.Include

        if (attr != null)
        {
            foreach (var namedArg in attr.NamedArguments)
            {
                if (namedArg.Key == "KeyCasing" && namedArg.Value.Value is int kc)
                    keyCasing = kc;
                else if (namedArg.Key == "MissingKey" && namedArg.Value.Value is int mk)
                    missingKey = mk;
                else if (namedArg.Key == "NullValue" && namedArg.Value.Value is int nv)
                    nullValue = nv;
            }
        }

        return (keyCasing, missingKey, nullValue);
    }

    private static bool HasImplementationBody(IMethodSymbol method, System.Threading.CancellationToken ct)
    {
        foreach (var syntaxRef in method.DeclaringSyntaxReferences)
        {
            var syntax = syntaxRef.GetSyntax(ct);
            if (syntax is MethodDeclarationSyntax mds)
            {
                if (mds.Body != null || mds.ExpressionBody != null)
                    return true;
            }
        }
        return false;
    }

    private static GeneratorForgeMode GetForgeMode(AttributeData attr)
    {
        var namedArg = attr.NamedArguments.FirstOrDefault(a => a.Key == "Mode");
        if (namedArg.Value.Value is int val)
            return (GeneratorForgeMode)val;
        return GeneratorForgeMode.Implicit;
    }

    private static bool GetBoolNamedArg(AttributeData attr, string name)
    {
        var namedArg = attr.NamedArguments.FirstOrDefault(a => a.Key == name);
        if (namedArg.Value.Value is bool val)
            return val;
        return false;
    }

    /// <summary>
    /// Reads a ForgePolicy-typed property from an attribute and returns:
    ///   - true if value is ForgePolicy.True (1)
    ///   - false if value is ForgePolicy.False (2) or ForgePolicy.Inherit (0)
    /// Defaults to false if the property is not found or is Inherit.
    /// </summary>
    private static bool GetForgePolicyAsBoolean(AttributeData attr, string propertyName)
    {
        var policy = GetForgePolicyValue(attr, propertyName);
        return policy ?? false;
    }

    private static int GetEnumMappingStrategy(AttributeData? attr)
    {
        if (attr is null) return 0;
        var namedArg = attr.NamedArguments.FirstOrDefault(a => a.Key == "MappingStrategy");
        if (namedArg.Value.Value is int val)
            return val;
        return 0; // Cast
    }

    private static string? GetStringNamedArg(AttributeData attr, string name)
    {
        var namedArg = attr.NamedArguments.FirstOrDefault(a => a.Key == name);
        if (namedArg.Value.Value is string val)
            return val;
        return null;
    }

    private static string AccessibilityToString(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public",
        Accessibility.Internal => "internal",
        Accessibility.Private => "private",
        Accessibility.Protected => "protected",
        Accessibility.ProtectedOrInternal => "protected internal",
        Accessibility.ProtectedAndInternal => "private protected",
        _ => "public"
    };

    private static List<ContainingTypeInfo> BuildContainingTypeChain(INamedTypeSymbol type)
    {
        var chain = new List<ContainingTypeInfo>();
        var current = type.ContainingType;
        while (current != null)
        {
            var keyword = current.IsRecord
                ? (current.IsValueType ? "record struct" : "record class")
                : (current.IsValueType ? "struct" : "class");
            chain.Add(new ContainingTypeInfo(
                AccessibilityToString(current.DeclaredAccessibility),
                keyword,
                current.Name));
            current = current.ContainingType;
        }
        chain.Reverse(); // outermost first
        return chain;
    }

    // ─── Nullable Helpers ─────────────────────────────────────────────────────

    private enum NullableConversionKind
    {
        /// <summary>Nullable&lt;T&gt; → T: use .Value</summary>
        UnwrapValue,
        /// <summary>T → Nullable&lt;T&gt; or reference-type nullability difference: direct assignment</summary>
        Direct
    }

    /// <summary>
    /// Checks if source and destination types are nullable-compatible.
    /// Returns true if they differ only in nullability.
    /// </summary>
    private static bool TryResolveNullableMapping(ITypeSymbol srcType, ITypeSymbol destType, out NullableConversionKind kind)
    {
        kind = NullableConversionKind.Direct;

        // Case 1: Nullable<T> → T (value type unwrap)
        if (srcType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            srcType is INamedTypeSymbol srcNullable &&
            srcNullable.TypeArguments.Length == 1)
        {
            var innerType = srcNullable.TypeArguments[0];
            if (innerType.ToDisplayString() == destType.ToDisplayString())
            {
                kind = NullableConversionKind.UnwrapValue;
                return true;
            }
        }

        // Case 2: T → Nullable<T> (value type wrap)
        if (destType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            destType is INamedTypeSymbol destNullable &&
            destNullable.TypeArguments.Length == 1)
        {
            var innerType = destNullable.TypeArguments[0];
            if (innerType.ToDisplayString() == srcType.ToDisplayString())
            {
                kind = NullableConversionKind.Direct;
                return true;
            }
        }

        // Case 3: Reference type nullability annotation difference (string vs string?)
        // Compare without nullable annotations
        if (srcType.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString() ==
            destType.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString())
        {
            if (srcType.ToDisplayString() != destType.ToDisplayString())
            {
                kind = NullableConversionKind.Direct;
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveEnumStringMapping(
        ITypeSymbol srcType,
        ITypeSymbol destType,
        string srcParamName,
        string srcMemberName,
        ISymbol? srcSymbol,
        ISymbol? destSymbol,
        out string expr)
    {
        expr = "";

        bool srcIsEnum = srcType.TypeKind == TypeKind.Enum;
        bool srcIsString = srcType.SpecialType == SpecialType.System_String;
        bool destIsEnum = destType.TypeKind == TypeKind.Enum;
        bool destIsString = destType.SpecialType == SpecialType.System_String;

        // Case 1: Enum → String
        if (srcIsEnum && destIsString)
        {
            expr = $"{srcParamName}.{srcMemberName}.ToString()";
            return true;
        }

        // Case 2: String → Enum
        if (srcIsString && destIsEnum)
        {
            var destEnumType = (INamedTypeSymbol)destType;
            var destEnumName = destEnumType.Name;

            // Check for DefaultValue on destSymbol (from [ForgeMap])
            var defaultValue = destSymbol != null ? GetForgeDefaultValue(destSymbol) : null;

            if (defaultValue != null)
            {
                // Use TryParse with fallback: Enum.TryParse<Dest>(..., out var __result) ? __result : fallback
                var literal = FormatLiteral(defaultValue);
                expr = $"(System.Enum.TryParse<{destEnumName}>({srcParamName}.{srcMemberName}, out var __parsed) ? __parsed : {literal})";
            }
            else
            {
                // Use Parse which throws on invalid
                expr = $"System.Enum.Parse<{destEnumName}>({srcParamName}.{srcMemberName})";
            }
            return true;
        }

        return false;
    }

    // ─── Internal Types ───────────────────────────────────────────────────────

    private enum GeneratorForgeMode { Implicit = 0, Explicit = 1 }

    private sealed class ForgeClassResult
    {
        public static readonly ForgeClassResult Empty = new(null, System.Array.Empty<Diagnostic>(), hasErrors: false);

        public ForgeClassModel? ClassModel { get; }
        public IReadOnlyList<Diagnostic> Diagnostics { get; }
        public bool HasErrors { get; }

        public ForgeClassResult(ForgeClassModel? classModel, IReadOnlyList<Diagnostic> diagnostics, bool hasErrors)
        {
            ClassModel = classModel;
            Diagnostics = diagnostics;
            HasErrors = hasErrors;
        }
    }

    private static bool GetForgeIgnoreIfDefault(ISymbol? member)
    {
        if (member == null) return false;
        var attr = member.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeMapAttribute");
        if (attr == null) return false;
        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg.Key == "IgnoreIfDefault" && namedArg.Value.Value is bool b)
                return b;
        }
        return false;
    }

    private static string? GetForgeConditionMethod(ISymbol? member)
    {
        if (member == null) return null;
        var attr = member.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeMapAttribute");
        if (attr == null) return null;
        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg.Key == "Condition" && namedArg.Value.Value is string s)
                return s;
        }
        return null;
    }

    private static (IReadOnlyList<string> IncludedFqns, List<Diagnostic> Diagnostics) ExtractAndValidateForgeUses(
        INamedTypeSymbol forgeClass,
        Microsoft.CodeAnalysis.Compilation compilation,
        List<Diagnostic> parentDiagnostics)
    {
        var diagnostics = new List<Diagnostic>();
        var includedFqns = new List<string>();

        var forgeUsesAttr = forgeClass.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeUsesAttribute");

        if (forgeUsesAttr == null)
            return (includedFqns, diagnostics);

        // Extract the Type[] from the constructor argument (params)
        if (forgeUsesAttr.ConstructorArguments.Length > 0)
        {
            var arg = forgeUsesAttr.ConstructorArguments[0];
            // The argument is an array of TypedConstants
            if (arg.Kind == TypedConstantKind.Array)
            {
                foreach (var typeValue in arg.Values)
                {
                    if (typeValue.Value is INamedTypeSymbol includedType)
                    {
                        var includedFqn = includedType.ToDisplayString();
                        var forgeClassFqn = forgeClass.ToDisplayString();

                        // Check if included class exists and is a forge
                        var includedSymbol = compilation.GetTypeByMetadataName(includedFqn);
                        if (includedSymbol == null)
                        {
                            diagnostics.Add(Diagnostic.Create(
                                ForgeDiagnostics.IncludedForgeClassNotFound,
                                forgeClass.Locations[0],
                                includedFqn));
                            continue;
                        }

                        // Check if included class has [Forge]
                        var hasForgeAttr = includedSymbol.GetAttributes()
                            .Any(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeAttribute");
                        if (!hasForgeAttr)
                        {
                            diagnostics.Add(Diagnostic.Create(
                                ForgeDiagnostics.IncludedClassNotForge,
                                forgeClass.Locations[0],
                                includedFqn));
                            continue;
                        }

                        // Check for direct self-include
                        if (includedFqn == forgeClassFqn)
                        {
                            diagnostics.Add(Diagnostic.Create(
                                ForgeDiagnostics.CircularForgeIncludes,
                                forgeClass.Locations[0],
                                $"{forgeClass.Name} → {forgeClass.Name}"));
                            continue;
                        }

                        // Check for transitive cycles (A uses B uses A, etc.)
                        if (DetectCircularForgeUses(forgeClassFqn, includedFqn, compilation, new HashSet<string>(), out var cycle))
                        {
                            diagnostics.Add(Diagnostic.Create(
                                ForgeDiagnostics.CircularForgeIncludes,
                                forgeClass.Locations[0],
                                string.Join(" → ", cycle)));
                            continue;
                        }

                        includedFqns.Add(includedFqn);
                    }
                }
            }
        }

        return (includedFqns, diagnostics);
    }

    /// <summary>
    /// Detects transitive cycles in ForgeUses relationships.
    /// Returns true if target's ForgeUses includes origin (directly or transitively).
    /// </summary>
    private static bool DetectCircularForgeUses(
        string origin,
        string target,
        Microsoft.CodeAnalysis.Compilation compilation,
        HashSet<string> visited,
        out List<string> cycle)
    {
        cycle = new List<string>();

        if (visited.Contains(target))
            return false;

        visited.Add(target);

        var targetSymbol = compilation.GetTypeByMetadataName(target);
        if (targetSymbol == null)
            return false;

        var forgeUsesAttr = targetSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeUsesAttribute");

        if (forgeUsesAttr == null)
            return false;

        // Check target's ForgeUses for origin
        if (forgeUsesAttr.ConstructorArguments.Length > 0)
        {
            var arg = forgeUsesAttr.ConstructorArguments[0];
            if (arg.Kind == TypedConstantKind.Array)
            {
                foreach (var typeValue in arg.Values)
                {
                    if (typeValue.Value is INamedTypeSymbol includedType)
                    {
                        var includedFqn = includedType.ToDisplayString();

                        // Found direct back-edge to origin
                        if (includedFqn == origin)
                        {
                            cycle.Add(origin);
                            cycle.Add(target);
                            cycle.Add(origin);
                            return true;
                        }

                        // Recurse to check transitive relationships
                        if (DetectCircularForgeUses(origin, includedFqn, compilation, visited, out var innerCycle))
                        {
                            cycle = innerCycle;
                            // Insert current node into the path
                            if (cycle.Count > 0 && cycle[0] == origin)
                                cycle.Insert(1, target);
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Detects classes with [ForgeUses] but missing [Forge] attribute.
    /// Returns a diagnostic if the class has [ForgeUses] but no [Forge], otherwise null.
    /// </summary>
    private static Diagnostic? DetectForgeUsesMissingForge(
        GeneratorAttributeSyntaxContext ctx,
        System.Threading.CancellationToken ct)
    {
        var type = ctx.TargetSymbol as INamedTypeSymbol;
        if (type is null) return null;

        // Check if class has [Forge] attribute
        var hasForgeAttr = type.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeAttribute");

        // If it already has [Forge], no error
        if (hasForgeAttr) return null;

        // Has [ForgeUses] but no [Forge] — emit FKF524
        return Diagnostic.Create(
            ForgeDiagnostics.ForgeUsesMissingForgeAttribute,
            type.Locations.FirstOrDefault(),
            type.Name);
    }

    /// <summary>
    /// Detects methods with [ForgeMethod] but missing [Forge] on the containing class.
    /// Returns a diagnostic if the method has [ForgeMethod] but its containing class lacks [Forge], otherwise null.
    /// </summary>
    private static Diagnostic? DetectForgeMethodWithoutForge(
        GeneratorAttributeSyntaxContext ctx,
        System.Threading.CancellationToken ct)
    {
        var method = ctx.TargetSymbol as IMethodSymbol;
        if (method is null) return null;

        var containingType = method.ContainingType;
        if (containingType is null) return null;

        // Check if containing class has [Forge] attribute
        var hasForgeAttr = containingType.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeAttribute");

        // If it already has [Forge], no error
        if (hasForgeAttr) return null;

        // Has [ForgeMethod] but containing class lacks [Forge] — emit FKF525
        return Diagnostic.Create(
            ForgeDiagnostics.ForgeMethodWithoutForgeClass,
            method.Locations.FirstOrDefault(),
            method.Name);
    }

    /// <summary>
    /// Detects methods with [ForgeConverter] but missing [Forge] on the containing class.
    /// Returns a diagnostic if the method has [ForgeConverter] but its containing class lacks [Forge], otherwise null.
    /// </summary>
    private static Diagnostic? DetectForgeConverterWithoutForge(
        GeneratorAttributeSyntaxContext ctx,
        System.Threading.CancellationToken ct)
    {
        var method = ctx.TargetSymbol as IMethodSymbol;
        if (method is null) return null;

        var containingType = method.ContainingType;
        if (containingType is null) return null;

        // Check if containing class has [Forge] attribute
        var hasForgeAttr = containingType.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeAttribute");

        // If it already has [Forge], no error
        if (hasForgeAttr) return null;

        // Has [ForgeConverter] but containing class lacks [Forge] — emit FKF526
        return Diagnostic.Create(
            ForgeDiagnostics.ForgeConverterWithoutForgeClass,
            method.Locations.FirstOrDefault(),
            method.Name);
    }

    /// <summary>
    /// Detects members with [ForgeMap] on non-destination types.
    /// [ForgeMap] only affects destination type members; if found on source types, it has no effect.
    /// Returns a diagnostic if the member's containing type is not used as a destination, otherwise null.
    /// </summary>
    private static Diagnostic? DetectForgeMapOnSourceMember(
        GeneratorAttributeSyntaxContext ctx,
        System.Threading.CancellationToken ct)
    {
        var member = ctx.TargetSymbol as ISymbol;
        if (member is null) return null;

        var containingType = (member as IPropertySymbol)?.ContainingType
            ?? (member as IFieldSymbol)?.ContainingType;

        if (containingType is null) return null;

        // [ForgeMap] is only meaningful on destination types in forge operations.
        // Warn if found on members of types that aren't used as destinations.
        // For simplicity, warn if the attribute is found on a member outside of a known forge context.
        // This catches the common mistake of putting [ForgeMap] on source type members instead of destination.

        var memberName = (member as IPropertySymbol)?.Name ?? (member as IFieldSymbol)?.Name ?? "Unknown";

        // Emit FKF527 — [ForgeMap] on member that is likely not a destination type
        return Diagnostic.Create(
            ForgeDiagnostics.ForgeMapOnSourceMember,
            member.Locations.FirstOrDefault(),
            $"{containingType.Name}.{memberName}");
    }

    /// <summary>
    /// Detects members with [ForgeIgnore] on non-destination types.
    /// [ForgeIgnore] only affects destination type members; if found on source types, it has no effect.
    /// Returns a diagnostic if the member's containing type is not used as a destination, otherwise null.
    /// </summary>
    private static Diagnostic? DetectForgeIgnoreOnSourceMember(
        GeneratorAttributeSyntaxContext ctx,
        System.Threading.CancellationToken ct)
    {
        var member = ctx.TargetSymbol as ISymbol;
        if (member is null) return null;

        var containingType = (member as IPropertySymbol)?.ContainingType
            ?? (member as IFieldSymbol)?.ContainingType;

        if (containingType is null) return null;

        // [ForgeIgnore] is only meaningful on destination types in forge operations.
        // Warn if found on members of types that aren't used as destinations.
        // For simplicity, warn if the attribute is found on a member outside of a known forge context.
        // This catches the common mistake of putting [ForgeIgnore] on source type members instead of destination.

        var memberName = (member as IPropertySymbol)?.Name ?? (member as IFieldSymbol)?.Name ?? "Unknown";

        // Emit FKF528 — [ForgeIgnore] on member that is likely not a destination type
        return Diagnostic.Create(
            ForgeDiagnostics.ForgeIgnoreOnSourceMember,
            member.Locations.FirstOrDefault(),
            $"{containingType.Name}.{memberName}");
    }

    private static bool IsSupportedDictionaryValueType(ITypeSymbol type)
    {
        var display = type.ToDisplayString();

        // object is always supported (catch-all type)
        if (display == "object") return true;

        // Primitive types
        var primitives = new[]
        {
            "string", "int", "long", "short", "byte", "sbyte", "float", "double", "decimal",
            "bool", "char", "uint", "ulong", "ushort", "System.Guid", "System.DateTime",
            "System.DateTimeOffset", "System.TimeSpan"
        };
        if (primitives.Contains(display)) return true;

        // Nullable primitive types
        if (type is INamedTypeSymbol named && named.IsGenericType && named.Name == "Nullable")
        {
            var underlyingType = named.TypeArguments.FirstOrDefault();
            if (underlyingType != null) return IsSupportedDictionaryValueType(underlyingType);
        }

        // Enum types
        if (type.TypeKind == TypeKind.Enum) return true;

        return false;
    }
}
