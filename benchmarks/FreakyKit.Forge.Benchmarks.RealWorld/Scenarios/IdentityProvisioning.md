# Scenario: Identity / User Provisioning

**Domain:** Authentication / identity management — user account with the full ASP.NET Identity
shape plus an audit trail.

## What this represents

A typical user-account record that gets returned from an identity-provider admin API or
provisioning sync. The shape covers:

- 15 outer-level scalar properties matching ASP.NET Identity's `IdentityUser` (plus a few admin
  fields)
- Heavy nullable string usage (PhoneNumber, SecurityStamp, LockoutEnd, LastLoginAt,
  PasswordChangedAt, etc.) — typical for accounts in mixed states (not all users have
  confirmed phones, locked out, etc.)
- Four parallel collections: Roles, Claims, ExternalLogins, AuditTrail
- All four collections have their own nullable-string fields (RoleDescription, ClaimIssuer,
  ExternalLoginDisplayName, AuditIpAddress, AuditUserAgent)

## Inspiration (hybrid attribution)

Shape is representative of patterns found in:

- **[ASP.NET Identity (Microsoft.AspNetCore.Identity)](https://github.com/dotnet/aspnetcore/tree/main/src/Identity)**
  (MIT) — the `IdentityUser`, `IdentityUserRole`, `IdentityUserClaim`, `IdentityUserLogin`
  schema is the canonical .NET identity model. Our `UserEntity` follows that shape with the
  audit trail collection added (most production systems layer audit on top of IdentityUser).
- **[OpenIddict](https://github.com/openiddict/openiddict-core)** (Apache-2.0) — its
  authorization/application/token entities follow similar audit + claims shapes.
- **[Duende IdentityServer](https://github.com/DuendeSoftware/products)** (commercial, but
  the schema is publicly documented) — its admin-API DTOs use almost identical
  user-with-roles-and-claims shapes.

## Why this is interesting to benchmark

- **High nullable count** — 8 nullable scalars on UserEntity alone. Each nullable adds a
  branch in the mapper's per-field handling.
- **Four parallel collections** of small flat DTOs — exercises the per-collection mapping
  overhead vs the per-element cost.
- **String-heavy** — most properties are strings of varying sizes (short UserName, longer
  SecurityStamp, full-name AuditAction). Different from the decimal-heavy banking scenario.
- **Audit trail growth** — real production systems can have hundreds of audit entries per
  user. The fixture has 5 to keep iterations fast, but the shape supports growth.

## Fixture rationale

The seeded user represents a typical established account: confirmed email but unconfirmed
phone, 2FA enabled, 3 roles (Member + Editor + BetaTester), 5 claims covering identity-provider
sub, email verification, HR-synced department and location, and a username preference. Two
linked external logins (Google + Microsoft work account). Audit trail covers 5 recent events
including one failed password-change attempt. Property values mirror what an admin API for a
SaaS product would return.

## Modifications

None — POCO modelling only. No ASP.NET Identity `[ProtectedPersonalData]` or `[PersonalData]`
attributes used; those are runtime metadata that doesn't affect mapping.
