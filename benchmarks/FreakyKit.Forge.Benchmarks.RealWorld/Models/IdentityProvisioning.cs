namespace ForgeBenchmarks.RealWorld.IdentityProvisioning;

// ─── Source entities ─────────────────────────────────────────────────────────

public class UserEntity
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
    public string? SecurityStamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public bool IsActive { get; set; }
    public List<RoleEntity> Roles { get; set; } = new();
    public List<ClaimEntity> Claims { get; set; } = new();
    public List<ExternalLoginEntity> ExternalLogins { get; set; } = new();
    public List<AuditEntryEntity> AuditTrail { get; set; } = new();
}

public class RoleEntity
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime AssignedAt { get; set; }
    public string AssignedBy { get; set; } = "";
}

public class ClaimEntity
{
    public string Type { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Issuer { get; set; }
}

public class ExternalLoginEntity
{
    public string Provider { get; set; } = "";
    public string ProviderKey { get; set; } = "";
    public string? DisplayName { get; set; }
    public DateTime LinkedAt { get; set; }
}

public class AuditEntryEntity
{
    public DateTime At { get; set; }
    public string Action { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Succeeded { get; set; }
}

// ─── DTOs ────────────────────────────────────────────────────────────────────

public class UserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
    public string? SecurityStamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public bool IsActive { get; set; }
    public List<RoleDto> Roles { get; set; } = new();
    public List<ClaimDto> Claims { get; set; } = new();
    public List<ExternalLoginDto> ExternalLogins { get; set; } = new();
    public List<AuditEntryDto> AuditTrail { get; set; } = new();
}

public class RoleDto
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime AssignedAt { get; set; }
    public string AssignedBy { get; set; } = "";
}

public class ClaimDto
{
    public string Type { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Issuer { get; set; }
}

public class ExternalLoginDto
{
    public string Provider { get; set; } = "";
    public string ProviderKey { get; set; } = "";
    public string? DisplayName { get; set; }
    public DateTime LinkedAt { get; set; }
}

public class AuditEntryDto
{
    public DateTime At { get; set; }
    public string Action { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Succeeded { get; set; }
}

[Facet.Facet(typeof(RoleEntity))]
public partial class RoleFacetDto;

[Facet.Facet(typeof(ClaimEntity))]
public partial class ClaimFacetDto;

[Facet.Facet(typeof(ExternalLoginEntity))]
public partial class ExternalLoginFacetDto;

[Facet.Facet(typeof(AuditEntryEntity))]
public partial class AuditEntryFacetDto;

[Facet.Facet(typeof(UserEntity), NestedFacets = [typeof(RoleFacetDto), typeof(ClaimFacetDto), typeof(ExternalLoginFacetDto), typeof(AuditEntryFacetDto)])]
public partial class UserFacetDto;
