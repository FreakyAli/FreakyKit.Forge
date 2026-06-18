using AutoMapper;

namespace ForgeBenchmarks.RealWorld.CrmContactImport;

// ─── Forge ───────────────────────────────────────────────────────────────────

[global::FreakyKit.Forge.Forge]
public static partial class CrmForges
{
    public static partial PhoneDto MapPhone(PhoneEntity source);
    public static partial EmailDto MapEmail(EmailEntity source);
    public static partial CrmAddressDto MapAddress(CrmAddressEntity source);

    [global::FreakyKit.Forge.ForgeMethod(AllowNestedForging = true)]
    public static partial ContactDto MapContact(ContactEntity source);
}

// ─── Hand-written baseline ───────────────────────────────────────────────────

public static class CrmHandWritten
{
    public static ContactDto MapContact(ContactEntity source)
    {
        var dto = new ContactDto
        {
            Id = source.Id,
            ExternalId = source.ExternalId,
            FirstName = source.FirstName,
            LastName = source.LastName,
            JobTitle = source.JobTitle,
            CompanyName = source.CompanyName,
            Source = source.Source,
            CreatedAt = source.CreatedAt,
            LastContactedAt = source.LastContactedAt,
            OwnerEmail = source.OwnerEmail,
            IsSubscribed = source.IsSubscribed,
            Phones = new List<PhoneDto>(source.Phones.Count),
            Emails = new List<EmailDto>(source.Emails.Count),
            Addresses = new List<CrmAddressDto>(source.Addresses.Count),
            Tags = new List<string>(source.Tags),
            CustomFields = new Dictionary<string, string>(source.CustomFields),
        };
        foreach (var p in source.Phones)
            dto.Phones.Add(new PhoneDto { Type = p.Type, Number = p.Number, IsPrimary = p.IsPrimary });
        foreach (var e in source.Emails)
            dto.Emails.Add(new EmailDto { Type = e.Type, Address = e.Address, IsPrimary = e.IsPrimary, IsVerified = e.IsVerified });
        foreach (var a in source.Addresses)
            dto.Addresses.Add(new CrmAddressDto { Type = a.Type, Line1 = a.Line1, City = a.City, State = a.State, PostalCode = a.PostalCode, Country = a.Country });
        return dto;
    }
}

// ─── Mapperly ────────────────────────────────────────────────────────────────

[Riok.Mapperly.Abstractions.Mapper]
public static partial class CrmMapperly
{
    public static partial PhoneDto MapPhone(PhoneEntity source);
    public static partial EmailDto MapEmail(EmailEntity source);
    public static partial CrmAddressDto MapAddress(CrmAddressEntity source);
    public static partial ContactDto MapContact(ContactEntity source);
}

// ─── AutoMapper profile ──────────────────────────────────────────────────────

public class CrmAutoMapperProfile : Profile
{
    public CrmAutoMapperProfile()
    {
        CreateMap<PhoneEntity, PhoneDto>();
        CreateMap<EmailEntity, EmailDto>();
        CreateMap<CrmAddressEntity, CrmAddressDto>();
        CreateMap<ContactEntity, ContactDto>();
    }
}

// ─── Mapster registration ────────────────────────────────────────────────────

public static class CrmMapsterConfig
{
    public static void Register()
    {
        Mapster.TypeAdapterConfig<PhoneEntity, PhoneDto>.NewConfig();
        Mapster.TypeAdapterConfig<EmailEntity, EmailDto>.NewConfig();
        Mapster.TypeAdapterConfig<CrmAddressEntity, CrmAddressDto>.NewConfig();
        Mapster.TypeAdapterConfig<ContactEntity, ContactDto>.NewConfig();
    }
}
