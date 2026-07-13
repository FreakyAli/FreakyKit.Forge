using Microsoft.CodeAnalysis;

namespace FreakyKit.Forge.Diagnostics;

/// <summary>
/// Single source of truth for all FreakyKit.Forge diagnostic descriptors.
/// All diagnostic IDs, titles, messages, severities, and categories are defined here.
/// No diagnostics may be defined anywhere else.
/// </summary>
/// <remarks>
/// DIAGNOSTIC ID ALLOCATION SCHEME
/// ===============================
/// Diagnostic IDs are allocated by range to prevent collisions and enable reserved space for future features.
///
/// Current allocation:
///   FKF001–099: Mode & class-level validation (Explicit mode, static class, access level, etc.)
///   FKF100–199: Member discovery & matching (constructor selection, member mapping, name resolution)
///   FKF200–299: Type safety & conversion (nullable/value type mismatches, incompatible types, converters)
///   FKF300–399: Nested forging & circularity (circular reference detection, nested method discovery)
///   FKF400–499: Construction & initialization (null fallback, init-only properties, etc.)
///   FKF500–599: RESERVED for P2/P3 feature diagnostics (Computed Properties, Conditional Mapping, Cross-Class Forge, etc.)
///   FKF600–699: RESERVED for performance warnings (deep nesting, expression complexity, etc.)
///
/// When adding new features (P2/P3), allocate diagnostic IDs from the FKF500–599 range.
/// When adding performance diagnostics, allocate from FKF600–699.
/// This prevents ID collisions across versions and enables predictable diagnostic management.
/// </remarks>
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
    /// FKF003 (Error): A class has [Forge] but is not declared static.
    /// </summary>
    public static readonly DiagnosticDescriptor ForgeClassNotStatic = new(
        id: "FKF003",
        title: "Forge class not static",
        messageFormat: "Forge class '{0}' is not static. Forge classes must be declared static.",
        category: Category_Mode,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The class has a [Forge] attribute but is not declared static. The generator produces static mapping methods and requires a static class.");

    /// <summary>
    /// FKF004 (Error): A class has [Forge] but is not declared partial.
    /// </summary>
    public static readonly DiagnosticDescriptor ForgeClassNotPartial = new(
        id: "FKF004",
        title: "Forge class not partial",
        messageFormat: "Forge class '{0}' is not partial. Forge classes must be declared partial so the generator can add the implementation.",
        category: Category_Mode,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The class has a [Forge] attribute but is not declared partial. The source generator adds a second partial class declaration; without partial, the generated code cannot be merged.");

    /// <summary>
    /// FKF005 (Error): [Forge] is applied to a non-class type (struct, interface, enum, etc.).
    /// </summary>
    public static readonly DiagnosticDescriptor ForgeOnNonClassType = new(
        id: "FKF005",
        title: "Forge attribute on non-class type",
        messageFormat: "[Forge] on '{0}' has no effect. Only static partial classes are supported as forge containers.",
        category: Category_Mode,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The [Forge] attribute was applied to a struct, interface, record, or other non-class type. The source generator only processes static partial classes.");

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
    /// FKF042 (Warning): A forge method produces zero member assignments.
    /// </summary>
    public static readonly DiagnosticDescriptor ZeroMembersMapped = new(
        id: "FKF042",
        title: "Zero members mapped",
        messageFormat: "Forge method '{0}' produces no member assignments. Source type '{1}' and destination type '{2}' have no matchable members.",
        category: Category_MethodShape,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "No members on the source and destination types share a matching name. The generated method will be empty. Check that the types are correct or that member names align.");

    /// <summary>
    /// FKF043 (Warning): AllowFlattening is enabled but no destination members were matched via flattening.
    /// </summary>
    public static readonly DiagnosticDescriptor FlatteningEnabledNoMatchFound = new(
        id: "FKF043",
        title: "Flattening enabled but no members flattened",
        messageFormat: "Forge method '{0}' has AllowFlattening = true but no destination members were matched via flattening. Check that destination member names follow the pattern '{{NavigationProperty}}{{NestedProperty}}' (e.g., AddressCity for Address.City).",
        category: Category_MethodShape,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "AllowFlattening was enabled on this forge method but no destination members were resolved by decomposing nested source properties. Either no destination member names follow the flattening convention, or AllowFlattening is unnecessary.");

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

    /// <summary>
    /// FKF107 (Info): A destination member matches a source member by name but is read-only and cannot be assigned.
    /// </summary>
    public static readonly DiagnosticDescriptor ReadOnlyDestinationMember = new(
        id: "FKF107",
        title: "Read-only destination member skipped",
        messageFormat: "Destination member '{0}.{1}' matches a source member but is read-only and cannot be assigned. Add a setter or exclude it with [ForgeIgnore].",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The destination member has a matching source member but cannot be written to because it has no setter. The mapping is silently skipped.");

    /// <summary>
    /// FKF108 (Info): A source member has no getter and cannot be read from during mapping.
    /// </summary>
    public static readonly DiagnosticDescriptor WriteOnlySourceMember = new(
        id: "FKF108",
        title: "Write-only source member skipped",
        messageFormat: "Source member '{0}.{1}' has no getter and cannot be read. It will not be mapped.",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "The source member is write-only (setter only, no getter). It cannot be read during mapping and will be excluded from member discovery.");

    /// <summary>
    /// FKF109 (Warning): A member has both [ForgeIgnore] and [ForgeMap], which is a conflicting configuration.
    /// </summary>
    public static readonly DiagnosticDescriptor MemberBothIgnoredAndMapped = new(
        id: "FKF109",
        title: "Member both ignored and explicitly mapped",
        messageFormat: "Member '{0}' on type '{1}' has both [ForgeIgnore] and [ForgeMap]. [ForgeIgnore] takes precedence — [ForgeMap] has no effect.",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A member cannot be both excluded from mapping and explicitly mapped at the same time. [ForgeIgnore] wins — remove [ForgeMap] or remove [ForgeIgnore].");

    /// <summary>
    /// FKF112 (Warning): A [ForgeMap] attribute maps a member to its own name — a no-op.
    /// </summary>
    public static readonly DiagnosticDescriptor ForgeMapSelfReference = new(
        id: "FKF112",
        title: "ForgeMap target is the member's own name",
        messageFormat: "Member '{0}' on type '{1}' has [ForgeMap(\"{2}\")] which maps to its own name. [ForgeMap] has no effect — remove it.",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The [ForgeMap] attribute specifies the same name as the member itself. This is a no-op and is almost certainly a copy-paste mistake. Remove [ForgeMap] or correct the target name.");

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
        messageFormat: "Member '{0}': mapping nullable value type '{1}' to non-nullable '{2}' will use .Value which may throw at runtime. Set DefaultValue on [ForgeMap] to provide a fallback value instead.",
        category: Category_TypeSafety,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A Nullable<T> value type is being mapped to its non-nullable counterpart T using .Value, which throws if the source is null. Set DefaultValue on [ForgeMap] to provide a fallback value that will be used instead of calling .Value.");

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
    /// FKF203 (Warning): A potentially lossy implicit numeric conversion was applied.
    /// </summary>
    public static readonly DiagnosticDescriptor LossyImplicitConversion = new(
        id: "FKF203",
        title: "Lossy implicit conversion",
        messageFormat: "Member '{0}': implicit conversion from '{1}' to '{2}' may lose precision or data",
        category: Category_TypeSafety,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A safe implicit numeric conversion was applied, but it may lose precision. Examples: float->double, int/uint->float, long/ulong->float/double.");

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

    /// <summary>
    /// FKF222 (Warning): Multiple [ForgeConverter] methods handle the same source→destination type pair.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateConverterForTypePair = new(
        id: "FKF222",
        title: "Duplicate converter for type pair",
        messageFormat: "Forge class '{0}' has multiple [ForgeConverter] methods that convert from '{1}' to '{2}'. Only one converter per type pair is allowed; duplicates will be ignored.",
        category: Category_TypeSafety,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Two or more methods in the forge class are marked with [ForgeConverter] and handle the same source-to-destination type pair. Only one will be used; the others will be silently ignored by the generator.");

    /// <summary>
    /// FKF230 (Info): An enum ↔ string mapping was applied for this member.
    /// </summary>
    public static readonly DiagnosticDescriptor EnumStringMapping = new(
        id: "FKF230",
        title: "Enum ↔ string mapping applied",
        messageFormat: "Member '{0}': enum ↔ string mapping from '{1}' to '{2}'",
        category: Category_TypeSafety,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "One member is an enum and the other is a string. The generator automatically converts between them using ToString() for enum→string and Enum.Parse for string→enum.");

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
    /// FKF301 (Error): Circular nested forge detected—a forge method with AllowNestedForging=true
    /// calls another forge method that (directly or indirectly) calls back to the first method.
    /// </summary>
    public static readonly DiagnosticDescriptor CircularNestedForge = new(
        id: "FKF301",
        title: "Circular nested forge detected",
        messageFormat: "Circular nested forge detected: {0}. Circular references prevent code generation. Break the cycle by setting AllowNestedForging=false on one of the methods, or by not using nested forging for one of the member assignments.",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Nested forging creates a directed graph of method-to-method calls. A cycle in this graph (e.g., ToDto calls ToAddressDto which calls back to ToDto) cannot be resolved at compile time. Break the cycle by disabling nested forging on one of the members in the cycle, or by using a distinct type that breaks the loop.");

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

    /// <summary>
    /// FKF311 (Info): A same-type mutable collection member is reference-shared with the source
    /// (rather than deep-copied) because ShareReference is in effect.
    /// </summary>
    public static readonly DiagnosticDescriptor SameTypeCollectionShared = new(
        id: "FKF311",
        title: "Same-type collection reference-shared",
        messageFormat: "Member '{0}' is reference-shared with the source collection because ShareReference is true. Mutations to the destination will affect the source.",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "When ShareReference is true (method-level or per-member), same-type mutable collection members are assigned by reference rather than copy-constructed. Faster and allocation-free, but the source and destination share the same collection instance, so mutations leak across.");

    /// <summary>
    /// FKF312 (Info): A same-type mutable custom-class member is reference-shared with the source.
    /// </summary>
    public static readonly DiagnosticDescriptor SameTypeReferenceShared = new(
        id: "FKF312",
        title: "Same-type reference member shared",
        messageFormat: "Member '{0}' is the same type '{1}' on both source and destination and is shared by reference. Mutations to the destination will affect the source. Use a distinct DTO type with AllowNestedForging + a forge method to deep-copy.",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Same-type reference-typed members (custom classes appearing on both source and destination) are assigned by reference. Forge does not auto-clone custom classes; to get an independent copy, use a distinct DTO type and AllowNestedForging with a forge method that maps the type.");

    /// <summary>
    /// FKF313 (Warning): Source-side and destination-side [ForgeMap] both set ShareReference to
    /// conflicting values.
    /// </summary>
    public static readonly DiagnosticDescriptor ShareReferenceConflict = new(
        id: "FKF313",
        title: "Conflicting ShareReference between source and destination",
        messageFormat: "Member '{0}': source-side [ForgeMap] sets ShareReference={1} but destination-side sets ShareReference={2}. The destination-side value ({2}) is used.",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When source-side and destination-side [ForgeMap] both explicitly set ShareReference with different values, the destination-side wins (the DTO author's intent for ownership semantics). Remove one of the conflicting attributes to silence this warning.");

    /// <summary>
    /// FKF314 (Warning): NullFallback is set on a value type member, which has no effect
    /// since value types cannot be null.
    /// </summary>
    public static readonly DiagnosticDescriptor NullFallbackOnValueType = new(
        id: "FKF314",
        title: "NullFallback has no effect on value type",
        messageFormat: "Member '{0}': NullFallback has no effect because the source member is a value type and cannot be null",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "NullFallback only applies to reference type members. Value types cannot be null, so the fallback strategy is never used. Remove the NullFallback attribute.");

    /// <summary>
    /// FKF315 (Error): Both IgnoreIfNull and NullFallback are set on the same member.
    /// These attributes conflict because they represent different null-handling strategies.
    /// </summary>
    public static readonly DiagnosticDescriptor IgnoreIfNullAndNullFallbackConflict = new(
        id: "FKF315",
        title: "IgnoreIfNull and NullFallback cannot both be set",
        messageFormat: "Member '{0}': Both IgnoreIfNull and NullFallback are set. These attributes are mutually exclusive — choose one strategy for handling null values.",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "IgnoreIfNull skips assignment when null; NullFallback provides a fallback value when null. Only one can be used per member.");

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

    /// <summary>
    /// FKF503 (Error): The destination type is abstract, an interface, or a static class and cannot be instantiated.
    /// </summary>
    public static readonly DiagnosticDescriptor DestinationTypeNotInstantiable = new(
        id: "FKF503",
        title: "Destination type not instantiable",
        messageFormat: "Destination type '{0}' cannot be constructed because it is {1}. Map to a concrete type instead.",
        category: Category_Construction,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The destination type is abstract, an interface, or a static class and cannot be instantiated with 'new'. Provide a concrete, non-static class as the forge destination.");

    /// <summary>
    /// FKF504 (Error): GenerateExpression = true is incompatible with update method shape.
    /// </summary>
    public static readonly DiagnosticDescriptor ExpressionIncompatibleWithUpdate = new(
        id: "FKF504",
        title: "Expression generation incompatible with update method",
        messageFormat: "Forge method '{0}' has GenerateExpression = true but is an update method (void return, two parameters). Expressions can only be generated for create methods.",
        category: Category_MethodShape,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Expression<Func<TSource, TDest>> properties model pure functions. Update methods modify state in place and have no return value, so no expression can be generated.");

    /// <summary>
    /// FKF505 (Warning): Before/after hooks are ignored when generating an expression property.
    /// </summary>
    public static readonly DiagnosticDescriptor ExpressionIgnoresHooks = new(
        id: "FKF505",
        title: "Hooks ignored in generated expression",
        messageFormat: "Forge method '{0}' has GenerateExpression = true but defines a before/after hook; the hook will be invoked from the imperative method but not from the generated expression property.",
        category: Category_MethodShape,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Expression trees model pure data flow and cannot invoke arbitrary side-effectful methods. Before/after hooks remain wired into the imperative partial method body but are omitted from the generated Expression<Func<,>> property.");

    /// <summary>
    /// FKF506 (Info): A member was excluded from the generated expression property because its
    /// conversion has no translatable expression-tree encoding.
    /// </summary>
    public static readonly DiagnosticDescriptor ExpressionMemberExcluded = new(
        id: "FKF506",
        title: "Member excluded from generated expression",
        messageFormat: "Member '{0}' was excluded from the generated expression property: {1}. The imperative method still maps this member normally.",
        category: Category_TypeSafety,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Some mapping cases (custom converters, conditional null skipping, non-translatable collection materializers) have no equivalent encoding inside an Expression<Func<,>>. The member is mapped normally by the imperative method but omitted from the generated expression property.");

    /// <summary>
    /// FKF507 (Error): RESERVED — Cycles are now caught by DetectCircularNestedForge (FKF301) before reaching expression inlining.
    /// This descriptor is kept for diagnostic ID stability but will never be emitted.
    /// </summary>
    public static readonly DiagnosticDescriptor ExpressionNestedCycle = new(
        id: "FKF507",
        title: "Circular nested forge in expression property",
        messageFormat: "Expression property for '{0}' cannot be generated because the nested forge call chain contains a cycle: {1}. Inlining would produce infinite source.",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Expression properties must inline nested forge methods because EF cannot translate Expression.Invoke. A cycle in the nested call chain (A → B → A or longer) would inline forever. Either break the cycle or remove GenerateExpression from the involved methods.");

    /// <summary>
    /// FKF508 (Info): A deeply-nested expression property was inlined.
    /// </summary>
    public static readonly DiagnosticDescriptor ExpressionDeepNesting = new(
        id: "FKF508",
        title: "Deep nested-forge inlining in expression property",
        messageFormat: "Expression property for '{0}' inlines nested forge methods {1} levels deep. The generated source size grows multiplicatively; consider whether flattening or a converter would be cleaner.",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Each level of nested-forge inlining substitutes the full body of the nested expression into the outer one. Deep chains can produce large generated source files. This diagnostic fires when depth exceeds 5 to surface the cost; no action is required.");

    /// <summary>
    /// FKF509 (Error): Expression nesting depth limit exceeded.
    /// </summary>
    public static readonly DiagnosticDescriptor ExpressionNestingDepthLimitExceeded = new(
        id: "FKF509",
        title: "Expression nesting depth limit exceeded",
        messageFormat: "Expression property for '{0}' exceeds the maximum nesting depth of 10 levels. This generates excessive source code and may cause compiler errors. Consider using flattening or a converter instead of nested-forge inlining.",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Expression property nesting is limited to 10 levels to prevent unbounded source code growth and compiler errors. Restructure the mapping to use flattening or converters instead.");

    /// <summary>
    /// FKF510 (Error): A condition method referenced in [ForgeMap] was not found.
    /// </summary>
    public static readonly DiagnosticDescriptor ConditionMethodNotFound = new(
        id: "FKF510",
        title: "Condition method not found",
        messageFormat: "Member '{0}': condition method '{1}' not found on forge class or included classes. Ensure the method exists and is static.",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The [ForgeMap] Condition property references a method that does not exist on the forge class or its included classes.");

    /// <summary>
    /// FKF511 (Error): A condition method has an invalid signature.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidConditionMethodSignature = new(
        id: "FKF511",
        title: "Condition method has invalid signature",
        messageFormat: "Member '{0}': condition method '{1}' has invalid signature. Must be: static bool MethodName(SourceType source)",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Condition methods must be static, accept exactly one parameter of the source type, and return bool.");

    /// <summary>
    /// FKF512 (Error): A condition method is not accessible from the forge class.
    /// </summary>
    public static readonly DiagnosticDescriptor ConditionMethodNotAccessible = new(
        id: "FKF512",
        title: "Condition method not accessible",
        messageFormat: "Member '{0}': condition method '{1}' is not accessible. Methods must be public or internal.",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Condition methods must be publicly or internally accessible to be discovered and called by the generator.");

    // ─── Cross-Class Nested Forge ────────────────────────────────────────────

    /// <summary>
    /// FKF520 (Error): An included forge class in [ForgeUses] was not found.
    /// </summary>
    public static readonly DiagnosticDescriptor IncludedForgeClassNotFound = new(
        id: "FKF520",
        title: "Included forge class not found",
        messageFormat: "Included forge class '{0}' not found. Verify the type name and assembly.",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The type specified in [ForgeUses] could not be resolved.");

    /// <summary>
    /// FKF521 (Error): An included class in [ForgeUses] is not decorated with [Forge].
    /// </summary>
    public static readonly DiagnosticDescriptor IncludedClassNotForge = new(
        id: "FKF521",
        title: "Included class is not a forge class",
        messageFormat: "Included class '{0}' is not decorated with [Forge]. Only forge classes can be included.",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[ForgeUses] can only include classes decorated with [Forge].");

    /// <summary>
    /// FKF522 (Error): Circular includes detected in [ForgeUses] attributes.
    /// </summary>
    public static readonly DiagnosticDescriptor CircularForgeIncludes = new(
        id: "FKF522",
        title: "Circular forge class includes detected",
        messageFormat: "Circular includes detected: {0}. Each forge class can only be included once in the chain.",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[ForgeUses] creates a circular dependency. Restructure the includes to avoid cycles.");

    /// <summary>
    /// FKF523 (Warning): A nested forge method is shadowed by another included class.
    /// </summary>
    public static readonly DiagnosticDescriptor ShadowedNestedForgeMethod = new(
        id: "FKF523",
        title: "Nested forge method shadowed by included class",
        messageFormat: "Member '{0}': Method '{1}' exists in multiple included forge classes. Using '{2}' (first match); '{3}' is shadowed.",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Multiple included forge classes have methods for the same type mapping. The first included class in [ForgeUses] is used; others are shadowed. Reorder classes if intentional.");

    /// <summary>
    /// FKF524 (Error): A class has [ForgeUses] but is missing [Forge] attribute.
    /// </summary>
    public static readonly DiagnosticDescriptor ForgeUsesMissingForgeAttribute = new(
        id: "FKF524",
        title: "ForgeUses requires Forge attribute",
        messageFormat: "Class '{0}' has [ForgeUses] attribute but is missing the [Forge] attribute. Add [Forge] to enable forge functionality.",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[ForgeUses] can only be used on classes decorated with [Forge]. The class must be a static partial class with the [Forge] attribute to use [ForgeUses].");

    /// <summary>
    /// FKF525 (Error): A method has [ForgeMethod] but is not in a [Forge] class.
    /// </summary>
    public static readonly DiagnosticDescriptor ForgeMethodWithoutForgeClass = new(
        id: "FKF525",
        title: "ForgeMethod without Forge class",
        messageFormat: "Method '{0}' has [ForgeMethod] but is not in a [Forge] class. Add [Forge] to the containing class to enable forge functionality.",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[ForgeMethod] can only be used on methods in classes decorated with [Forge]. The containing class must be a static partial class with the [Forge] attribute.");

    /// <summary>
    /// FKF526 (Error): A method has [ForgeConverter] but is not in a [Forge] class.
    /// </summary>
    public static readonly DiagnosticDescriptor ForgeConverterWithoutForgeClass = new(
        id: "FKF526",
        title: "ForgeConverter without Forge class",
        messageFormat: "Method '{0}' has [ForgeConverter] but is not in a [Forge] class. Add [Forge] to the containing class to enable forge functionality.",
        category: Category_Nested,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[ForgeConverter] can only be used on methods in classes decorated with [Forge]. The containing class must be a static partial class with the [Forge] attribute.");

    /// <summary>
    /// FKF527 (Warning): A member has [ForgeMap] but it is on a source type (not a destination type).
    /// [ForgeMap] only affects destination type members.
    /// </summary>
    public static readonly DiagnosticDescriptor ForgeMapOnSourceMember = new(
        id: "FKF527",
        title: "ForgeMap on source type member",
        messageFormat: "[ForgeMap] on '{0}' has no effect. [ForgeMap] is meant for destination type members. This attribute only takes effect when applied to the actual destination type being generated to.",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "[ForgeMap] attributes are typically placed on destination type members to customize their mapping behavior. If this attribute appears on a source type (the type being mapped from), it has no effect and should be removed or moved to the destination type.");

    /// <summary>
    /// FKF528 (Warning): A member has [ForgeIgnore] but it is on a source type (not a destination type).
    /// [ForgeIgnore] only affects destination type members.
    /// </summary>
    public static readonly DiagnosticDescriptor ForgeIgnoreOnSourceMember = new(
        id: "FKF528",
        title: "ForgeIgnore on source type member",
        messageFormat: "[ForgeIgnore] on '{0}' has no effect. [ForgeIgnore] is meant for destination type members. This attribute only takes effect when applied to the actual destination type being generated to.",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "[ForgeIgnore] attributes are typically placed on destination type members to exclude them from mapping. If this attribute appears on a source type (the type being mapped from), it has no effect and should be removed or moved to the destination type.");

    /// <summary>
    /// FKF530 (Error): Ambiguous flattening was auto-resolved by preferring the longest prefix match.
    /// When multiple source property paths could match a destination member name, the generator
    /// uses the longest-matching prefix to disambiguate, then continues recursively.
    /// This is an error to force explicit resolution and prevent silent bugs from unclear mappings.
    /// </summary>
    public static readonly DiagnosticDescriptor AmbiguousFlatteningAutoResolved = new(
        id: "FKF530",
        title: "Ambiguous flattening auto-resolved",
        messageFormat: "Destination member '{0}' matched via ambiguous flattening: multiple prefixes could match '{1}'. The longest prefix '{2}' was selected. Ambiguous flattening is not allowed — explicitly resolve this by renaming the destination member or excluding one of the source properties with [ForgeIgnore].",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Ambiguous flattening occurs when multiple source property paths could match a destination member name's prefix. The generator uses a greedy longest-prefix strategy to disambiguate, but this can hide bugs and lead to unexpected mappings. This is now an error to force explicit, intentional naming. Resolve it by renaming the destination member to be unambiguous, excluding one of the ambiguous source properties with [ForgeIgnore], or adjusting your type structure to eliminate the ambiguity.");

    /// <summary>
    /// FKF531 (Info): Deep flattening (3+ levels) was detected on a destination member.
    /// This informs users that the generated code contains deep property access chains,
    /// which may indicate complex data structure mapping that could be simplified.
    /// </summary>
    public static readonly DiagnosticDescriptor DeepFlatteningDetected = new(
        id: "FKF531",
        title: "Deep flattening detected",
        messageFormat: "Destination member '{0}' uses deep flattening with {1} levels: {2}. Consider whether a simpler structure or nested forging would improve code clarity.",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A destination member was matched via flattening that traverses 3 or more levels of nested properties. While supported, this may indicate overly-deep nesting that could be simplified via a different design. No action required—this is informational only.");

    /// <summary>
    /// FKF532 (Error): Flattening nesting depth exceeded the limit of 10 levels.
    /// </summary>
    public static readonly DiagnosticDescriptor FlatteningDepthLimitExceeded = new(
        id: "FKF532",
        title: "Flattening nesting depth limit exceeded",
        messageFormat: "Destination member '{0}' exceeds the maximum flattening depth of 10 levels. Flattening stopped. Consider restructuring the source type hierarchy or using nested forging instead.",
        category: Category_MemberMatching,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Flattening is limited to 10 levels of nesting to prevent unbounded recursive traversal. Restructure the source type hierarchy to be less deeply nested, or use nested forging instead of flattening for this member.");
}
