using Microsoft.CodeAnalysis;

namespace FreakyKit.Forge.Diagnostics;

/// <summary>
/// Single source of truth for all FreakyKit.Forge diagnostic descriptors.
/// All diagnostic IDs, titles, messages, severities, and categories are defined here.
/// No diagnostics may be defined anywhere else.
/// </summary>
public static class ForgeDiagnostics
{
    private const string Category_Mode = "FreakyKit.Forge.Mode";
    private const string Category_MethodShape = "FreakyKit.Forge.MethodShape";
    private const string Category_MemberDiscovery = "FreakyKit.Forge.MemberDiscovery";
    private const string Category_MemberMatching = "FreakyKit.Forge.MemberMatching";
    private const string Category_TypeSafety = "FreakyKit.Forge.TypeSafety";
    private const string Category_Nested = "FreakyKit.Forge.Nested";
    private const string Category_Construction = "FreakyKit.Forge.Construction";

    // ─── Mode & Visibility ───────────────────────────────────────────────────

    /// <summary>
    /// FKF001 (Info): Explicit method selection mode is active on this forge class.
    /// Only methods with [ForgeMethod] will be treated as forge methods.
    /// </summary>
    public static readonly DiagnosticDescriptor ExplicitModeActivated = new(
        id: "FKF001",
        title: "Explicit mode activated",
        messageFormat: "Forge class '{0}' uses explicit method selection mode. Only methods decorated with [ForgeMethod] will be treated as forge methods.",
        category: Category_Mode,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The forge class is configured with ForgeMode.Explicit. Methods without [ForgeMethod] will be ignored and emit FKF002.");

    /// <summary>
    /// FKF002 (Warning): A candidate forge method is ignored because the class uses explicit mode
    /// and the method lacks a [ForgeMethod] attribute.
    /// </summary>
    public static readonly DiagnosticDescriptor MethodIgnoredInExplicitMode = new(
        id: "FKF002",
        title: "Method ignored in explicit mode",
        messageFormat: "Method '{0}' in forge class '{1}' is ignored because explicit mode is active. Add [ForgeMethod] to include this method.",
        category: Category_Mode,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "In explicit mode (ForgeMode.Explicit), only methods decorated with [ForgeMethod] are treated as forge methods. This method matches the forge shape but lacks the attribute.");

    /// <summary>
    /// FKF010 (Warning): A private forge method is ignored because ShouldIncludePrivate is false.
    /// </summary>
    public static readonly DiagnosticDescriptor PrivateMethodIgnored = new(
        id: "FKF010",
        title: "Private forge method ignored",
        messageFormat: "Private method '{0}' in forge class '{1}' is ignored. Set ShouldIncludePrivate = true on [Forge] to include private methods.",
        category: Category_Mode,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Private forge methods are ignored unless ShouldIncludePrivate is enabled on the containing [Forge].");

    /// <summary>
    /// FKF011 (Info): Private method inclusion is enabled on this forge class.
    /// </summary>
    public static readonly DiagnosticDescriptor PrivateVisibilityEnabled = new(
        id: "FKF011",
        title: "Private visibility enabled",
        messageFormat: "Forge class '{0}' has ShouldIncludePrivate = true. Private forge methods will be included.",
        category: Category_Mode,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The forge class allows private methods to be treated as forge methods.");

    // ─── Method Shape ─────────────────────────────────────────────────────────

    /// <summary>
    /// FKF020 (Error): A forge method has an implementation body.
    /// Forge methods must be declaration-only partial methods.
    /// </summary>
    public static readonly DiagnosticDescriptor ForgeMethodDeclaresBody = new(
        id: "FKF020",
        title: "Forge method declares a body",
        messageFormat: "Forge method '{0}' must not have an implementation body. Remove the body; the generator will provide it.",
        category: Category_MethodShape,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Forge methods are declaration-only partial methods. The source generator provides the implementation. Having a body prevents generation.");

    /// <summary>
    /// FKF030 (Error): Two or more forge methods in the same class share the same name (overloading).
    /// Forge method names must be unique within a forge class.
    /// </summary>
    public static readonly DiagnosticDescriptor ForgeMethodNameOverloaded = new(
        id: "FKF030",
        title: "Forge method name overloaded",
        messageFormat: "Forge method name '{0}' in class '{1}' is used more than once. Forge method names must be unique within a forge class.",
        category: Category_MethodShape,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Multiple forge methods with the same name create ambiguity. Each forge method must have a unique name within its containing class.");

    /// <summary>
    /// FKF040 (Info): Forge method uses update mode. The destination object will be modified in place.
    /// </summary>
    public static readonly DiagnosticDescriptor UpdateModeActivated = new(
        id: "FKF040",
        title: "Update mode activated",
        messageFormat: "Forge method '{0}' uses update mode. The destination object will be modified in place.",
        category: Category_MethodShape,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The forge method uses the update mapping shape (void return, 2 parameters). The destination object's members will be overwritten in place.");

    /// <summary>
    /// FKF041 (Error): Update forge method destination type has no settable members.
    /// </summary>
    public static readonly DiagnosticDescriptor UpdateDestinationNoSettableMembers = new(
        id: "FKF041",
        title: "Update destination has no settable members",
        messageFormat: "Update forge method '{0}' destination type '{1}' has no settable members",
        category: Category_MethodShape,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The update forge method's destination type has no settable properties or fields. There is nothing to update.");

    /// <summary>
    /// FKF050 (Info): A before-hook partial method was detected for this forge method.
    /// </summary>
    public static readonly DiagnosticDescriptor BeforeHookDetected = new(
        id: "FKF050",
        title: "Before hook detected",
        messageFormat: "Before hook '{0}' detected for forge method '{1}'",
        category: Category_MethodShape,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A partial method named OnBefore{MethodName} was found. It will be called before the mapping assignments.");

    /// <summary>
    /// FKF051 (Info): An after-hook partial method was detected for this forge method.
    /// </summary>
    public static readonly DiagnosticDescriptor AfterHookDetected = new(
        id: "FKF051",
        title: "After hook detected",
        messageFormat: "After hook '{0}' detected for forge method '{1}'",
        category: Category_MethodShape,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A partial method named OnAfter{MethodName} was found. It will be called after the mapping assignments.");

    // ─── Member Discovery ─────────────────────────────────────────────────────

    /// <summary>
    /// FKF400 (Warning): A field in the source or destination type was ignored because
    /// ShouldIncludeFields is false on the forge method.
    /// </summary>
    public static readonly DiagnosticDescriptor FieldIgnored = new(
        id: "FKF400",
        title: "Field ignored",
        messageFormat: "Field '{0}' on type '{1}' is ignored because ShouldIncludeFields is false. Set ShouldIncludeFields = true on [ForgeMethod] to include fields.",
        category: Category_MemberDiscovery,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Fields are excluded from member discovery by default. Set ShouldIncludeFields = true on the [ForgeMethod] attribute to include them.");

    /// <summary>
    /// FKF401 (Info): Fields are enabled for this forge method.
    /// </summary>
    public static readonly DiagnosticDescriptor FieldsEnabled = new(
        id: "FKF401",
        title: "Fields enabled",
        messageFormat: "Forge method '{0}' has ShouldIncludeFields = true. Fields will be included in member discovery.",
        category: Category_MemberDiscovery,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The forge method is configured to include fields in member discovery.");

    // ─── Member Matching ──────────────────────────────────────────────────────

    /// <summary>
    /// FKF100 (Warning): A destination member has no matching source member.
    /// The destination member will be left at its default value.
    /// </summary>
    public static readonly DiagnosticDescriptor DestinationMemberMissing = new(
        id: "FKF100",
        title: "Destination member missing source",
        messageFormat: "Destination member '{0}.{1}' has no matching member in source type '{2}'. It will be left at its default value.",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A member exists on the destination type but no member with a matching name was found on the source type.");

    /// <summary>
    /// FKF101 (Warning): A source member has no matching destination member and will not be mapped.
    /// </summary>
    public static readonly DiagnosticDescriptor SourceMemberUnused = new(
        id: "FKF101",
        title: "Source member unused",
        messageFormat: "Source member '{0}.{1}' has no matching member in destination type '{2}' and will not be mapped",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A member exists on the source type but no member with a matching name was found on the destination type.");

    /// <summary>
    /// FKF102 (Info): A member is excluded from mapping via [ForgeIgnore].
    /// </summary>
    public static readonly DiagnosticDescriptor MemberIgnored = new(
        id: "FKF102",
        title: "Member ignored via [ForgeIgnore]",
        messageFormat: "Member '{0}' on type '{1}' is excluded from mapping via [ForgeIgnore]",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "This member is marked with [ForgeIgnore] and will not participate in forge mapping.");

    /// <summary>
    /// FKF103 (Info): A custom member mapping was applied via [ForgeMap].
    /// </summary>
    public static readonly DiagnosticDescriptor CustomMemberMapping = new(
        id: "FKF103",
        title: "Custom member mapping",
        messageFormat: "Member '{0}' on type '{1}' is mapped to counterpart '{2}' via [ForgeMap]",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The member has a [ForgeMap] attribute that maps it to a differently-named member on the counterpart type.");

    /// <summary>
    /// FKF104 (Error): A [ForgeMap] target member was not found on the counterpart type.
    /// </summary>
    public static readonly DiagnosticDescriptor ForgeMapTargetNotFound = new(
        id: "FKF104",
        title: "ForgeMap target not found",
        messageFormat: "Member '{0}' on type '{1}' maps to '{2}' via [ForgeMap], but no member named '{2}' was found on the counterpart type",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The [ForgeMap] attribute specifies a target member name that does not exist on the counterpart type.");

    /// <summary>
    /// FKF105 (Warning): Multiple members map to the same target via [ForgeMap].
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateForgeMapTarget = new(
        id: "FKF105",
        title: "Duplicate ForgeMap target",
        messageFormat: "Multiple members map to the same target key '{0}'. Member '{1}' on type '{2}' conflicts with a previous mapping.",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Two or more members on the same type map to the same counterpart member name via [ForgeMap] or by convention. The later mapping will overwrite the earlier one.");

    /// <summary>
    /// FKF106 (Info): A flattened mapping was applied from a nested source property.
    /// </summary>
    public static readonly DiagnosticDescriptor FlattenedMapping = new(
        id: "FKF106",
        title: "Flattened mapping applied",
        messageFormat: "Destination member '{0}' was mapped via flattening to source path '{1}.{2}'",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The destination member was matched by flattening a nested source property (e.g., AddressCity maps to Address.City).");

    // ─── Strict Mapping (Drift Detection) ──────────────────────────────────────

    /// <summary>
    /// FKF110 (Error): Strict mode — a destination member has no matching source member.
    /// Emitted instead of FKF100 when StrictMapping = true.
    /// </summary>
    public static readonly DiagnosticDescriptor StrictDestinationMemberMissing = new(
        id: "FKF110",
        title: "Strict: destination member missing source",
        messageFormat: "Destination member '{0}.{1}' has no matching member in source type '{2}'. StrictMapping is enabled — all destination members must be mapped.",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "StrictMapping is enabled on this forge method. Every destination member must have a corresponding source member. This catches mapping drift when types change.");

    /// <summary>
    /// FKF111 (Error): Strict mode — a source member has no matching destination member.
    /// Emitted instead of FKF101 when StrictMapping = true.
    /// </summary>
    public static readonly DiagnosticDescriptor StrictSourceMemberUnused = new(
        id: "FKF111",
        title: "Strict: source member unused",
        messageFormat: "Source member '{0}.{1}' has no matching member in destination type '{2}'. StrictMapping is enabled — all source members must be consumed or explicitly ignored.",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "StrictMapping is enabled on this forge method. Every source member must have a corresponding destination member or be excluded via [ForgeIgnore]. This catches mapping drift when types change.");

    // ─── Type Safety ──────────────────────────────────────────────────────────

    /// <summary>
    /// FKF200 (Error): A source member and destination member share a name but have incompatible types,
    /// and no forge method exists to bridge them.
    /// </summary>
    public static readonly DiagnosticDescriptor IncompatibleMemberTypes = new(
        id: "FKF200",
        title: "Incompatible member types",
        messageFormat: "Member '{0}': source type '{1}' is incompatible with destination type '{2}'. No forge conversion is available. Use AllowNestedForging = true and provide a forge method, or exclude this member.",
        category: Category_TypeSafety,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The source and destination members share a name but have different types with no available forge conversion. This is a hard type-safety violation.");

    /// <summary>
    /// FKF201 (Warning): Mapping from a nullable value type to a non-nullable value type uses .Value,
    /// which may throw InvalidOperationException at runtime.
    /// </summary>
    public static readonly DiagnosticDescriptor NullableValueTypeMapping = new(
        id: "FKF201",
        title: "Nullable value type to non-nullable mapping",
        messageFormat: "Member '{0}': mapping nullable value type '{1}' to non-nullable '{2}' will use .Value which may throw at runtime",
        category: Category_TypeSafety,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A Nullable<T> value type is being mapped to its non-nullable counterpart T using .Value, which throws if the source is null.");

    /// <summary>
    /// FKF202 (Info): A nullable mapping was applied automatically.
    /// </summary>
    public static readonly DiagnosticDescriptor NullableMappingApplied = new(
        id: "FKF202",
        title: "Nullable mapping applied",
        messageFormat: "Member '{0}': nullable mapping applied from '{1}' to '{2}'",
        category: Category_TypeSafety,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The source and destination types differ only in nullability. The generator handles this automatically.");

    /// <summary>
    /// FKF210 (Info): An enum cast mapping was applied from source to destination enum type.
    /// </summary>
    public static readonly DiagnosticDescriptor EnumCastMapping = new(
        id: "FKF210",
        title: "Enum cast mapping",
        messageFormat: "Member '{0}': enum cast from '{1}' to '{2}'",
        category: Category_TypeSafety,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The source and destination members are different enum types. A direct cast will be generated.");

    /// <summary>
    /// FKF211 (Info): A name-based enum mapping was applied from source to destination enum type.
    /// </summary>
    public static readonly DiagnosticDescriptor EnumNameMapping = new(
        id: "FKF211",
        title: "Enum name-based mapping",
        messageFormat: "Member '{0}': enum name-based mapping from '{1}' to '{2}'",
        category: Category_TypeSafety,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The source and destination members are different enum types. A switch expression mapping by member name will be generated.");

    /// <summary>
    /// FKF212 (Warning): A source enum member has no corresponding member in the destination enum type.
    /// </summary>
    public static readonly DiagnosticDescriptor EnumMemberMissing = new(
        id: "FKF212",
        title: "Enum member missing in destination",
        messageFormat: "Enum member '{0}' in source type '{1}' has no corresponding member in destination type '{2}'",
        category: Category_TypeSafety,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A member of the source enum type has no matching member (by name) in the destination enum type. This may cause unexpected behavior at runtime.");

    /// <summary>
    /// FKF220 (Info): A type converter method was used for a member mapping.
    /// </summary>
    public static readonly DiagnosticDescriptor ConverterUsed = new(
        id: "FKF220",
        title: "Type converter used",
        messageFormat: "Member '{0}': type converter '{1}' was used to convert from '{2}' to '{3}'",
        category: Category_TypeSafety,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A method marked with [ForgeConverter] was used to bridge the type mismatch for this member.");

    /// <summary>
    /// FKF221 (Warning): A method marked with [ForgeConverter] has an invalid signature and will be ignored.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidConverterSignature = new(
        id: "FKF221",
        title: "Invalid converter signature",
        messageFormat: "Method '{0}' is marked with [ForgeConverter] but has an invalid signature: {1}. The converter will be ignored.",
        category: Category_TypeSafety,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A [ForgeConverter] method must be static, non-void, non-generic, and take exactly one parameter. Methods that don't meet these requirements are silently ignored by the generator, which can cause unexpected FKF200 errors.");

    // ─── Nested / Collections ────────────────────────────────────────────────

    /// <summary>
    /// FKF300 (Warning): A member pair has different types and a forge method exists for the conversion,
    /// but AllowNestedForging is false on this forge method.
    /// </summary>
    public static readonly DiagnosticDescriptor NestedForgingDisabled = new(
        id: "FKF300",
        title: "Nested forging disabled",
        messageFormat: "Member '{0}': source type '{1}' differs from destination type '{2}'. A forge method exists for this conversion but AllowNestedForging is false. Set AllowNestedForging = true on [ForgeMethod] to enable nested forging, or the member will be skipped.",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Nested forging allows the generator to call another forge method to convert a nested member. Enable it explicitly with AllowNestedForging = true.");

    /// <summary>
    /// FKF310 (Info): A collection mapping was applied for this member.
    /// </summary>
    public static readonly DiagnosticDescriptor CollectionMapping = new(
        id: "FKF310",
        title: "Collection mapping applied",
        messageFormat: "Member '{0}': collection mapping from '{1}' to '{2}'",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The source and destination members are both collection types. The generator will map element-by-element using LINQ.");

    // ─── Construction ─────────────────────────────────────────────────────────

    /// <summary>
    /// FKF500 (Error): Multiple constructors on the destination type are equally viable
    /// for initialization from the source type.
    /// </summary>
    public static readonly DiagnosticDescriptor ConstructorAmbiguity = new(
        id: "FKF500",
        title: "Constructor ambiguity",
        messageFormat: "Type '{0}' has multiple constructors that are equally viable for forge construction. Provide a single preferred constructor or add a parameterless constructor.",
        category: Category_Construction,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When multiple constructors on the destination type are all satisfiable from the source members, the forge generator cannot deterministically choose one.");

    /// <summary>
    /// FKF501 (Error): A required constructor parameter on the destination type has no matching
    /// source member to satisfy it.
    /// </summary>
    public static readonly DiagnosticDescriptor MissingConstructorParameter = new(
        id: "FKF501",
        title: "Missing constructor parameter",
        messageFormat: "Constructor parameter '{0}' on type '{1}' has no matching source member in '{2}'. The constructor cannot be satisfied.",
        category: Category_Construction,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A constructor parameter on the destination type has no corresponding member in the source type with a matching name and type.");

    /// <summary>
    /// FKF502 (Error): No viable constructor was found on the destination type.
    /// </summary>
    public static readonly DiagnosticDescriptor NoViableConstructor = new(
        id: "FKF502",
        title: "No viable constructor",
        messageFormat: "Type '{0}' has no viable constructor for forge construction. Provide a parameterless constructor or a constructor whose parameters can all be satisfied from source type '{1}'.",
        category: Category_Construction,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The destination type has no constructor that can be fully satisfied from the available source members.");
}
