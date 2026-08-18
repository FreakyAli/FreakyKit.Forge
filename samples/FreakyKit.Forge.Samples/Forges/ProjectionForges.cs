
namespace FreakyKit.Forge.Samples;

/// <summary>
/// Expression projections: GenerateExpression = true emits a static
/// Expression&lt;Func&lt;Person, PersonProjectionDto&gt;&gt; property alongside the method.
/// Use it with EF Core IQueryable.Select() to push mapping to SQL.
/// </summary>
[Forge]
public static partial class ProjectionForges
{
    // The generator emits both:
    //   - PersonProjectionDto ToProjectionDto(Person source)     ← imperative method
    //   - Expression<Func<Person, PersonProjectionDto>> ToProjectionDtoExpression  ← static property
    [ForgeMethod(GenerateExpression = true)]
    public static partial PersonProjectionDto ToProjectionDto(Person source);

    // Usage with EF Core:
    //   var dtos = await dbContext.People
    //       .Where(p => p.IsActive)
    //       .Select(ProjectionForges.ToProjectionDtoExpression)
    //       .ToListAsync();
}
