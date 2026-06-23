namespace ForgeBenchmarks.RealWorld.HealthcarePatient;

// ─── Source entities ─────────────────────────────────────────────────────────

public class PatientEntity
{
    public string Id { get; set; } = "";
    public string Mrn { get; set; } = "";
    public string FamilyName { get; set; } = "";
    public string GivenName { get; set; } = "";
    public DateTime BirthDate { get; set; }
    public AdministrativeGender Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string PreferredLanguage { get; set; } = "";
    public bool Deceased { get; set; }
    public DateTime? DeceasedDate { get; set; }
    public List<ObservationEntity> Observations { get; set; } = new();
    public List<MedicationEntity> Medications { get; set; } = new();
    public List<AllergyEntity> Allergies { get; set; } = new();
    public CoverageEntity Coverage { get; set; } = null!;
}

public class ObservationEntity
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string CodeSystem { get; set; } = "";
    public string Display { get; set; } = "";
    public DateTime EffectiveAt { get; set; }
    public decimal? ValueQuantity { get; set; }
    public string? ValueUnit { get; set; }
    public ObservationStatus Status { get; set; }
}

public class MedicationEntity
{
    public string Id { get; set; } = "";
    public string RxNormCode { get; set; } = "";
    public string Display { get; set; } = "";
    public string Dosage { get; set; } = "";
    public string Frequency { get; set; } = "";
    public DateTime StartedOn { get; set; }
    public DateTime? EndedOn { get; set; }
    public MedicationStatus Status { get; set; }
}

public class AllergyEntity
{
    public string Id { get; set; } = "";
    public string Allergen { get; set; } = "";
    public AllergyCriticality Criticality { get; set; }
    public string? Reaction { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class CoverageEntity
{
    public string PayerName { get; set; } = "";
    public string MemberId { get; set; } = "";
    public string PlanType { get; set; } = "";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public enum AdministrativeGender { Unknown, Male, Female, Other }
public enum ObservationStatus { Preliminary, Final, Amended, Cancelled }
public enum MedicationStatus { Active, Completed, Stopped, OnHold }
public enum AllergyCriticality { Low, High, UnableToAssess }

// ─── DTOs ────────────────────────────────────────────────────────────────────

public class PatientDto
{
    public string Id { get; set; } = "";
    public string Mrn { get; set; } = "";
    public string FamilyName { get; set; } = "";
    public string GivenName { get; set; } = "";
    public DateTime BirthDate { get; set; }
    public AdministrativeGender Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string PreferredLanguage { get; set; } = "";
    public bool Deceased { get; set; }
    public DateTime? DeceasedDate { get; set; }
    public List<ObservationDto> Observations { get; set; } = new();
    public List<MedicationDto> Medications { get; set; } = new();
    public List<AllergyDto> Allergies { get; set; } = new();
    public CoverageDto Coverage { get; set; } = null!;
}

public class ObservationDto
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string CodeSystem { get; set; } = "";
    public string Display { get; set; } = "";
    public DateTime EffectiveAt { get; set; }
    public decimal? ValueQuantity { get; set; }
    public string? ValueUnit { get; set; }
    public ObservationStatus Status { get; set; }
}

public class MedicationDto
{
    public string Id { get; set; } = "";
    public string RxNormCode { get; set; } = "";
    public string Display { get; set; } = "";
    public string Dosage { get; set; } = "";
    public string Frequency { get; set; } = "";
    public DateTime StartedOn { get; set; }
    public DateTime? EndedOn { get; set; }
    public MedicationStatus Status { get; set; }
}

public class AllergyDto
{
    public string Id { get; set; } = "";
    public string Allergen { get; set; } = "";
    public AllergyCriticality Criticality { get; set; }
    public string? Reaction { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class CoverageDto
{
    public string PayerName { get; set; } = "";
    public string MemberId { get; set; } = "";
    public string PlanType { get; set; } = "";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

[Facet.Facet(typeof(ObservationEntity))]
public partial class ObservationFacetDto;

[Facet.Facet(typeof(MedicationEntity))]
public partial class MedicationFacetDto;

[Facet.Facet(typeof(AllergyEntity))]
public partial class AllergyFacetDto;

[Facet.Facet(typeof(CoverageEntity))]
public partial class CoverageFacetDto;

[Facet.Facet(typeof(PatientEntity), NestedFacets = [typeof(ObservationFacetDto), typeof(MedicationFacetDto), typeof(AllergyFacetDto), typeof(CoverageFacetDto)])]
public partial class PatientFacetDto;
