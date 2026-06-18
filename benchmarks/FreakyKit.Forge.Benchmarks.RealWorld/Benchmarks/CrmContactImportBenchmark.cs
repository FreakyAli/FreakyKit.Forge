using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Facet.Extensions;
using ForgeBenchmarks.RealWorld.CrmContactImport;
using Mapster;

namespace ForgeBenchmarks.RealWorld.Benchmarks;

/// <summary>
/// CRM contact import scenario — contact with multiple phones/emails/addresses, tag list, and a
/// custom-field dictionary. Tests dictionary mapping and unbounded collection growth. See
/// Scenarios/CrmContactImport.md for provenance.
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 50, warmupCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class CrmContactImportBenchmark
{
    private ContactEntity _contact = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterSetup.Configure();
        _ = AutoMapperSetup.Mapper;

        _contact = new ContactEntity
        {
            Id = 50128,
            ExternalId = "salesforce-003a1c8d2",
            FirstName = "Marcus",
            LastName = "Walden",
            JobTitle = "VP Procurement",
            CompanyName = "Northwind Industrial",
            Source = ContactSource.ApiSync,
            CreatedAt = new DateTime(2023, 5, 12, 9, 22, 0),
            LastContactedAt = new DateTime(2024, 8, 28, 16, 0, 0),
            OwnerEmail = "alex@example.com",
            IsSubscribed = true,
            Phones = new List<PhoneEntity>
            {
                new() { Type = "work", Number = "+1-555-0142", IsPrimary = true },
                new() { Type = "mobile", Number = "+1-555-0177", IsPrimary = false },
                new() { Type = "assistant", Number = "+1-555-0188", IsPrimary = false },
            },
            Emails = new List<EmailEntity>
            {
                new() { Type = "work", Address = "marcus.walden@northwind.example", IsPrimary = true, IsVerified = true },
                new() { Type = "personal", Address = "mwalden@gmail.example", IsPrimary = false, IsVerified = false },
            },
            Addresses = new List<CrmAddressEntity>
            {
                new() { Type = "work", Line1 = "100 Industrial Pkwy", City = "Pittsburgh", State = "PA", PostalCode = "15201", Country = "USA" },
                new() { Type = "home", Line1 = "47 Oak Lane", City = "Sewickley", State = "PA", PostalCode = "15143", Country = "USA" },
            },
            Tags = new List<string> { "enterprise", "decision-maker", "q4-target", "renewal-2025", "manufacturing" },
            CustomFields = new Dictionary<string, string>
            {
                ["annual_spend_band"] = "$500K-$1M",
                ["preferred_contact_method"] = "email",
                ["renewal_quarter"] = "Q1-2025",
                ["account_segment"] = "Strategic",
                ["nps_score"] = "9",
                ["industry"] = "Industrial Manufacturing",
                ["employees"] = "501-1000",
                ["region"] = "Northeast US",
            },
        };
    }

    [Benchmark(Baseline = true, Description = "Hand-written")]
    [BenchmarkCategory("CrmContactImport")]
    public ContactDto HandWritten() => CrmHandWritten.MapContact(_contact);

    [Benchmark(Description = "Forge")]
    [BenchmarkCategory("CrmContactImport")]
    public ContactDto ForgeGenerated() => CrmForges.MapContact(_contact);

    [Benchmark(Description = "Mapperly")]
    [BenchmarkCategory("CrmContactImport")]
    public ContactDto Mapperly() => CrmMapperly.MapContact(_contact);

    [Benchmark(Description = "AutoMapper")]
    [BenchmarkCategory("CrmContactImport")]
    public ContactDto AutoMapper() => AutoMapperSetup.Mapper.Map<ContactDto>(_contact);

    [Benchmark(Description = "Mapster")]
    [BenchmarkCategory("CrmContactImport")]
    public ContactDto Mapster() => _contact.Adapt<ContactDto>();

    [Benchmark(Description = "Facet")]
    [BenchmarkCategory("CrmContactImport")]
    public ContactFacetDto Facet() => _contact.ToFacet<ContactEntity, ContactFacetDto>();
}
