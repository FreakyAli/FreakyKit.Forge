using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
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

            var source = GenerateSource(model.ClassModel);
            spc.AddSource($"{model.ClassModel.FullyQualifiedName.Replace('.', '_').Replace('<', '_').Replace('>', '_')}.Forge.g.cs", SourceText.From(source, Encoding.UTF8));
        });
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

            // Private filter (analyzer handles FKF010)
            if (method.DeclaredAccessibility == Accessibility.Private && !includePrivate) continue;

            // Shape filter
            if (!isCandidate) continue;

            // Body check (analyzer handles FKF020)
            if (HasImplementationBody(method, ct))
            {
                diagnostics.Add(Diagnostic.Create(
                    ForgeDiagnostics.ForgeMethodDeclaresBody,
                    method.Locations.FirstOrDefault(),
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
                        m.Locations.FirstOrDefault(),
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

        // Extract each forge method model
        var methodModels = new List<ForgeMethodModel>();
        foreach (var method in forgeMethods)
        {
            var (methodModel, methodDiags) = ExtractForgeMethod(method, type, ct);
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
            containingTypes: containingTypes);

        return new ForgeClassResult(classModel, diagnostics, hasErrors: false);
    }

    private static (ForgeMethodModel? Model, List<Diagnostic> Diagnostics) ExtractForgeMethod(
        IMethodSymbol method,
        INamedTypeSymbol forgeClass,
        System.Threading.CancellationToken ct)
    {
        var diagnostics = new List<Diagnostic>();

        // Detect update vs create shape
        bool isUpdate = IsUpdateMethodShape(method);
        var methodKind = isUpdate ? ForgeMethodKind.Update : ForgeMethodKind.Create;

        var srcParamName = method.Parameters[0].Name;

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
                    srcElemType, destElemType, srcParamName, diagnostics);
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
                method.Locations.FirstOrDefault(),
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
        bool methodIgnoreIfNull = forgeAttr != null && GetBoolNamedArg(forgeAttr, "IgnoreIfNull");
        int enumMappingStrategy = GetEnumMappingStrategy(forgeAttr);

        if (includeFields)
        {
            diagnostics.Add(Diagnostic.Create(
                ForgeDiagnostics.FieldsEnabled,
                method.Locations.FirstOrDefault(),
                method.Name));
        }

        // Collect source members
        var sourceMembers = CollectMembers(sourceType, includeFields, method, diagnostics, isSourceSide: true);

        // Collect dest members (no FKF400 for dest — only source triggers it)
        var destMembers = CollectMembers(destType, includeFields, null, null, isSourceSide: false);

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
                    method.Locations.FirstOrDefault(),
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
                if (allowFlattening && TryResolveFlattenedMapping(sourceType, key, destMember.Type, srcParamName, out var flattenExpr))
                {
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.FlattenedMapping,
                        method.Locations.FirstOrDefault(),
                        destMember.Name,
                        sourceType.Name,
                        flattenExpr.Replace($"{srcParamName}.", "")));

                    assignments.Add(new MemberAssignmentModel(
                        destMemberName: destMember.Name,
                        sourceExpression: flattenExpr,
                        isInitOnly: initOnly));
                    continue;
                }

                // FKF100: handled by analyzer — generator just skips
                continue;
            }

            matchedSourceKeys.Add(key);

            // Determine IgnoreIfNull: per-member overrides method-level
            var srcSymbolForNull = sourceType.GetMembers().FirstOrDefault(m => m.Name == srcMember.Name);
            var destSymbolForNull = destType.GetMembers().FirstOrDefault(m => m.Name == destMember.Name);
            bool memberIgnoreIfNull = (srcSymbolForNull != null && GetForgeIgnoreIfNull(srcSymbolForNull))
                || (destSymbolForNull != null && GetForgeIgnoreIfNull(destSymbolForNull))
                || methodIgnoreIfNull;
            string? nullCheckExpr = memberIgnoreIfNull ? $"{srcParamName}.{srcMember.Name}" : null;

            if (srcMember.Type.ToDisplayString() == destMember.Type.ToDisplayString())
            {
                // Exact type match
                assignments.Add(new MemberAssignmentModel(
                    destMemberName: destMember.Name,
                    sourceExpression: $"{srcParamName}.{srcMember.Name}",
                    ignoreIfNull: memberIgnoreIfNull,
                    nullCheckExpression: nullCheckExpr,
                    isInitOnly: initOnly));
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
                if (nullableKind == NullableConversionKind.UnwrapValue && defaultValue != null)
                {
                    // Use ?? defaultValue for safe fallback
                    sourceExpr = $"{paramName}.{srcMember.Name} ?? {FormatLiteral(defaultValue)}";
                }
                else if (nullableKind == NullableConversionKind.UnwrapValue)
                {
                    sourceExpr = $"{paramName}.{srcMember.Name}.Value";
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.NullableValueTypeMapping,
                        method.Locations.FirstOrDefault(),
                        key,
                        srcMember.Type.ToDisplayString(),
                        destMember.Type.ToDisplayString()));
                }
                else
                {
                    sourceExpr = $"{paramName}.{srcMember.Name}";
                }

                assignments.Add(new MemberAssignmentModel(
                    destMemberName: destMember.Name,
                    sourceExpression: sourceExpr,
                    ignoreIfNull: memberIgnoreIfNull,
                    nullCheckExpression: nullCheckExpr,
                    isInitOnly: initOnly));
            }
            else if (srcMember.Type.TypeKind == TypeKind.Enum && destMember.Type.TypeKind == TypeKind.Enum)
            {
                // Enum-to-enum mapping
                var paramName = srcParamName;

                if (enumMappingStrategy == 1) // ByName
                {
                    // Generate switch expression mapping by member name
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
                    foreach (var srcField in srcEnumMembers)
                    {
                        if (destMemberNames.Contains(srcField.Name))
                        {
                            switchArms.Add($"{srcEnumType.Name}.{srcField.Name} => {destEnumType.Name}.{srcField.Name}");
                        }
                        else
                        {
                            // FKF212: source enum member missing in destination
                            diagnostics.Add(Diagnostic.Create(
                                ForgeDiagnostics.EnumMemberMissing,
                                method.Locations.FirstOrDefault(),
                                srcField.Name,
                                srcEnumType.Name,
                                destEnumType.Name));
                            switchArms.Add($"{srcEnumType.Name}.{srcField.Name} => throw new InvalidOperationException(\"No mapping for {srcEnumType.Name}.{srcField.Name}\")");
                        }
                    }

                    switchArms.Add($"_ => throw new InvalidOperationException($\"Unknown enum value: {{{paramName}.{srcMember.Name}}}\")");

                    var switchExpr = $"{paramName}.{srcMember.Name} switch {{ {string.Join(", ", switchArms)} }}";

                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.EnumNameMapping,
                        method.Locations.FirstOrDefault(),
                        key,
                        srcMember.Type.ToDisplayString(),
                        destMember.Type.ToDisplayString()));

                    assignments.Add(new MemberAssignmentModel(
                        destMemberName: destMember.Name,
                        sourceExpression: switchExpr,
                        ignoreIfNull: memberIgnoreIfNull,
                        nullCheckExpression: nullCheckExpr,
                        isInitOnly: initOnly));
                }
                else // Cast (default)
                {
                    var destEnumType = (INamedTypeSymbol)destMember.Type;

                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.EnumCastMapping,
                        method.Locations.FirstOrDefault(),
                        key,
                        srcMember.Type.ToDisplayString(),
                        destMember.Type.ToDisplayString()));

                    assignments.Add(new MemberAssignmentModel(
                        destMemberName: destMember.Name,
                        sourceExpression: $"({destEnumType.Name}){paramName}.{srcMember.Name}",
                        ignoreIfNull: memberIgnoreIfNull,
                        nullCheckExpression: nullCheckExpr,
                        isInitOnly: initOnly));
                }
            }
            else if (TryResolveCollectionMapping(srcMember.Type, destMember.Type, forgeClass, allowNested, srcParamName, srcMember.Name, out var collectionExpr))
            {
                diagnostics.Add(Diagnostic.Create(
                    ForgeDiagnostics.CollectionMapping,
                    method.Locations.FirstOrDefault(),
                    key,
                    srcMember.Type.ToDisplayString(),
                    destMember.Type.ToDisplayString()));

                assignments.Add(new MemberAssignmentModel(
                    destMemberName: destMember.Name,
                    sourceExpression: collectionExpr,
                    ignoreIfNull: memberIgnoreIfNull,
                    nullCheckExpression: nullCheckExpr,
                    isInitOnly: initOnly));
            }
            else if (FindConverterMethod(forgeClass, srcMember.Type, destMember.Type, out var converterName))
            {
                // Type converter found
                diagnostics.Add(Diagnostic.Create(
                    ForgeDiagnostics.ConverterUsed,
                    method.Locations.FirstOrDefault(),
                    key, converterName!,
                    srcMember.Type.ToDisplayString(),
                    destMember.Type.ToDisplayString()));

                assignments.Add(new MemberAssignmentModel(
                    destMemberName: destMember.Name,
                    sourceExpression: $"{converterName}({srcParamName}.{srcMember.Name})",
                    ignoreIfNull: memberIgnoreIfNull,
                    nullCheckExpression: nullCheckExpr,
                    isInitOnly: initOnly));
            }
            else
            {
                bool nestedForgeExists = FindNestedForgeMethod(forgeClass, srcMember.Type, destMember.Type, out var nestedMethodName);

                if (nestedForgeExists && allowNested && nestedMethodName != null)
                {
                    var srcAccess = $"{srcParamName}.{srcMember.Name}";
                    string nestedExpr;
                    // Null-safe nested access: if source member is a reference type, guard against null
                    if (srcMember.Type.IsReferenceType)
                    {
                        nestedExpr = $"{srcAccess} != null ? {nestedMethodName}({srcAccess}) : null";
                    }
                    else
                    {
                        nestedExpr = $"{nestedMethodName}({srcAccess})";
                    }

                    assignments.Add(new MemberAssignmentModel(
                        destMemberName: destMember.Name,
                        sourceExpression: nestedExpr,
                        ignoreIfNull: memberIgnoreIfNull,
                        nullCheckExpression: nullCheckExpr,
                        isInitOnly: initOnly));
                }
                else if (!nestedForgeExists)
                {
                    // FKF200: incompatible types, no forge conversion available — block generation
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.IncompatibleMemberTypes,
                        method.Locations.FirstOrDefault(),
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
                    method.Locations.FirstOrDefault(),
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
                    method.Locations.FirstOrDefault(),
                    afterName, method.Name));
            }
        }

        var accessibility = AccessibilityToString(method.DeclaredAccessibility);
        var sourceLocation = method.Locations.FirstOrDefault();
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
            sourceLineNumber: (lineSpan?.StartLinePosition.Line ?? -1) + 1);

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
        List<Diagnostic> diagnostics)
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
            if (FindNestedForgeMethod(forgeClass, srcElemType, destElemType, out var nestedName) && nestedName != null)
            {
                elementTransform = $"x => {nestedName}(x)";
            }
            else
            {
                diagnostics.Add(Diagnostic.Create(
                    ForgeDiagnostics.IncompatibleMemberTypes,
                    method.Locations.FirstOrDefault(),
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

        var location = method.Locations.FirstOrDefault();
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
            sourceLineNumber: location?.GetLineSpan().StartLinePosition.Line + 1 ?? 0,
            collectionProjectExpression: fullExpr);

        return (model, diagnostics);
    }

    /// <summary>Builds a short, unqualified name for a type, handling arrays and generic collections.</summary>
    private static string BuildShortTypeName(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol arr)
            return $"{arr.ElementType.Name}[]";
        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            var args = string.Join(", ", named.TypeArguments.Select(t => t.Name));
            return $"{named.Name}<{args}>";
        }
        return type.Name;
    }

    private static (ConstructionModel Construction, List<Diagnostic> Diagnostics) DetermineConstruction(
        INamedTypeSymbol destType,
        Dictionary<string, (ITypeSymbol Type, string Name, bool IsField)> sourceMembers,
        IMethodSymbol forgeMethod,
        string srcParamName,
        INamedTypeSymbol sourceType)
    {
        var diagnostics = new List<Diagnostic>();
        var sourceName = sourceType.Name;

        var publicCtors = destType.InstanceConstructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        if (publicCtors.Count == 0)
        {
            diagnostics.Add(Diagnostic.Create(
                ForgeDiagnostics.NoViableConstructor,
                forgeMethod.Locations.FirstOrDefault(),
                destType.Name,
                sourceName));
            return (new ConstructionModel(ConstructionKind.Parameterless, new List<ConstructorArgModel>()), diagnostics);
        }

        // Prefer parameterless
        var parameterlessCtor = publicCtors.FirstOrDefault(c => c.Parameters.Length == 0);
        if (parameterlessCtor != null)
        {
            return (new ConstructionModel(ConstructionKind.Parameterless, new List<ConstructorArgModel>()), diagnostics);
        }

        // Find viable parameterized constructors
        var viable = new List<(IMethodSymbol Ctor, List<ConstructorArgModel> Args)>();

        foreach (var ctor in publicCtors)
        {
            var args = new List<ConstructorArgModel>();
            bool allSatisfied = true;

            foreach (var param in ctor.Parameters)
            {
                // Check [ForgeMap] on the constructor parameter first, then fall back to param name
                var forgeMapName = GetForgeMapName(param);
                var key = (forgeMapName ?? param.Name).ToLowerInvariant();
                if (sourceMembers.TryGetValue(key, out var src))
                {
                    if (src.Type.ToDisplayString() == param.Type.ToDisplayString())
                    {
                        args.Add(new ConstructorArgModel(param.Name, $"{srcParamName}.{src.Name}"));
                    }
                    else if (TryResolveNullableMapping(src.Type, param.Type, out var nk))
                    {
                        var srcSymbol = sourceType.GetMembers().FirstOrDefault(m => m.Name == src.Name);
                        var defaultVal = srcSymbol != null ? GetForgeDefaultValue(srcSymbol) : null;
                        string expr;
                        if (nk == NullableConversionKind.UnwrapValue && defaultVal != null)
                            expr = $"{srcParamName}.{src.Name} ?? {FormatLiteral(defaultVal)}";
                        else if (nk == NullableConversionKind.UnwrapValue)
                            expr = $"{srcParamName}.{src.Name}.Value";
                        else
                            expr = $"{srcParamName}.{src.Name}";
                        args.Add(new ConstructorArgModel(param.Name, expr));
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
                forgeMethod.Locations.FirstOrDefault(),
                destType.Name));
            return (new ConstructionModel(ConstructionKind.Parameterless, new List<ConstructorArgModel>()), diagnostics);
        }

        if (viable.Count == 1)
        {
            return (new ConstructionModel(ConstructionKind.Parameterized, viable[0].Args), diagnostics);
        }

        // No viable: try single constructor with FKF501
        if (publicCtors.Count == 1)
        {
            var ctor = publicCtors[0];
            foreach (var param in ctor.Parameters)
            {
                var forgeMapName501 = GetForgeMapName(param);
                var key = (forgeMapName501 ?? param.Name).ToLowerInvariant();
                var typesMatch = sourceMembers.TryGetValue(key, out var src) &&
                    (src.Type.ToDisplayString() == param.Type.ToDisplayString() ||
                     TryResolveNullableMapping(src.Type, param.Type, out _));
                if (!typesMatch)
                {
                    diagnostics.Add(Diagnostic.Create(
                        ForgeDiagnostics.MissingConstructorParameter,
                        forgeMethod.Locations.FirstOrDefault(),
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
                forgeMethod.Locations.FirstOrDefault(),
                destType.Name,
                sourceName));
        }

        return (new ConstructionModel(ConstructionKind.Parameterless, new List<ConstructorArgModel>()), diagnostics);
    }

    // ─── Source Generation ────────────────────────────────────────────────────

    private static string GenerateSource(ForgeClassModel model)
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
        foreach (var ct in model.ContainingTypes)
        {
            sb.AppendLine($"{baseIndent}partial {ct.Keyword} {ct.Name}");
            sb.AppendLine($"{baseIndent}{{");
            baseIndent += "    ";
        }

        sb.AppendLine($"{baseIndent}{model.Accessibility} static partial class {model.ClassName}");
        sb.AppendLine($"{baseIndent}{{");

        var methodIndent = baseIndent + "    ";
        foreach (var method in model.Methods)
        {
            GenerateMethodBody(sb, method, indent: methodIndent);
        }

        sb.AppendLine($"{baseIndent}}}");

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

    private static void GenerateMethodBody(StringBuilder sb, ForgeMethodModel method, string indent)
    {
        if (method.MethodKind == ForgeMethodKind.CollectionProject)
        {
            sb.AppendLine($"{indent}/// <summary>Projects each element of <paramref name=\"{method.SourceParameterName}\"/> to <see cref=\"{method.DestTypeShortName}\"/>. Auto-generated by FreakyKit.Forge.</summary>");
            sb.AppendLine($"{indent}[GeneratedCode(\"FreakyKit.Forge.Generator\", \"1.0.0\")]");
            sb.AppendLine($"{indent}[DebuggerStepThrough]");
            if (!string.IsNullOrEmpty(method.SourceFilePath) && method.SourceLineNumber > 0)
                sb.AppendLine($"{indent}#line {method.SourceLineNumber} \"{method.SourceFilePath}\"");
            sb.AppendLine($"{indent}{method.Accessibility} static partial {method.DestTypeShortName} {method.MethodName}({method.SourceTypeShortName} {method.SourceParameterName})");
            sb.AppendLine($"{indent}#line default");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}    return {method.CollectionProjectExpression};");
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
            sb.AppendLine($"{indent}#line {method.SourceLineNumber} \"{method.SourceFilePath}\"");

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
                if (assignment.IgnoreIfNull && assignment.NullCheckExpression != null)
                {
                    sb.AppendLine($"{indent}    if ({assignment.NullCheckExpression} != null) {method.DestParameterName}.{assignment.DestMemberName} = {assignment.SourceExpression};");
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

            // Separate init-only assignments (must go in object initializer) from regular assignments
            var initOnlyAssignments = method.Assignments.Where(a => a.IsInitOnly && !a.IgnoreIfNull).ToList();
            var regularAssignments = method.Assignments.Where(a => !a.IsInitOnly).ToList();
            // IgnoreIfNull init-only assignments cannot use object initializer (need if-check), so skip them
            var skippedInitOnly = method.Assignments.Where(a => a.IsInitOnly && a.IgnoreIfNull).ToList();

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
                if (assignment.IgnoreIfNull && assignment.NullCheckExpression != null)
                {
                    sb.AppendLine($"{indent}    if ({assignment.NullCheckExpression} != null) __result.{assignment.DestMemberName} = {assignment.SourceExpression};");
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
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static Dictionary<string, (ITypeSymbol Type, string Name, bool IsField)> CollectMembers(
        INamedTypeSymbol type,
        bool includeFields,
        IMethodSymbol? forgeMethod,
        List<Diagnostic>? diagnostics,
        bool isSourceSide = true)
    {
        var result = new Dictionary<string, (ITypeSymbol, string, bool)>();

        foreach (var member in type.GetMembers())
        {
            if (member.IsStatic) continue;
            if (member.DeclaredAccessibility == Accessibility.Private) continue;

            if (member is IPropertySymbol prop)
            {
                if (prop.IsIndexer) continue;
                if (ShouldIgnoreMember(prop, isSourceSide)) continue;
                var mapName = GetForgeMapName(prop);
                var key = (mapName ?? prop.Name).ToLowerInvariant();
                if (result.ContainsKey(key))
                {
                    if (forgeMethod != null && diagnostics != null)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            ForgeDiagnostics.DuplicateForgeMapTarget,
                            forgeMethod.Locations.FirstOrDefault(),
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
                    if (forgeMethod != null && diagnostics != null)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            ForgeDiagnostics.FieldIgnored,
                            forgeMethod.Locations.FirstOrDefault(),
                            field.Name,
                            type.Name));
                    }
                    continue;
                }

                var mapName = GetForgeMapName(field);
                var key = (mapName ?? field.Name).ToLowerInvariant();
                if (result.ContainsKey(key))
                {
                    if (forgeMethod != null && diagnostics != null)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            ForgeDiagnostics.DuplicateForgeMapTarget,
                            forgeMethod.Locations.FirstOrDefault(),
                            key, field.Name, type.Name));
                    }
                }
                else
                {
                    result[key] = (field.Type, field.Name, true);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Tries to resolve a flattened mapping. For a dest key like "addresscity", checks
    /// if source has a member "address" whose type has a member "city" with a compatible type.
    /// </summary>
    private static bool TryResolveFlattenedMapping(
        INamedTypeSymbol sourceType,
        string destKeyLower,
        ITypeSymbol destMemberType,
        string sourceParamName,
        out string flattenExpression)
    {
        flattenExpression = "";
        var paramName = sourceParamName;

        // Try each source member as a prefix
        foreach (var member in sourceType.GetMembers())
        {
            if (member.IsStatic || member.DeclaredAccessibility == Accessibility.Private) continue;

            string memberName;
            ITypeSymbol memberType;

            if (member is IPropertySymbol prop)
            {
                if (prop.IsIndexer) continue;
                memberName = prop.Name;
                memberType = prop.Type;
            }
            else continue; // Only support properties for flattening

            var prefixLower = memberName.ToLowerInvariant();
            if (!destKeyLower.StartsWith(prefixLower) || destKeyLower.Length <= prefixLower.Length)
                continue;

            // Remainder after prefix
            var remainder = destKeyLower.Substring(prefixLower.Length);

            // Check if the member's type has a property matching the remainder
            if (memberType is INamedTypeSymbol nestedType)
            {
                foreach (var nestedMember in nestedType.GetMembers())
                {
                    if (nestedMember.IsStatic || nestedMember.DeclaredAccessibility == Accessibility.Private) continue;
                    if (nestedMember is IPropertySymbol nestedProp)
                    {
                        if (nestedProp.Name.ToLowerInvariant() == remainder &&
                            nestedProp.Type.ToDisplayString() == destMemberType.ToDisplayString())
                        {
                            // Null-safe flattened access: use ?. if the intermediate member is a reference type
                            if (memberType.IsReferenceType)
                                flattenExpression = $"{paramName}.{memberName}?.{nestedProp.Name}";
                            else
                                flattenExpression = $"{paramName}.{memberName}.{nestedProp.Name}";
                            return true;
                        }
                    }
                }
            }
        }
        return false;
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
        out string? methodName)
    {
        var sourceDisplay = sourceType.ToDisplayString();
        var destDisplay = destType.ToDisplayString();

        foreach (var member in forgeClass.GetMembers())
        {
            if (member is IMethodSymbol m &&
                m.IsStatic &&
                m.IsPartialDefinition &&
                m.Parameters.Length == 1 &&
                m.Parameters[0].Type.ToDisplayString() == sourceDisplay &&
                m.ReturnType.ToDisplayString() == destDisplay)
            {
                methodName = m.Name;
                return true;
            }
        }
        methodName = null;
        return false;
    }

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
        out string? converterName)
    {
        converterName = null;
        var srcDisplay = sourceType.ToDisplayString();
        var destDisplay = destType.ToDisplayString();

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
        return false;
    }

    // ─── Collection Helpers ─────────────────────────────────────────────────

    private static bool TryResolveCollectionMapping(
        ITypeSymbol srcType, ITypeSymbol destType,
        INamedTypeSymbol forgeClass, bool allowNested,
        string sourceParamName, string srcMemberName,
        out string expression)
    {
        expression = "";
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

        if (srcElem.ToDisplayString() == destElem.ToDisplayString())
        {
            // Same element type: just materialize
            if (srcIsRefType)
                expression = $"{srcAccessor} != null ? {srcAccessor}{suffix} : {nullFallback}";
            else
                expression = $"{srcAccessor}{suffix}";
            return true;
        }

        // Different element types: check for nested forge
        if (allowNested && FindNestedForgeMethod(forgeClass, srcElem, destElem, out var nestedName) && nestedName != null)
        {
            if (srcIsRefType)
                expression = $"{srcAccessor} != null ? {srcAccessor}.Select(x => {nestedName}(x)){suffix} : {nullFallback}";
            else
                expression = $"{srcAccessor}.Select(x => {nestedName}(x)){suffix}";
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

    private static bool GetForgeIgnoreIfNull(ISymbol member)
    {
        var attr = member.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "FreakyKit.Forge.ForgeMapAttribute");
        if (attr == null) return false;
        foreach (var namedArg in attr.NamedArguments)
        {
            if (namedArg.Key == "IgnoreIfNull" && namedArg.Value.Value is bool b)
                return b;
        }
        return false;
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

    // ─── Internal Types ───────────────────────────────────────────────────────

    private enum GeneratorForgeMode { Implicit = 0, Explicit = 1 }

    private sealed class ForgeClassResult
    {
        public static readonly ForgeClassResult Empty = new(null, new List<Diagnostic>(), hasErrors: false);

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
}
