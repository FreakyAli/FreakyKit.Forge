using AutoMapper;

namespace ForgeBenchmarks.RealWorld.IdentityProvisioning;

// ─── Forge ───────────────────────────────────────────────────────────────────

[global::FreakyKit.Forge.Forge]
public static partial class IdentityForges
{
    public static partial RoleDto MapRole(RoleEntity source);
    public static partial ClaimDto MapClaim(ClaimEntity source);
    public static partial ExternalLoginDto MapExternalLogin(ExternalLoginEntity source);
    public static partial AuditEntryDto MapAudit(AuditEntryEntity source);

    [global::FreakyKit.Forge.ForgeMethod(AllowNestedForging = true)]
    public static partial UserDto MapUser(UserEntity source);
}

// ─── Hand-written baseline ───────────────────────────────────────────────────

public static class IdentityHandWritten
{
    public static UserDto MapUser(UserEntity s)
    {
        var dto = new UserDto
        {
            Id = s.Id,
            UserName = s.UserName,
            Email = s.Email,
            EmailConfirmed = s.EmailConfirmed,
            PhoneNumber = s.PhoneNumber,
            PhoneNumberConfirmed = s.PhoneNumberConfirmed,
            TwoFactorEnabled = s.TwoFactorEnabled,
            LockoutEnabled = s.LockoutEnabled,
            LockoutEnd = s.LockoutEnd,
            AccessFailedCount = s.AccessFailedCount,
            SecurityStamp = s.SecurityStamp,
            CreatedAt = s.CreatedAt,
            LastLoginAt = s.LastLoginAt,
            PasswordChangedAt = s.PasswordChangedAt,
            IsActive = s.IsActive,
            Roles = new List<RoleDto>(s.Roles.Count),
            Claims = new List<ClaimDto>(s.Claims.Count),
            ExternalLogins = new List<ExternalLoginDto>(s.ExternalLogins.Count),
            AuditTrail = new List<AuditEntryDto>(s.AuditTrail.Count),
        };
        foreach (var r in s.Roles)
            dto.Roles.Add(new RoleDto { Name = r.Name, Description = r.Description, AssignedAt = r.AssignedAt, AssignedBy = r.AssignedBy });
        foreach (var c in s.Claims)
            dto.Claims.Add(new ClaimDto { Type = c.Type, Value = c.Value, Issuer = c.Issuer });
        foreach (var l in s.ExternalLogins)
            dto.ExternalLogins.Add(new ExternalLoginDto { Provider = l.Provider, ProviderKey = l.ProviderKey, DisplayName = l.DisplayName, LinkedAt = l.LinkedAt });
        foreach (var a in s.AuditTrail)
            dto.AuditTrail.Add(new AuditEntryDto { At = a.At, Action = a.Action, IpAddress = a.IpAddress, UserAgent = a.UserAgent, Succeeded = a.Succeeded });
        return dto;
    }
}

// ─── Mapperly ────────────────────────────────────────────────────────────────

[Riok.Mapperly.Abstractions.Mapper]
public static partial class IdentityMapperly
{
    public static partial RoleDto MapRole(RoleEntity source);
    public static partial ClaimDto MapClaim(ClaimEntity source);
    public static partial ExternalLoginDto MapExternalLogin(ExternalLoginEntity source);
    public static partial AuditEntryDto MapAudit(AuditEntryEntity source);
    public static partial UserDto MapUser(UserEntity source);
}

// ─── AutoMapper profile ──────────────────────────────────────────────────────

public class IdentityAutoMapperProfile : Profile
{
    public IdentityAutoMapperProfile()
    {
        CreateMap<RoleEntity, RoleDto>();
        CreateMap<ClaimEntity, ClaimDto>();
        CreateMap<ExternalLoginEntity, ExternalLoginDto>();
        CreateMap<AuditEntryEntity, AuditEntryDto>();
        CreateMap<UserEntity, UserDto>();
    }
}

// ─── Mapster registration ────────────────────────────────────────────────────

public static class IdentityMapsterConfig
{
    public static void Register()
    {
        Mapster.TypeAdapterConfig<RoleEntity, RoleDto>.NewConfig();
        Mapster.TypeAdapterConfig<ClaimEntity, ClaimDto>.NewConfig();
        Mapster.TypeAdapterConfig<ExternalLoginEntity, ExternalLoginDto>.NewConfig();
        Mapster.TypeAdapterConfig<AuditEntryEntity, AuditEntryDto>.NewConfig();
        Mapster.TypeAdapterConfig<UserEntity, UserDto>.NewConfig();
    }
}
