using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Facet.Extensions;
using ForgeBenchmarks.RealWorld.IdentityProvisioning;
using Mapster;

namespace ForgeBenchmarks.RealWorld.Benchmarks;

/// <summary>
/// Identity / user provisioning scenario — user entity with roles, claims, external logins,
/// and audit trail. Lots of nullable strings/timestamps representative of ASP.NET Identity
/// flows. See Scenarios/IdentityProvisioning.md.
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 50, warmupCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class IdentityProvisioningBenchmark
{
    private UserEntity _user = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterSetup.Configure();
        _ = AutoMapperSetup.Mapper;

        _user = new UserEntity
        {
            Id = Guid.Parse("8a7b6c5d-1234-5678-9abc-def012345678"),
            UserName = "ana.gomez@example.com",
            Email = "ana.gomez@example.com",
            EmailConfirmed = true,
            PhoneNumber = "+1-555-0199",
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = true,
            LockoutEnabled = false,
            LockoutEnd = null,
            AccessFailedCount = 0,
            SecurityStamp = "WXYZ1234567890ABCDEFGHIJ",
            CreatedAt = new DateTime(2022, 11, 3, 14, 22, 0),
            LastLoginAt = new DateTime(2024, 9, 14, 9, 8, 15),
            PasswordChangedAt = new DateTime(2024, 3, 1, 11, 0, 0),
            IsActive = true,
            Roles = new List<RoleEntity>
            {
                new() { Name = "Member", Description = "Default user role", AssignedAt = new DateTime(2022, 11, 3), AssignedBy = "registration" },
                new() { Name = "Editor", Description = "Can edit content", AssignedAt = new DateTime(2023, 5, 12), AssignedBy = "admin-pavel" },
                new() { Name = "BetaTester", Description = null, AssignedAt = new DateTime(2024, 7, 1), AssignedBy = "beta-program" },
            },
            Claims = new List<ClaimEntity>
            {
                new() { Type = "sub", Value = "8a7b6c5d", Issuer = "internal" },
                new() { Type = "email_verified", Value = "true", Issuer = "internal" },
                new() { Type = "department", Value = "marketing", Issuer = "hr-sync" },
                new() { Type = "office_location", Value = "Mexico City", Issuer = "hr-sync" },
                new() { Type = "preferred_username", Value = "ana.gomez", Issuer = null },
            },
            ExternalLogins = new List<ExternalLoginEntity>
            {
                new() { Provider = "Google", ProviderKey = "104857...", DisplayName = "Ana Gómez", LinkedAt = new DateTime(2022, 11, 3, 14, 22, 0) },
                new() { Provider = "Microsoft", ProviderKey = "9ac3f8...", DisplayName = "ana@contoso.com", LinkedAt = new DateTime(2023, 1, 18, 10, 0, 0) },
            },
            AuditTrail = new List<AuditEntryEntity>
            {
                new() { At = new DateTime(2024, 9, 14, 9, 8, 15), Action = "Login", IpAddress = "203.0.113.42", UserAgent = "Mozilla/5.0 ...", Succeeded = true },
                new() { At = new DateTime(2024, 9, 13, 17, 30, 0), Action = "Logout", IpAddress = "203.0.113.42", UserAgent = "Mozilla/5.0 ...", Succeeded = true },
                new() { At = new DateTime(2024, 9, 13, 8, 15, 0), Action = "Login", IpAddress = "198.51.100.7", UserAgent = "Mozilla/5.0 ...", Succeeded = true },
                new() { At = new DateTime(2024, 9, 12, 22, 4, 0), Action = "PasswordChangeAttempted", IpAddress = "198.51.100.7", UserAgent = "Mozilla/5.0 ...", Succeeded = false },
                new() { At = new DateTime(2024, 9, 10, 12, 0, 0), Action = "RoleAdded:BetaTester", IpAddress = null, UserAgent = null, Succeeded = true },
            },
        };
    }

    [Benchmark(Baseline = true, Description = "Hand-written")]
    [BenchmarkCategory("IdentityProvisioning")]
    public UserDto HandWritten() => IdentityHandWritten.MapUser(_user);

    [Benchmark(Description = "Forge")]
    [BenchmarkCategory("IdentityProvisioning")]
    public UserDto ForgeGenerated() => IdentityForges.MapUser(_user);

    [Benchmark(Description = "Mapperly")]
    [BenchmarkCategory("IdentityProvisioning")]
    public UserDto Mapperly() => IdentityMapperly.MapUser(_user);

    [Benchmark(Description = "AutoMapper")]
    [BenchmarkCategory("IdentityProvisioning")]
    public UserDto AutoMapper() => AutoMapperSetup.Mapper.Map<UserDto>(_user);

    [Benchmark(Description = "Mapster")]
    [BenchmarkCategory("IdentityProvisioning")]
    public UserDto Mapster() => _user.Adapt<UserDto>();

    [Benchmark(Description = "Facet")]
    [BenchmarkCategory("IdentityProvisioning")]
    public UserFacetDto Facet() => _user.ToFacet<UserEntity, UserFacetDto>();
}
