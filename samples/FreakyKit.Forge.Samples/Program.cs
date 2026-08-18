using FreakyKit.Forge.Samples;
using FreakyKit.Forge.Conventions;

// ─── Sample data ──────────────────────────────────────────────

var person = new Person
{
    Id = 1,
    FirstName = "Alice",
    LastName = "Smith",
    Age = 30,
    Email = "alice@example.com",
    InternalNotes = "VIP customer",
    Score = 95,
    Status = PersonStatus.Active,
    HomeAddress = new Address
    {
        Street = "123 Main St",
        City = "Portland",
        State = "OR",
        ZipCode = "97201"
    },
    Tags = ["vip", "early-adopter", "beta"],
    Orders =
    [
        new Order
        {
            OrderId = 100,
            Product = "Widget",
            Amount = 29.99m,
            CreatedAt = new DateTime(2025, 1, 15),
            Items = [new OrderItem { Sku = "WDG-001", Quantity = 2, UnitPrice = 14.99m }]
        },
        new Order
        {
            OrderId = 101,
            Product = "Gadget",
            Amount = 49.99m,
            CreatedAt = new DateTime(2025, 3, 1),
            Items = [new OrderItem { Sku = "GDG-001", Quantity = 1, UnitPrice = 49.99m }]
        }
    ]
};

// ─── 1. Basic Mapping ─────────────────────────────────────────

PrintHeader("1. Basic Mapping (Implicit Mode)");
var basic = BasicForges.ToPersonDto(person);
Console.WriteLine($"  {person.FirstName} {person.LastName} → PersonDto: Id={basic.Id}, Name={basic.FirstName} {basic.LastName}, Age={basic.Age}");

// ─── 2. Explicit Mode ─────────────────────────────────────────

PrintHeader("2. Explicit Mode");
var explicitDto = ExplicitModeForges.ToExplicitDto(person);
Console.WriteLine($"  Only [ForgeMethod]-decorated methods are generated: {explicitDto.FirstName} {explicitDto.LastName}");

// ─── 3. ForgeMap (Custom Name Mapping) ────────────────────────

PrintHeader("3. ForgeMap (Custom Name Mapping)");
var summary = ForgeMapForges.ToPersonSummary(person);
Console.WriteLine($"  source.FirstName → PersonSummary.Name = \"{summary.Name}\"");

// ─── 4. ForgeIgnore ───────────────────────────────────────────

PrintHeader("4. ForgeIgnore");
var ignored = new PersonIgnored
{
    Id = 1, FirstName = "Alice", LastName = "Smith",
    Email = "alice@example.com", InternalNotes = "SECRET"
};
var publicDto = ForgeIgnoreForges.ToPublicDto(ignored);
Console.WriteLine($"  InternalNotes excluded: Id={publicDto.Id}, Email={publicDto.Email}");

// ─── 5. Field Mapping ─────────────────────────────────────────

PrintHeader("5. Field Mapping (ShouldIncludeFields = true)");
var measurement = new Measurement { Label = "Temperature", Value = 98.6, Unit = "°F" };
var measurementDto = FieldMappingForges.ToMeasurementDto(measurement);
Console.WriteLine($"  {measurementDto.Label}: {measurementDto.Value} {measurementDto.Unit}");

// ─── 6. Enum Mapping ──────────────────────────────────────────

PrintHeader("6. Enum Mapping");
var castDto = EnumMappingForges.ToCastDto(person);
Console.WriteLine($"  Cast strategy:  {person.Status} → {castDto.Status}");
var byNameDto = EnumMappingForges.ToByNameDto(person);
Console.WriteLine($"  ByName strategy: {person.Status} → {byNameDto.Status}");

// ─── 7. Nullable Handling ─────────────────────────────────────

PrintHeader("7. Nullable Handling");
var scoreDto = NullableForges.ToScoreDto(person);
Console.WriteLine($"  int? Score ({person.Score}) → int Score = {scoreDto.Score}");
Console.WriteLine($"  int Age ({person.Age}) → int? Age = {scoreDto.Age}");

// ─── 8. Collection Mapping ────────────────────────────────────

PrintHeader("8. Collection Mapping");
var withTags = CollectionForges.ToWithTags(person);
Console.WriteLine($"  List<string> Tags → string[] Tags: [{string.Join(", ", withTags.Tags)}]");
var withOrders = CollectionForges.ToWithOrders(person);
Console.WriteLine($"  List<Order> → List<OrderDto> ({withOrders.Orders.Count} orders):");
foreach (var o in withOrders.Orders)
    Console.WriteLine($"    OrderId={o.OrderId}, Product={o.Product}, Amount={o.Amount:C}");

// ─── 9. Flattening ────────────────────────────────────────────

PrintHeader("9. Flattening (AllowFlattening = true)");
var flat = FlatteningForges.ToFlatDto(person);
Console.WriteLine($"  source.HomeAddress.City → HomeAddressCity = \"{flat.HomeAddressCity}\"");
Console.WriteLine($"  source.HomeAddress.State → HomeAddressState = \"{flat.HomeAddressState}\"");
Console.WriteLine($"  source.HomeAddress.ZipCode → HomeAddressZipCode = \"{flat.HomeAddressZipCode}\"");

// ─── 10. Nested Forging ───────────────────────────────────────

PrintHeader("10. Nested Forging (AllowNestedForging = true)");
var withAddr = NestedForgingForges.ToWithAddress(person);
Console.WriteLine($"  source.HomeAddress (Address) → AddressDto: {withAddr.HomeAddress.Street}, {withAddr.HomeAddress.City}, {withAddr.HomeAddress.State}");

// ─── 11. Constructor Mapping ──────────────────────────────────

PrintHeader("11. Constructor Mapping");
var ctorSource = new ConstructorSource { Name = "Bob", Age = 25, Email = "bob@example.com" };
var record = ConstructorForges.ToRecordDto(ctorSource);
Console.WriteLine($"  Ctor args: Name=\"{record.Name}\", Age={record.Age}");
Console.WriteLine($"  Property setter: Email=\"{record.Email}\"");

// ─── 12. Type Converters ──────────────────────────────────────

PrintHeader("12. Type Converters ([ForgeConverter])");
var evt = new Event
{
    Title = "Conference",
    OccurredAt = new DateTime(2025, 6, 15, 9, 0, 0),
    Duration = TimeSpan.FromHours(2.5)
};
var eventDto = ConverterForges.ToEventDto(evt);
Console.WriteLine($"  DateTime → string: \"{eventDto.OccurredAt}\"");
Console.WriteLine($"  TimeSpan → double: {eventDto.Duration} hours");

// ─── 13. Before/After Hooks ───────────────────────────────────

PrintHeader("13. Before/After Hooks");
var hooked = HooksForges.ToPersonDtoWithHooks(person);
Console.WriteLine($"  Result: {hooked.FirstName} {hooked.LastName}");

// ─── 14. Update Mapping ───────────────────────────────────────

PrintHeader("14. Update Mapping (void, in-place)");
var mutable = new PersonMutableDto { FirstName = "OldFirst", LastName = "OldLast", Age = 0, Email = "old@example.com" };
Console.WriteLine($"  Before: {mutable.FirstName} {mutable.LastName}, Age={mutable.Age}");
UpdateForges.UpdatePerson(person, mutable);
Console.WriteLine($"  After:  {mutable.FirstName} {mutable.LastName}, Age={mutable.Age}, LastUpdated={mutable.LastUpdated:u}");

// ─── 15. Private Methods ──────────────────────────────────────

PrintHeader("15. Private Methods (ShouldIncludePrivate = true)");
var internalDto = PrivateMethodForges.MapInternal(person);
Console.WriteLine($"  Private forge method result: {internalDto.FirstName}, Age={internalDto.Age}");

// ─── 16. Strict Mapping ──────────────────────────────────────

PrintHeader("16. Strict Mapping (StrictMapping = true)");
var strictSource = new StrictSource { Id = 42, Name = "Strict Alice", Email = "strict@example.com" };
var strictDto = StrictMappingForges.ToStrictDto(strictSource);
Console.WriteLine($"  All members must match: Id={strictDto.Id}, Name={strictDto.Name}, Email={strictDto.Email}");
Console.WriteLine($"  Try adding a property to StrictSource (FKF111) or StrictDto (FKF110) — the build will fail");

// ─── 17. Cross-Class Method Sharing ──────────────────────────

PrintHeader("17. Cross-Class Method Sharing ([ForgeUses])");
var crossDto = PersonCrossClassForges.ToCrossClassDto(person);
Console.WriteLine($"  Address mapped via [ForgeUses]: City={crossDto.HomeAddress.City}, State={crossDto.HomeAddress.State}");

// ─── 18. Conditional Mapping ─────────────────────────────────

PrintHeader("18. Conditional Mapping (IgnoreIfNull)");
var patch = new PersonPatchDto { FirstName = "Updated", LastName = null, Email = "new@example.com", Age = null };
var target = new Person { FirstName = "Original", LastName = "Smith", Email = "old@example.com", Age = 30 };
ConditionalForges.PatchPerson(patch, target);
Console.WriteLine($"  Patch applied: FirstName={target.FirstName} (updated), LastName={target.LastName} (kept), Age={target.Age} (kept)");

// ─── 19. ShareReference ──────────────────────────────────────

PrintHeader("19. ShareReference (Reference Semantics)");
var sharedDto = ShareReferenceForges.ToSharedDto(person);
Console.WriteLine($"  Tags same instance? {ReferenceEquals(person.Tags, sharedDto.Tags)} (method-level: share)");
Console.WriteLine($"  Orders same instance? {ReferenceEquals(person.Orders, sharedDto.Orders)} (per-member override: copy)");

// ─── 20. Dictionary Mapping ──────────────────────────────────

PrintHeader("20. Dictionary Mapping ([ForgeDictionary])");
var settings = new AppSettings { AppName = "Forge Demo", MaxRetries = 3, DebugMode = true };
var dict = DictionaryForges.ToDict(settings);
Console.WriteLine($"  Object → Dict: {string.Join(", ", dict.Select(kv => $"{kv.Key}={kv.Value}"))}");
var roundTripped = DictionaryForges.FromDict(new Dictionary<string, object>
{
    ["appName"] = "RoundTrip",
    ["maxRetries"] = 5,
    ["debugMode"] = false
});
Console.WriteLine($"  Dict → Object: AppName={roundTripped.AppName}, MaxRetries={roundTripped.MaxRetries}, Debug={roundTripped.DebugMode}");

// ─── 21. Expression Projection ───────────────────────────────

PrintHeader("21. Expression Projection (GenerateExpression = true)");
var projDto = ProjectionForges.ToProjectionDto(person);
Console.WriteLine($"  Imperative: Id={projDto.Id}, Name={projDto.FirstName}, Email={projDto.Email}");
var exprResult = ProjectionForges.ToProjectionDtoExpression.Compile()(person);
Console.WriteLine($"  Expression: Id={exprResult.Id}, Name={exprResult.FirstName}, Email={exprResult.Email}");
Console.WriteLine($"  Use with EF Core: dbContext.People.Select(ProjectionForges.ToProjectionDtoExpression)");

// ─── Conventions ──────────────────────────────────────────────

PrintHeader("Bonus: Naming Conventions");
Console.WriteLine($"  ForgeClassName(\"Person\")     = \"{ForgeConventions.ForgeClassName("Person")}\"");
Console.WriteLine($"  ForgeMethodName(\"PersonDto\") = \"{ForgeConventions.ForgeMethodName("PersonDto")}\"");

Console.WriteLine();
Console.WriteLine("All samples completed successfully!");

// ──────────────────────────────────────────────────────────────

static void PrintHeader(string title)
{
    Console.WriteLine();
    Console.WriteLine($"── {title} ──");
}
