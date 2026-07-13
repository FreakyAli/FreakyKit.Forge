using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FreakyKit.Forge.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FreakyKit.Forge.Analyzers;

/// <summary>
/// Roslyn diagnostic analyzer for FreakyKit.Forge.
/// Enforces all forge rules and emits diagnostics. Does not generate source.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ForgeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            ForgeDiagnostics.ExplicitModeActivated,
            ForgeDiagnostics.MethodIgnoredInExplicitMode,
            ForgeDiagnostics.PrivateMethodIgnored,
            ForgeDiagnostics.PrivateVisibilityEnabled,
            ForgeDiagnostics.ForgeMethodDeclaresBody,
            ForgeDiagnostics.ForgeMethodNameOverloaded,
            ForgeDiagnostics.UpdateModeActivated,
            ForgeDiagnostics.UpdateDestinationNoSettableMembers,
            ForgeDiagnostics.FieldIgnored,
            ForgeDiagnostics.FieldsEnabled,
            ForgeDiagnostics.DestinationMemberMissing,
            ForgeDiagnostics.SourceMemberUnused,
            ForgeDiagnostics.StrictDestinationMemberMissing,
            ForgeDiagnostics.StrictSourceMemberUnused,
            ForgeDiagnostics.IncompatibleMemberTypes,
            ForgeDiagnostics.NestedForgingDisabled,
            ForgeDiagnostics.ConstructorAmbiguity,
            ForgeDiagnostics.MissingConstructorParameter,
            ForgeDiagnostics.NoViableConstructor,
            ForgeDiagnostics.MemberIgnored,
            ForgeDiagnostics.NullableValueTypeMapping,
            ForgeDiagnostics.NullableMappingApplied,
            ForgeDiagnostics.EnumCastMapping,
            ForgeDiagnostics.EnumNameMapping,
            ForgeDiagnostics.EnumMemberMissing,
            ForgeDiagnostics.CustomMemberMapping,
            ForgeDiagnostics.ForgeMapTargetNotFound,
            ForgeDiagnostics.DuplicateForgeMapTarget,
            ForgeDiagnostics.FlattenedMapping,
            ForgeDiagnostics.BeforeHookDetected,
            ForgeDiagnostics.AfterHookDetected,
            ForgeDiagnostics.CollectionMapping,
            ForgeDiagnostics.ConverterUsed,
            ForgeDiagnostics.InvalidConverterSignature,
            ForgeDiagnostics.ZeroMembersMapped,
            ForgeDiagnostics.ReadOnlyDestinationMember,
            ForgeDiagnostics.WriteOnlySourceMember,
            ForgeDiagnostics.MemberBothIgnoredAndMapped,
            ForgeDiagnostics.ForgeClassNotStatic,
            ForgeDiagnostics.ForgeClassNotPartial,
            ForgeDiagnostics.ForgeOnNonClassType,
            ForgeDiagnostics.FlatteningEnabledNoMatchFound,
            ForgeDiagnostics.ForgeMapSelfReference,
            ForgeDiagnostics.DuplicateConverterForTypePair,
            ForgeDiagnostics.DestinationTypeNotInstantiable,
            ForgeDiagnostics.ExpressionIncompatibleWithUpdate,
            ForgeDiagnostics.ExpressionIgnoresHooks,
            ForgeDiagnostics.ExpressionMemberExcluded,
            ForgeDiagnostics.ExpressionNestedCycle,
            ForgeDiagnostics.ExpressionDeepNesting,
            ForgeDiagnostics.SameTypeCollectionShared,
            ForgeDiagnostics.SameTypeReferenceShared,
            ForgeDiagnostics.ShareReferenceConflict
        );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        var forgeClassAttr = GetForgeClassAttribute(type);
        if (forgeClassAttr is null) return;

        // FKF005: [Forge] on non-class types
        if (type.TypeKind != TypeKind.Class)
        {
            var loc005 = type.Locations.FirstOrDefault();
            if (loc005 != null)
                context.ReportDiagnostic(Diagnostic.Create(ForgeDiagnostics.ForgeOnNonClassType, loc005, type.Name));
            return;
        }

        // FKF003: forge class must be static
        if (!type.IsStatic)
        {
            var loc003 = type.Locations.FirstOrDefault();
            if (loc003 != null)
                context.ReportDiagnostic(Diagnostic.Create(ForgeDiagnostics.ForgeClassNotStatic, loc003, type.Name));
            return;
        }

        // FKF004: forge class must be partial
        if (!IsPartialClass(type, context.CancellationToken))
        {
            var loc004 = type.Locations.FirstOrDefault();
            if (loc004 != null)
                context.ReportDiagnostic(Diagnostic.Create(ForgeDiagnostics.ForgeClassNotPartial, loc004, type.Name));
            return;
        }

        var mode = GetForgeMode(forgeClassAttr);
        var includePrivate = GetIncludePrivateMethods(forgeClassAttr);

        // FKF001: explicit mode activated
        if (mode == ForgeMode.Explicit)
        {
            var classLocation = type.Locations.FirstOrDefault();
            if (classLocation != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ForgeDiagnostics.ExplicitModeActivated,
                    classLocation,
                    type.Name));
            }
        }

        // FKF011: private visibility enabled
        if (includePrivate)
        {
            var classLocation = type.Locations.FirstOrDefault();
            if (classLocation != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ForgeDiagnostics.PrivateVisibilityEnabled,
                    classLocation,
                    type.Name));
            }
        }

        // FKF020: detect partial method implementations that have a body.
        // (User wrote a body on what should be a bodyless forge declaration.)
        // These appear as methods where IsPartialDefinition = false and the
        // syntax has both the `partial` modifier AND a block/expression body.
        // Methods from source-generator output (*.g.cs files) are skipped —
        // they ARE the expected implementations.
        foreach (var impl in type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic && !m.IsPartialDefinition && m.PartialDefinitionPart == null))
        {
            // Skip methods that originate from generated files (e.g. *.g.cs from a source generator)
            bool inGeneratedFile = false;
            foreach (var syntaxRef in impl.DeclaringSyntaxReferences)
            {
                var fp = syntaxRef.SyntaxTree.FilePath;
                if (fp != null && fp.EndsWith(".g.cs", System.StringComparison.OrdinalIgnoreCase))
                {
                    inGeneratedFile = true;
                    break;
                }
            }
            if (inGeneratedFile) continue;

            // Use syntax to confirm this has `partial` keyword + body
            bool hasPartialWithBody = false;
            foreach (var syntaxRef in impl.DeclaringSyntaxReferences)
            {
                var sNode = syntaxRef.GetSyntax(context.CancellationToken);
                if (sNode is MethodDeclarationSyntax mds2 &&
                    mds2.Modifiers.Any(SyntaxKind.PartialKeyword) &&
                    (mds2.Body != null || mds2.ExpressionBody != null))
                {
                    hasPartialWithBody = true;
                    break;
                }
            }
            if (!hasPartialWithBody) continue;

            bool hasForgeAttr = HasForgeAttribute(impl);
            bool looksLikeCreateShape = !impl.ReturnsVoid &&
                                        impl.Parameters.Length == 1 &&
                                        impl.TypeParameters.Length == 0;
            bool looksLikeUpdateShape = impl.ReturnsVoid &&
                                        impl.Parameters.Length == 2 &&
                                        impl.TypeParameters.Length == 0;
            bool looksLikeForgeShape = looksLikeCreateShape || looksLikeUpdateShape;
            if (!looksLikeForgeShape && !hasForgeAttr) continue;

            var loc = impl.Locations.FirstOrDefault();
            if (loc != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ForgeDiagnostics.ForgeMethodDeclaresBody,
                    loc,
                    impl.Name));
            }
        }

        // FKF221: validate [ForgeConverter] method signatures
        foreach (var member in type.GetMembers().OfType<IMethodSymbol>())
        {
            bool hasConverterAttr = member.GetAttributes()
                .Any(a => IsForgeAttribute(a, "FreakyKit.Forge.ForgeConverterAttribute"));
            if (!hasConverterAttr) continue;

            string? reason = null;
            if (!member.IsStatic)
                reason = "must be static";
            else if (member.ReturnsVoid)
                reason = "must have a non-void return type";
            else if (member.TypeParameters.Length > 0)
                reason = "must not be generic";
            else if (member.Parameters.Length != 1)
                reason = $"must have exactly one parameter (found {member.Parameters.Length})";
            else if (member.DeclaredAccessibility != Accessibility.Public && member.DeclaredAccessibility != Accessibility.Internal)
                reason = $"must be public or internal (is {member.DeclaredAccessibility.ToString().ToLower()})";

            if (reason != null)
            {
                var loc = member.Locations.FirstOrDefault();
                if (loc != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ForgeDiagnostics.InvalidConverterSignature,
                        loc,
                        member.Name,
                        reason));
                }
            }
        }

        // FKF222: duplicate [ForgeConverter] for the same type pair
        var convertersByTypePair = new Dictionary<(string, string), List<IMethodSymbol>>();
        foreach (var member in type.GetMembers().OfType<IMethodSymbol>())
        {
            bool hasConverterAttr = member.GetAttributes()
                .Any(a => IsForgeAttribute(a, "FreakyKit.Forge.ForgeConverterAttribute"));
            if (!hasConverterAttr) continue;
            // Only count valid converters (invalid ones are already flagged by FKF221)
            if (!member.IsStatic || member.ReturnsVoid || member.TypeParameters.Length > 0 || member.Parameters.Length != 1)
                continue;

            var pairKey = (member.Parameters[0].Type.ToDisplayString(), member.ReturnType.ToDisplayString());
            if (!convertersByTypePair.TryGetValue(pairKey, out var bucket))
            {
                bucket = new List<IMethodSymbol>();
                convertersByTypePair[pairKey] = bucket;
            }
            bucket.Add(member);
        }
        foreach (var kvp in convertersByTypePair)
        {
            if (kvp.Value.Count > 1)
            {
                foreach (var m in kvp.Value)
                {
                    var loc = m.Locations.FirstOrDefault();
                    if (loc != null)
                        context.ReportDiagnostic(Diagnostic.Create(
                            ForgeDiagnostics.DuplicateConverterForTypePair,
                            loc,
                            type.Name,
                            kvp.Key.Item1,
                            kvp.Key.Item2));
                }
            }
        }

        // Collect all candidate forge methods in this class
        var methods = type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic && m.IsPartialDefinition)
            .ToList();

        var forgeMethodsByName = new Dictionary<string, List<IMethodSymbol>>();

        foreach (var method in methods)
        {
            bool hasForgeAttr = HasForgeAttribute(method);
            bool isCandidate = IsForgeMethodCandidate(method);

            if (!isCandidate && !hasForgeAttr)
                continue;

            // In explicit mode: methods without [Forge] get FKF002
            if (mode == ForgeMode.Explicit && !hasForgeAttr && isCandidate)
            {
                var loc = method.Locations.FirstOrDefault();
                if (loc != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ForgeDiagnostics.MethodIgnoredInExplicitMode,
                        loc,
                        method.Name,
                        type.Name));
                }
                continue;
            }

            // Private methods
            if (method.DeclaredAccessibility == Accessibility.Private && !includePrivate)
            {
                var loc = method.Locations.FirstOrDefault();
                if (loc != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ForgeDiagnostics.PrivateMethodIgnored,
                        loc,
                        method.Name,
                        type.Name));
                }
                continue;
            }

            // Only valid forge methods reach here — must be static partial, right shape
            if (!isCandidate) continue;

            if (!forgeMethodsByName.TryGetValue(method.Name, out var bucket))
            {
                bucket = new List<IMethodSymbol>();
                forgeMethodsByName[method.Name] = bucket;
            }
            bucket.Add(method);
        }

        // FKF030: overloaded forge method names
        var overloadedNames = new List<string>();
        foreach (var kvp in forgeMethodsByName)
        {
            if (kvp.Value.Count > 1)
            {
                foreach (var m in kvp.Value)
                {
                    var loc = m.Locations.FirstOrDefault();
                    if (loc != null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            ForgeDiagnostics.ForgeMethodNameOverloaded,
                            loc,
                            kvp.Key,
                            type.Name));
                    }
                }
                overloadedNames.Add(kvp.Key);
            }
        }
        foreach (var name in overloadedNames)
            forgeMethodsByName.Remove(name);

        // Analyze each valid forge method
        foreach (var bucket in forgeMethodsByName.Values)
        {
            var method = bucket[0];
            AnalyzeForgeMethod(context, method, type);
        }
    }

    private static void AnalyzeForgeMethod(SymbolAnalysisContext context, IMethodSymbol method, INamedTypeSymbol forgeClass)
    {
        // FKF020: method has a body
        if (HasImplementationBody(method, context.CancellationToken))
        {
            var loc = method.Locations.FirstOrDefault();
            if (loc != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ForgeDiagnostics.ForgeMethodDeclaresBody,
                    loc,
                    method.Name));
            }
            return; // Cannot analyze further
        }

        // Detect update vs create shape
        bool isUpdate = method.ReturnsVoid && method.Parameters.Length == 2 && method.TypeParameters.Length == 0;

        INamedTypeSymbol? sourceType;
        INamedTypeSymbol? destType;

        if (isUpdate)
        {
            sourceType = method.Parameters[0].Type as INamedTypeSymbol;
            destType = method.Parameters[1].Type as INamedTypeSymbol;
        }
        else
        {
            if (method.Parameters.Length != 1) return;
            if (method.ReturnsVoid) return;

            sourceType = method.Parameters[0].Type as INamedTypeSymbol;
            destType = method.ReturnType as INamedTypeSymbol;
        }

        if (sourceType is null || destType is null) return;

        // FKF040: update mode info
        if (isUpdate)
        {
            var loc = method.Locations.FirstOrDefault();
            if (loc != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ForgeDiagnostics.UpdateModeActivated,
                    loc,
                    method.Name));
            }
        }

        var forgeAttr = GetForgeAttribute(method);
        bool includeFields = forgeAttr != null && GetBoolProperty(forgeAttr, "ShouldIncludeFields");
        bool allowNested = forgeAttr != null && GetBoolProperty(forgeAttr, "AllowNestedForging");
        bool allowFlattening = forgeAttr != null && GetBoolProperty(forgeAttr, "AllowFlattening");
        bool strictMapping = forgeAttr != null && GetBoolProperty(forgeAttr, "StrictMapping");

        // FKF401: fields enabled
        if (includeFields)
        {
            var loc = method.Locations.FirstOrDefault();
            if (loc != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ForgeDiagnostics.FieldsEnabled,
                    loc,
                    method.Name));
            }
        }

        // Collect source members
        var forgeAssembly = forgeClass.ContainingAssembly;
        var sourceMembers = CollectMembers(sourceType, includeFields, context, method, isSourceSide: true, forgeAssembly: forgeAssembly);

        // Collect dest members
        var destMembers = CollectMembers(destType, includeFields, context, method, isSourceSide: false, forgeAssembly: forgeAssembly);

        if (isUpdate)
        {
            // FKF041: check that dest type has at least one settable member
            bool hasSettable = false;
            foreach (var kvp in destMembers)
            {
                bool isWritable = false;

                foreach (var member in destType.GetMembers())
                {
                    if (member.IsStatic) continue;
                    if (member.DeclaredAccessibility == Accessibility.Private) continue;

                    if (kvp.Value.IsField && member is IFieldSymbol field)
                    {
                        var mapName = GetForgeMapName(field);
                        var effectiveKey = (mapName ?? field.Name).ToLowerInvariant();
                        if (effectiveKey == kvp.Key)
                        {
                            isWritable = !field.IsReadOnly && !field.IsConst;
                            break;
                        }
                    }
                    else if (!kvp.Value.IsField && member is IPropertySymbol prop && !prop.IsIndexer)
                    {
                        var mapName = GetForgeMapName(prop);
                        var effectiveKey = (mapName ?? prop.Name).ToLowerInvariant();
                        if (effectiveKey == kvp.Key)
                        {
                            isWritable = prop.SetMethod != null && !prop.SetMethod.IsInitOnly;
                            break;
                        }
                    }
                }

                if (isWritable)
                {
                    hasSettable = true;
                    break;
                }
            }

            if (!hasSettable)
            {
                var loc = method.Locations.FirstOrDefault();
                if (loc != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ForgeDiagnostics.UpdateDestinationNoSettableMembers,
                        loc,
                        method.Name,
                        destType.Name));
                }
                return; // Cannot analyze further
            }

            // Skip construction analysis for update methods
        }
        HashSet<string> constructorBoundKeys;
        if (isUpdate)
        {
            constructorBoundKeys = new HashSet<string>();
            // Skip construction analysis for update methods
        }
        else
        {
            // Analyze construction (only for create methods)
            constructorBoundKeys = AnalyzeConstruction(context, method, destType, sourceMembers);
        }

        // Analyze member matching
        AnalyzeMemberMatching(context, method, sourceType, destType, sourceMembers, destMembers, allowNested, allowFlattening, forgeClass, constructorBoundKeys, isUpdate, strictMapping);
    }

    private static HashSet<string> AnalyzeConstruction(
        SymbolAnalysisContext context,
        IMethodSymbol forgeMethod,
        INamedTypeSymbol destType,
        Dictionary<string, (ITypeSymbol Type, bool IsField)> sourceMembers)
    {
        var none = new HashSet<string>();

        // FKF503: abstract class, interface, or static class cannot be constructed
        // Note: in Roslyn, static classes have both IsAbstract=true and IsStatic=true (marked as
        // 'abstract sealed' in IL). IsStatic is checked first for a more specific error message.
        if (destType.IsAbstract || destType.IsStatic)
        {
            var loc503 = forgeMethod.Locations.FirstOrDefault();
            if (loc503 != null)
            {
                string kind = destType.IsStatic ? "a static class"
                    : destType.TypeKind == TypeKind.Interface ? "an interface"
                    : "abstract";
                context.ReportDiagnostic(Diagnostic.Create(
                    ForgeDiagnostics.DestinationTypeNotInstantiable, loc503, destType.Name, kind));
            }
            return none;
        }

        var publicCtors = destType.InstanceConstructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        if (publicCtors.Count == 0)
        {
            var loc = forgeMethod.Locations.FirstOrDefault();
            if (loc != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ForgeDiagnostics.NoViableConstructor,
                    loc,
                    destType.Name,
                    forgeMethod.Parameters[0].Type.Name));
            }
            return none;
        }

        // Parameterless constructor always works — no constructor params bound
        var parameterlessCtor = publicCtors.FirstOrDefault(c => c.Parameters.Length == 0);
        if (parameterlessCtor != null) return none;

        // Find viable parameterized constructors
        var viableCtors = new List<IMethodSymbol>();
        var unsatisfiedCtors = new List<(IMethodSymbol Ctor, List<IParameterSymbol> Missing)>();

        foreach (var ctor in publicCtors)
        {
            var missing = new List<IParameterSymbol>();
            foreach (var param in ctor.Parameters)
            {
                var forgeMapName = GetForgeMapName(param);
                var key = (forgeMapName ?? param.Name).ToLowerInvariant();
                if (!sourceMembers.TryGetValue(key, out var srcMember) ||
                    (srcMember.Type.ToDisplayString() != param.Type.ToDisplayString() &&
                     !AreNullableCompatible(srcMember.Type, param.Type)))
                {
                    missing.Add(param);
                }
            }
            if (missing.Count == 0)
                viableCtors.Add(ctor);
            else
                unsatisfiedCtors.Add((ctor, missing));
        }

        if (viableCtors.Count > 1)
        {
            var loc = forgeMethod.Locations.FirstOrDefault();
            if (loc != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ForgeDiagnostics.ConstructorAmbiguity,
                    loc,
                    destType.Name));
            }
            return none;
        }

        if (viableCtors.Count == 1)
        {
            // Return the parameter names the generator will bind via this constructor
            var bound = new HashSet<string>();
            foreach (var param in viableCtors[0].Parameters)
                bound.Add(param.Name.ToLowerInvariant());
            return bound;
        }

        // No viable constructor: report FKF501 for each unsatisfied single-constructor scenario,
        // or FKF502 if all constructors have missing parameters.
        if (publicCtors.Count == 1)
        {
            var (ctor, missing) = unsatisfiedCtors[0];
            foreach (var param in missing)
            {
                var loc = forgeMethod.Locations.FirstOrDefault();
                if (loc != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ForgeDiagnostics.MissingConstructorParameter,
                        loc,
                        param.Name,
                        destType.Name,
                        forgeMethod.Parameters[0].Type.Name));
                }
            }
        }
        else
        {
            var loc = forgeMethod.Locations.FirstOrDefault();
            if (loc != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ForgeDiagnostics.NoViableConstructor,
                    loc,
                    destType.Name,
                    forgeMethod.Parameters[0].Type.Name));
            }
        }
        return none;
    }

    private static void AnalyzeMemberMatching(
        SymbolAnalysisContext context,
        IMethodSymbol forgeMethod,
        INamedTypeSymbol sourceType,
        INamedTypeSymbol destType,
        Dictionary<string, (ITypeSymbol Type, bool IsField)> sourceMembers,
        Dictionary<string, (ITypeSymbol Type, bool IsField)> destMembers,
        bool allowNested,
        bool allowFlattening,
        INamedTypeSymbol forgeClass,
        HashSet<string> constructorBoundKeys,
        bool isUpdate = false,
        bool strictMapping = false)
    {
        var matchedSourceKeys = new HashSet<string>();
        bool isCollectionProjection = IsCollectionType(sourceType) && IsCollectionType(destType);
        int matchedCount = 0;
        int flattenedCount = 0;

        // FKF104: validate [ForgeMap] targets exist on counterpart type
        ValidateForgeMapTargets(context, forgeMethod, sourceType, destMembers, isSourceSide: true);
        ValidateForgeMapTargets(context, forgeMethod, destType, sourceMembers, isSourceSide: false);

        foreach (var destKvp in destMembers)
        {
            var key = destKvp.Key;
            var destMember = destKvp.Value;

            // FKF107: read-only destination member that has a matching source member
            if (IsReadOnlyDestMember(destType, key, isUpdate))
            {
                if (sourceMembers.ContainsKey(key))
                {
                    if (constructorBoundKeys.Contains(key))
                    {
                        // Member is handled via a constructor parameter — count it as matched
                        matchedSourceKeys.Add(key);
                        matchedCount++;
                    }
                    else
                    {
                        var loc107 = forgeMethod.Locations.FirstOrDefault();
                        if (loc107 != null)
                            context.ReportDiagnostic(Diagnostic.Create(
                                ForgeDiagnostics.ReadOnlyDestinationMember, loc107, destType.Name, key));
                    }
                }
                continue;
            }

            if (!sourceMembers.TryGetValue(key, out var srcMember))
            {
                // Try flattening before reporting FKF100
                if (allowFlattening && CanFlatten(sourceType, key, destMember.Type, out var sourceNavKey))
                {
                    if (sourceNavKey != null)
                        matchedSourceKeys.Add(sourceNavKey);
                    matchedCount++;
                    flattenedCount++;
                    continue;
                }

                // FKF100 (warning) or FKF110 (error in strict mode)
                var loc = forgeMethod.Locations.FirstOrDefault();
                if (loc != null)
                {
                    var descriptor = strictMapping
                        ? ForgeDiagnostics.StrictDestinationMemberMissing
                        : ForgeDiagnostics.DestinationMemberMissing;
                    context.ReportDiagnostic(Diagnostic.Create(
                        descriptor,
                        loc,
                        destType.Name,
                        key,
                        sourceType.Name));
                }
                continue;
            }

            matchedSourceKeys.Add(key);
            matchedCount++;

            if (SymbolEqualityComparer.Default.Equals(srcMember.Type, destMember.Type))
                continue; // Exact type match — OK

            // Check nullable compatibility
            if (AreNullableCompatible(srcMember.Type, destMember.Type))
            {
                // Types differ only in nullability — OK, emit info/warning
                bool isValueTypeUnwrap = IsNullableValueType(srcMember.Type) && !IsNullableValueType(destMember.Type) && destMember.Type.IsValueType;
                var loc = forgeMethod.Locations.FirstOrDefault();
                if (isValueTypeUnwrap && loc != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ForgeDiagnostics.NullableValueTypeMapping,
                        loc,
                        key,
                        srcMember.Type.ToDisplayString(),
                        destMember.Type.ToDisplayString()));
                }
                else if (loc != null)
                {
                    // FKF202: nullable mapping applied automatically
                    context.ReportDiagnostic(Diagnostic.Create(
                        ForgeDiagnostics.NullableMappingApplied,
                        loc,
                        key,
                        srcMember.Type.ToDisplayString(),
                        destMember.Type.ToDisplayString()));
                }
                continue;
            }

            // Check enum-to-enum mapping
            if (srcMember.Type.TypeKind == TypeKind.Enum && destMember.Type.TypeKind == TypeKind.Enum)
            {
                // Enum-to-enum is handled by the generator — no FKF200
                continue;
            }

            // Check collection mapping
            if (IsCollectionType(srcMember.Type) && IsCollectionType(destMember.Type))
                continue; // Collection mapping is handled by the generator

            // Check for type converter
            if (ConverterExists(forgeClass, srcMember.Type, destMember.Type))
                continue; // Type converter handles this

            // Check for implicit conversion
            if (TryImplicitConversion(context.Compilation, srcMember.Type, destMember.Type, out var isLossy))
            {
                if (isLossy)
                {
                    // FKF203: lossy implicit conversion
                    var loc = forgeMethod.Locations.FirstOrDefault();
                    if (loc != null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            ForgeDiagnostics.LossyImplicitConversion,
                            loc,
                            key,
                            srcMember.Type.ToDisplayString(),
                            destMember.Type.ToDisplayString()));
                    }
                }
                continue; // Implicit conversion handles this
            }

            // Types differ: check for nested forge
            bool nestedForgeExists = NestedForgeExists(forgeClass, srcMember.Type, destMember.Type);

            if (nestedForgeExists && !allowNested)
            {
                // FKF300: nested forge available but disabled
                var loc = forgeMethod.Locations.FirstOrDefault();
                if (loc != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ForgeDiagnostics.NestedForgingDisabled,
                        loc,
                        key,
                        srcMember.Type.Name,
                        destMember.Type.Name));
                }
            }
            else if (!nestedForgeExists)
            {
                // FKF200: incompatible types, no forge conversion available
                var loc = forgeMethod.Locations.FirstOrDefault();
                if (loc != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ForgeDiagnostics.IncompatibleMemberTypes,
                        loc,
                        key,
                        srcMember.Type.ToDisplayString(),
                        destMember.Type.ToDisplayString()));
                }
            }
            // else: nested forging is allowed and available — OK
        }

        // FKF101 (warning) or FKF111 (error in strict mode)
        foreach (var srcKey in sourceMembers.Keys)
        {
            var key = srcKey;
            if (!matchedSourceKeys.Contains(key) && !destMembers.ContainsKey(key))
            {
                var loc = forgeMethod.Locations.FirstOrDefault();
                if (loc != null)
                {
                    var descriptor = strictMapping
                        ? ForgeDiagnostics.StrictSourceMemberUnused
                        : ForgeDiagnostics.SourceMemberUnused;
                    context.ReportDiagnostic(Diagnostic.Create(
                        descriptor,
                        loc,
                        sourceType.Name,
                        key,
                        destType.Name));
                }
            }
        }

        // FKF042: no members were matched at all (skip for collection projection methods)
        if (matchedCount == 0 && !isCollectionProjection)
        {
            var loc = forgeMethod.Locations.FirstOrDefault();
            if (loc != null)
                context.ReportDiagnostic(Diagnostic.Create(
                    ForgeDiagnostics.ZeroMembersMapped,
                    loc,
                    forgeMethod.Name,
                    sourceType.Name,
                    destType.Name));
        }

        // FKF043: AllowFlattening enabled but no members were matched via flattening
        if (allowFlattening && flattenedCount == 0 && !isCollectionProjection)
        {
            var loc = forgeMethod.Locations.FirstOrDefault();
            if (loc != null)
                context.ReportDiagnostic(Diagnostic.Create(
                    ForgeDiagnostics.FlatteningEnabledNoMatchFound,
                    loc,
                    forgeMethod.Name));
        }
    }

    private static void ValidateForgeMapTargets(
        SymbolAnalysisContext context,
        IMethodSymbol forgeMethod,
        INamedTypeSymbol type,
        Dictionary<string, (ITypeSymbol Type, bool IsField)> counterpartMembers,
        bool isSourceSide)
    {
        for (var currentType = type; currentType != null; currentType = currentType.BaseType)
        {
            foreach (var member in currentType.GetMembers())
            {
                if (member.IsStatic || member.DeclaredAccessibility == Accessibility.Private) continue;

                string? mapName = null;
                string memberName = "";

                if (member is IPropertySymbol prop && !prop.IsIndexer)
                {
                    mapName = GetForgeMapName(prop);
                    memberName = prop.Name;
                }
                else if (member is IFieldSymbol field)
                {
                    mapName = GetForgeMapName(field);
                    memberName = field.Name;
                }

                if (mapName == null) continue;

                var mapKey = mapName.ToLowerInvariant();
                if (!counterpartMembers.ContainsKey(mapKey))
                {
                    var loc = forgeMethod.Locations.FirstOrDefault();
                    if (loc != null)
                        context.ReportDiagnostic(Diagnostic.Create(
                            ForgeDiagnostics.ForgeMapTargetNotFound,
                            loc,
                            memberName, type.Name, mapName));
                }
            }
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static Dictionary<string, (ITypeSymbol Type, bool IsField)> CollectMembers(
        INamedTypeSymbol type,
        bool includeFields,
        SymbolAnalysisContext context,
        IMethodSymbol forgeMethod,
        bool isSourceSide = true,
        IAssemblySymbol? forgeAssembly = null)
    {
        var result = new Dictionary<string, (ITypeSymbol, bool)>();

        for (var currentType = type; currentType != null; currentType = currentType.BaseType)
        {
            var isDeclaredType = SymbolEqualityComparer.Default.Equals(currentType, type);

            foreach (var member in currentType.GetMembers())
            {
                if (member.IsStatic) continue;
                if (!IsMemberAccessibleFromStaticContext(member, forgeAssembly)) continue;

                if (member is IPropertySymbol prop)
                {
                    if (prop.IsIndexer) continue;

                    // FKF109: member has both [ForgeIgnore] and [ForgeMap] (only for directly declared members)
                    if (isDeclaredType && HasIgnoreAttribute(prop) && GetForgeMapName(prop) != null)
                    {
                        var loc = forgeMethod.Locations.FirstOrDefault();
                        if (loc != null)
                            context.ReportDiagnostic(Diagnostic.Create(
                                ForgeDiagnostics.MemberBothIgnoredAndMapped, loc, prop.Name, type.Name));
                    }

                    if (ShouldIgnoreMember(prop, isSourceSide))
                    {
                        // FKF102: member excluded via [ForgeIgnore]
                        if (isDeclaredType)
                        {
                            var loc102 = forgeMethod.Locations.FirstOrDefault();
                            if (loc102 != null)
                                context.ReportDiagnostic(Diagnostic.Create(
                                    ForgeDiagnostics.MemberIgnored, loc102, prop.Name, type.Name));
                        }
                        continue;
                    }

                    // FKF108: source member getter absent or inaccessible to generated code
                    if (isSourceSide && !IsGetterAccessible(prop))
                    {
                        if (isDeclaredType)
                        {
                            var loc = forgeMethod.Locations.FirstOrDefault();
                            if (loc != null)
                                context.ReportDiagnostic(Diagnostic.Create(
                                    ForgeDiagnostics.WriteOnlySourceMember, loc, type.Name, prop.Name));
                        }
                        continue;
                    }

                    var mapName = GetForgeMapName(prop);

                    // FKF103: custom member mapping via [ForgeMap]
                    if (isDeclaredType && mapName != null)
                    {
                        var loc103 = forgeMethod.Locations.FirstOrDefault();
                        if (loc103 != null)
                            context.ReportDiagnostic(Diagnostic.Create(
                                ForgeDiagnostics.CustomMemberMapping, loc103, prop.Name, type.Name, mapName));
                    }

                    // FKF112: [ForgeMap] target is the member's own name — no-op
                    if (isDeclaredType && mapName != null && string.Equals(mapName, prop.Name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        var loc112 = forgeMethod.Locations.FirstOrDefault();
                        if (loc112 != null)
                            context.ReportDiagnostic(Diagnostic.Create(
                                ForgeDiagnostics.ForgeMapSelfReference, loc112, prop.Name, type.Name, mapName));
                    }

                    var keyLower = (mapName ?? prop.Name).ToLowerInvariant();
                    if (result.ContainsKey(keyLower))
                    {
                        if (isDeclaredType)
                        {
                            var loc = forgeMethod.Locations.FirstOrDefault();
                            if (loc != null)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(
                                    ForgeDiagnostics.DuplicateForgeMapTarget,
                                    loc,
                                    keyLower, prop.Name, type.Name));
                            }
                        }
                    }
                    else
                    {
                        result[keyLower] = (prop.Type, false);
                    }
                }
                else if (member is IFieldSymbol field)
                {
                    // FKF109: field has both [ForgeIgnore] and [ForgeMap] (only for directly declared members)
                    if (isDeclaredType && HasIgnoreAttribute(field) && GetForgeMapName(field) != null)
                    {
                        var loc = forgeMethod.Locations.FirstOrDefault();
                        if (loc != null)
                            context.ReportDiagnostic(Diagnostic.Create(
                                ForgeDiagnostics.MemberBothIgnoredAndMapped, loc, field.Name, type.Name));
                    }

                    if (ShouldIgnoreMember(field, isSourceSide))
                    {
                        // FKF102: field excluded via [ForgeIgnore]
                        if (isDeclaredType)
                        {
                            var loc102 = forgeMethod.Locations.FirstOrDefault();
                            if (loc102 != null)
                                context.ReportDiagnostic(Diagnostic.Create(
                                    ForgeDiagnostics.MemberIgnored, loc102, field.Name, type.Name));
                        }
                        continue;
                    }
                    if (!includeFields)
                    {
                        if (isDeclaredType)
                        {
                            // FKF400: field ignored
                            var loc = forgeMethod.Locations.FirstOrDefault();
                            if (loc != null)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(
                                    ForgeDiagnostics.FieldIgnored,
                                    loc,
                                    field.Name,
                                    type.Name));
                            }
                        }
                        continue;
                    }
                    var mapName = GetForgeMapName(field);

                    // FKF103: custom field mapping via [ForgeMap]
                    if (isDeclaredType && mapName != null)
                    {
                        var loc103 = forgeMethod.Locations.FirstOrDefault();
                        if (loc103 != null)
                            context.ReportDiagnostic(Diagnostic.Create(
                                ForgeDiagnostics.CustomMemberMapping, loc103, field.Name, type.Name, mapName));
                    }

                    // FKF112: [ForgeMap] target is the field's own name — no-op
                    if (isDeclaredType && mapName != null && string.Equals(mapName, field.Name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        var loc112 = forgeMethod.Locations.FirstOrDefault();
                        if (loc112 != null)
                            context.ReportDiagnostic(Diagnostic.Create(
                                ForgeDiagnostics.ForgeMapSelfReference, loc112, field.Name, type.Name, mapName));
                    }

                    var keyLower = (mapName ?? field.Name).ToLowerInvariant();
                    if (result.ContainsKey(keyLower))
                    {
                        if (isDeclaredType)
                        {
                            var loc = forgeMethod.Locations.FirstOrDefault();
                            if (loc != null)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(
                                    ForgeDiagnostics.DuplicateForgeMapTarget,
                                    loc,
                                    keyLower, field.Name, type.Name));
                            }
                        }
                    }
                    else
                    {
                        result[keyLower] = (field.Type, true);
                    }
                }
            }
        }

        return result;
    }

    // Generated code lives in the same assembly but is not a subclass of the source type,
    // so private/protected/private-protected getters are all off-limits.
    private static bool IsGetterAccessible(IPropertySymbol prop)
    {
        if (prop.GetMethod == null) return false;
        var acc = prop.GetMethod.DeclaredAccessibility;
        return acc == Accessibility.Public
            || acc == Accessibility.Internal
            || acc == Accessibility.ProtectedOrInternal;
    }

    private static bool IsNullableValueType(ITypeSymbol type) =>
        type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

    private static bool AreNullableCompatible(ITypeSymbol srcType, ITypeSymbol destType)
    {
        // Case 1: Nullable<T> → T
        if (srcType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            srcType is INamedTypeSymbol srcNullable &&
            srcNullable.TypeArguments.Length == 1)
        {
            if (SymbolEqualityComparer.Default.Equals(srcNullable.TypeArguments[0], destType))
                return true;
        }

        // Case 2: T → Nullable<T>
        if (destType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            destType is INamedTypeSymbol destNullable &&
            destNullable.TypeArguments.Length == 1)
        {
            if (SymbolEqualityComparer.Default.Equals(destNullable.TypeArguments[0], srcType))
                return true;
        }

        // Case 3: Reference type nullability annotation difference
        if (SymbolEqualityComparer.Default.Equals(
                srcType.WithNullableAnnotation(NullableAnnotation.NotAnnotated),
                destType.WithNullableAnnotation(NullableAnnotation.NotAnnotated)))
        {
            if (!SymbolEqualityComparer.IncludeNullability.Equals(srcType, destType))
                return true;
        }

        return false;
    }

    private static bool ConverterExists(INamedTypeSymbol forgeClass, ITypeSymbol sourceType, ITypeSymbol destType)
    {
        return forgeClass.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic && !m.ReturnsVoid && m.Parameters.Length == 1)
            .Any(m =>
                m.GetAttributes().Any(a =>
                    IsForgeAttribute(a, "FreakyKit.Forge.ForgeConverterAttribute")) &&
                m.Parameters[0].Type.ToDisplayString() == sourceType.ToDisplayString() &&
                m.ReturnType.ToDisplayString() == destType.ToDisplayString());
    }

    private static bool IsCollectionType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol) return true;
        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            if (named.TypeArguments.Length == 1)
            {
                foreach (var iface in named.AllInterfaces)
                {
                    if (iface.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>")
                        return true;
                }
                if (named.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>")
                    return true;
            }
            // Dictionary types (2 type args) are also handled by the generator
            if (named.TypeArguments.Length == 2)
            {
                var def = named.OriginalDefinition.ToDisplayString();
                if (def == "System.Collections.Generic.Dictionary<TKey, TValue>" ||
                    def == "System.Collections.Generic.IDictionary<TKey, TValue>" ||
                    def == "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>")
                    return true;
            }
        }
        return false;
    }

    private static bool IsReadOnlyDestMember(INamedTypeSymbol destType, string keyLower, bool isUpdate = false)
    {
        foreach (var member in destType.GetMembers())
        {
            if (member.IsStatic) continue;
            if (member.DeclaredAccessibility == Accessibility.Private) continue;

            if (member is IPropertySymbol prop && !prop.IsIndexer)
            {
                var mapName = GetForgeMapName(prop);
                var effectiveKey = (mapName ?? prop.Name).ToLowerInvariant();
                if (effectiveKey == keyLower)
                {
                    if (prop.SetMethod == null) return true;
                    // Init-only: read-only for update methods, writable for create methods (via object initializer)
                    if (prop.SetMethod.IsInitOnly) return isUpdate;
                    return false;
                }
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

    private static bool CanFlatten(INamedTypeSymbol sourceType, string destKeyLower, ITypeSymbol destMemberType, out string? sourceNavKey)
    {
        sourceNavKey = null;
        foreach (var member in sourceType.GetMembers())
        {
            if (member.IsStatic || member.DeclaredAccessibility == Accessibility.Private) continue;
            if (member is not IPropertySymbol prop || prop.IsIndexer) continue;

            var prefixLower = prop.Name.ToLowerInvariant();
            if (!destKeyLower.StartsWith(prefixLower) || destKeyLower.Length <= prefixLower.Length)
                continue;

            var remainder = destKeyLower.Substring(prefixLower.Length);
            if (prop.Type is INamedTypeSymbol nestedType)
            {
                foreach (var nestedMember in nestedType.GetMembers())
                {
                    if (nestedMember.IsStatic || nestedMember.DeclaredAccessibility == Accessibility.Private) continue;
                    if (nestedMember is IPropertySymbol nestedProp &&
                        nestedProp.Name.ToLowerInvariant() == remainder &&
                        nestedProp.Type.ToDisplayString() == destMemberType.ToDisplayString())
                    {
                        sourceNavKey = prefixLower;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private static bool NestedForgeExists(INamedTypeSymbol forgeClass, ITypeSymbol sourceType, ITypeSymbol destType)
    {
        return forgeClass.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.IsStatic && m.IsPartialDefinition)
            .Any(m =>
                m.Parameters.Length == 1 &&
                m.Parameters[0].Type.ToDisplayString() == sourceType.ToDisplayString() &&
                m.ReturnType.ToDisplayString() == destType.ToDisplayString());
    }

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
        isLossy = IsLossyConversion(sourceType, destType);

        return true;
    }

    private static bool IsLossyConversion(ITypeSymbol sourceType, ITypeSymbol destType)
    {
        var srcName = sourceType.ToDisplayString();
        var destName = destType.ToDisplayString();

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

    private static bool IsForgeMethodCandidate(IMethodSymbol method)
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

    private static bool HasImplementationBody(IMethodSymbol method, System.Threading.CancellationToken ct)
    {
        // Check the method's own syntax for a body (declaration side never has one by definition).
        // Implementation parts provided by the source generator are NOT user bodies and must not
        // trigger FKF020; orphan user implementations are caught by the separate scan above.
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

    private static bool IsPartialClass(INamedTypeSymbol type, System.Threading.CancellationToken ct)
    {
        foreach (var syntaxRef in type.DeclaringSyntaxReferences)
        {
            var syntax = syntaxRef.GetSyntax(ct);
            if (syntax is ClassDeclarationSyntax cds)
            {
                if (cds.Modifiers.Any(SyntaxKind.PartialKeyword))
                    return true;
            }
        }
        return false;
    }

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

    private static bool IsForgeAttribute(AttributeData a, string fqn)
        => a.AttributeClass?.ToDisplayString() == fqn;

    private static AttributeData? GetForgeClassAttribute(INamedTypeSymbol type)
    {
        return type.GetAttributes()
            .FirstOrDefault(a => IsForgeAttribute(a, "FreakyKit.Forge.ForgeAttribute"));
    }

    private static AttributeData? GetForgeAttribute(IMethodSymbol method)
    {
        return method.GetAttributes()
            .FirstOrDefault(a => IsForgeAttribute(a, "FreakyKit.Forge.ForgeMethodAttribute"));
    }

    private static bool HasForgeAttribute(IMethodSymbol method)
    {
        return GetForgeAttribute(method) != null;
    }

    private static bool HasIgnoreAttribute(ISymbol member)
        => member.GetAttributes().Any(a =>
            IsForgeAttribute(a, "FreakyKit.Forge.ForgeIgnoreAttribute"));

    private static bool ShouldIgnoreMember(ISymbol member, bool isSourceSide)
    {
        var attr = member.GetAttributes()
            .FirstOrDefault(a => IsForgeAttribute(a, "FreakyKit.Forge.ForgeIgnoreAttribute"));
        if (attr == null) return false;

        var sideArg = attr.NamedArguments.FirstOrDefault(a => a.Key == "Side");
        int side = sideArg.Key != null && sideArg.Value.Value is int sv ? sv : 0;

        return side == 0
            || (side == 1 && isSourceSide)
            || (side == 2 && !isSourceSide);
    }

    private static string? GetForgeMapName(ISymbol member)
    {
        var attr = member.GetAttributes()
            .FirstOrDefault(a => IsForgeAttribute(a, "FreakyKit.Forge.ForgeMapAttribute"));
        if (attr != null && attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments[0].Value is string name)
            return name;
        return null;
    }

    private static ForgeMode GetForgeMode(AttributeData attr)
    {
        var namedArg = attr.NamedArguments.FirstOrDefault(a => a.Key == "Mode");
        if (namedArg.Value.Value is int val)
            return (ForgeMode)val;
        return ForgeMode.Implicit;
    }

    private static bool GetIncludePrivateMethods(AttributeData attr)
    {
        var namedArg = attr.NamedArguments.FirstOrDefault(a => a.Key == "ShouldIncludePrivate");
        if (namedArg.Value.Value is bool val)
            return val;
        return false;
    }

    private static bool GetBoolProperty(AttributeData attr, string name)
    {
        var namedArg = attr.NamedArguments.FirstOrDefault(a => a.Key == name);
        if (namedArg.Value.Value is bool val)
            return val;
        return false;
    }

    // Local enum mirror to avoid referencing the core library from analyzers
    private enum ForgeMode
    {
        Implicit = 0,
        Explicit = 1
    }
}
