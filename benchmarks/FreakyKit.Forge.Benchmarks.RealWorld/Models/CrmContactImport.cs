namespace ForgeBenchmarks.RealWorld.CrmContactImport;

// ─── Source entities ─────────────────────────────────────────────────────────

public class ContactEntity
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public ContactSource Source { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastContactedAt { get; set; }
    public string OwnerEmail { get; set; } = "";
    public bool IsSubscribed { get; set; }
    public List<PhoneEntity> Phones { get; set; } = new();
    public List<EmailEntity> Emails { get; set; } = new();
    public List<CrmAddressEntity> Addresses { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string> CustomFields { get; set; } = new();
}

public class PhoneEntity
{
    public string Type { get; set; } = "";
    public string Number { get; set; } = "";
    public bool IsPrimary { get; set; }
}

public class EmailEntity
{
    public string Type { get; set; } = "";
    public string Address { get; set; } = "";
    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }
}

public class CrmAddressEntity
{
    public string Type { get; set; } = "";
    public string Line1 { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "";
}

public enum ContactSource { WebForm, ImportedFile, ApiSync, ManualEntry, ReferralProgram }

// ─── DTOs ────────────────────────────────────────────────────────────────────

public class ContactDto
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public ContactSource Source { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastContactedAt { get; set; }
    public string OwnerEmail { get; set; } = "";
    public bool IsSubscribed { get; set; }
    public List<PhoneDto> Phones { get; set; } = new();
    public List<EmailDto> Emails { get; set; } = new();
    public List<CrmAddressDto> Addresses { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string> CustomFields { get; set; } = new();
}

public class PhoneDto
{
    public string Type { get; set; } = "";
    public string Number { get; set; } = "";
    public bool IsPrimary { get; set; }
}

public class EmailDto
{
    public string Type { get; set; } = "";
    public string Address { get; set; } = "";
    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }
}

public class CrmAddressDto
{
    public string Type { get; set; } = "";
    public string Line1 { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "";
}

[Facet.Facet(typeof(PhoneEntity))]
public partial class PhoneFacetDto;

[Facet.Facet(typeof(EmailEntity))]
public partial class EmailFacetDto;

[Facet.Facet(typeof(CrmAddressEntity))]
public partial class CrmAddressFacetDto;

[Facet.Facet(typeof(ContactEntity), NestedFacets = [typeof(PhoneFacetDto), typeof(EmailFacetDto), typeof(CrmAddressFacetDto)])]
public partial class ContactFacetDto;
