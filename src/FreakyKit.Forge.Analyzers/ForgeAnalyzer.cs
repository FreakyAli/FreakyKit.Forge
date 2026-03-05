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
            ForgeDiagnostics.ConverterUsed
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

        // Only analyze static partial classes with [Forge]
        if (!type.IsStatic) return;
        if (!IsPartialClass(type, context.CancellationToken)) return;

        var forgeClassAttr = GetForgeClassAttribute(type);
        if (forgeClassAttr is null) return;

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
        var sourceMembers = CollectMembers(sourceType, includeFields, context, method);

        // Collect dest members
        var destMembers = CollectMembers(destType, includeFields, context, method);

        if (isUpdate)
        {
            // FKF041: check that dest type has at least one settable member
            bool hasSettable = false;
            foreach (var kvp in destMembers)
            {
                if (kvp.Value.IsField)
                {
                    hasSettable = true;
                    break;
                }

                // Find the actual property by resolving its effective key (respecting ForgeMap)
                bool isReadOnly = false;
                foreach (var member in destType.GetMembers())
                {
                    if (member is IPropertySymbol prop &&
                        !prop.IsStatic &&
                        !prop.IsIndexer &&
                        member.DeclaredAccessibility != Accessibility.Private)
                    {
                        var mapName = GetForgeMapName(prop);
                        var effectiveKey = (mapName ?? prop.Name).ToLowerInvariant();
                        if (effectiveKey == kvp.Key)
                        {
                            if (prop.SetMethod == null)
                                isReadOnly = true;
                            break;
                        }
                    }
                }
                if (!isReadOnly)
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
        else
        {
            // Analyze construction (only for create methods)
            AnalyzeConstruction(context, method, destType, sourceMembers);
        }

        // Analyze member matching
        AnalyzeMemberMatching(context, method, sourceType, destType, sourceMembers, destMembers, allowNested, allowFlattening, forgeClass);
    }

    private static void AnalyzeConstruction(
        SymbolAnalysisContext context,
        IMethodSymbol forgeMethod,
        INamedTypeSymbol destType,
        Dictionary<string, (ITypeSymbol Type, bool IsField)> sourceMembers)
    {
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
            return;
        }

        // Parameterless constructor always works
        var parameterlessCtor = publicCtors.FirstOrDefault(c => c.Parameters.Length == 0);
        if (parameterlessCtor != null) return;

        // Find viable parameterized constructors
        var viableCtors = new List<IMethodSymbol>();
        var unsatisfiedCtors = new List<(IMethodSymbol Ctor, List<IParameterSymbol> Missing)>();

        foreach (var ctor in publicCtors)
        {
            var missing = new List<IParameterSymbol>();
            foreach (var param in ctor.Parameters)
            {
                var key = param.Name.ToLowerInvariant();
                if (!sourceMembers.TryGetValue(key, out var srcMember) ||
                    (!SymbolEqualityComparer.Default.Equals(srcMember.Type, param.Type) &&
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
            return;
        }

        if (viableCtors.Count == 1) return; // Exactly one viable ctor — success

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
        INamedTypeSymbol forgeClass)
    {
        var matchedSourceKeys = new HashSet<string>();

        foreach (var destKvp in destMembers)
        {
            var key = destKvp.Key;
            var destMember = destKvp.Value;

            if (!sourceMembers.TryGetValue(key, out var srcMember))
            {
                // Try flattening before reporting FKF100
                if (allowFlattening && CanFlatten(sourceType, key, destMember.Type))
                    continue;

                // FKF100: destination member has no source counterpart
                var loc = forgeMethod.Locations.FirstOrDefault();
                if (loc != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ForgeDiagnostics.DestinationMemberMissing,
                        loc,
                        destType.Name,
                        key,
                        sourceType.Name));
                }
                continue;
            }

            matchedSourceKeys.Add(key);

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

        // FKF101: source members with no destination counterpart
        foreach (var srcKey in sourceMembers.Keys)
        {
            var key = srcKey;
            if (!matchedSourceKeys.Contains(key) && !destMembers.ContainsKey(key))
            {
                var loc = forgeMethod.Locations.FirstOrDefault();
                if (loc != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ForgeDiagnostics.SourceMemberUnused,
                        loc,
                        sourceType.Name,
                        key,
                        destType.Name));
                }
            }
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static Dictionary<string, (ITypeSymbol Type, bool IsField)> CollectMembers(
        INamedTypeSymbol type,
        bool includeFields,
        SymbolAnalysisContext context,
        IMethodSymbol forgeMethod)
    {
        var result = new Dictionary<string, (ITypeSymbol, bool)>();

        foreach (var member in type.GetMembers())
        {
            if (member.IsStatic) continue;
            if (member.DeclaredAccessibility == Accessibility.Private) continue;

            if (member is IPropertySymbol prop)
            {
                if (prop.IsIndexer) continue;
                if (HasForgeIgnoreAttribute(prop)) continue;
                var mapName = GetForgeMapName(prop);
                var keyLower = (mapName ?? prop.Name).ToLowerInvariant();
                if (result.ContainsKey(keyLower))
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
                else
                {
                    result[keyLower] = (prop.Type, false);
                }
            }
            else if (member is IFieldSymbol field)
            {
                if (HasForgeIgnoreAttribute(field)) continue;
                if (!includeFields)
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
                    continue;
                }
                var mapName = GetForgeMapName(field);
                var keyLower = (mapName ?? field.Name).ToLowerInvariant();
                if (result.ContainsKey(keyLower))
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
                else
                {
                    result[keyLower] = (field.Type, true);
                }
            }
        }

        return result;
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
            if (!SymbolEqualityComparer.Default.Equals(srcType, destType))
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
                    a.AttributeClass?.Name == "ForgeConverterAttribute" ||
                    a.AttributeClass?.Name == "ForgeConverter") &&
                SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, sourceType) &&
                SymbolEqualityComparer.Default.Equals(m.ReturnType, destType));
    }

    private static bool IsCollectionType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol) return true;
        if (type is INamedTypeSymbol named && named.IsGenericType && named.TypeArguments.Length == 1)
        {
            foreach (var iface in named.AllInterfaces)
            {
                if (iface.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>")
                    return true;
            }
            // Also check the type itself (e.g. IEnumerable<T>)
            if (named.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>")
                return true;
        }
        return false;
    }

    private static bool CanFlatten(INamedTypeSymbol sourceType, string destKeyLower, ITypeSymbol destMemberType)
    {
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
                        SymbolEqualityComparer.Default.Equals(nestedProp.Type, destMemberType))
                    {
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
                SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, sourceType) &&
                SymbolEqualityComparer.Default.Equals(m.ReturnType, destType));
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

    private static AttributeData? GetForgeClassAttribute(INamedTypeSymbol type)
    {
        return type.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ForgeAttribute" ||
                                 a.AttributeClass?.Name == "Forge");
    }

    private static AttributeData? GetForgeAttribute(IMethodSymbol method)
    {
        return method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ForgeMethodAttribute" ||
                                 a.AttributeClass?.Name == "ForgeMethod");
    }

    private static bool HasForgeAttribute(IMethodSymbol method)
    {
        return GetForgeAttribute(method) != null;
    }

    private static bool HasForgeIgnoreAttribute(ISymbol member)
    {
        return member.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "ForgeIgnoreAttribute" ||
                       a.AttributeClass?.Name == "ForgeIgnore");
    }

    private static string? GetForgeMapName(ISymbol member)
    {
        var attr = member.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ForgeMapAttribute" ||
                                  a.AttributeClass?.Name == "ForgeMap");
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
