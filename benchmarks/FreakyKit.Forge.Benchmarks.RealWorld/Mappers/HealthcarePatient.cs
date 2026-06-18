using AutoMapper;

namespace ForgeBenchmarks.RealWorld.HealthcarePatient;

// ─── Forge ───────────────────────────────────────────────────────────────────

[global::FreakyKit.Forge.Forge]
public static partial class HealthcareForges
{
    public static partial ObservationDto MapObservation(ObservationEntity source);
    public static partial MedicationDto MapMedication(MedicationEntity source);
    public static partial AllergyDto MapAllergy(AllergyEntity source);
    public static partial CoverageDto MapCoverage(CoverageEntity source);

    [global::FreakyKit.Forge.ForgeMethod(AllowNestedForging = true)]
    public static partial PatientDto MapPatient(PatientEntity source);
}

// ─── Hand-written baseline ───────────────────────────────────────────────────

public static class HealthcareHandWritten
{
    public static PatientDto MapPatient(PatientEntity s)
    {
        var dto = new PatientDto
        {
            Id = s.Id,
            Mrn = s.Mrn,
            FamilyName = s.FamilyName,
            GivenName = s.GivenName,
            BirthDate = s.BirthDate,
            Gender = s.Gender,
            MaritalStatus = s.MaritalStatus,
            PreferredLanguage = s.PreferredLanguage,
            Deceased = s.Deceased,
            DeceasedDate = s.DeceasedDate,
            Coverage = new CoverageDto
            {
                PayerName = s.Coverage.PayerName,
                MemberId = s.Coverage.MemberId,
                PlanType = s.Coverage.PlanType,
                EffectiveFrom = s.Coverage.EffectiveFrom,
                EffectiveTo = s.Coverage.EffectiveTo,
            },
            Observations = new List<ObservationDto>(s.Observations.Count),
            Medications = new List<MedicationDto>(s.Medications.Count),
            Allergies = new List<AllergyDto>(s.Allergies.Count),
        };
        foreach (var o in s.Observations)
            dto.Observations.Add(new ObservationDto { Id = o.Id, Code = o.Code, CodeSystem = o.CodeSystem, Display = o.Display, EffectiveAt = o.EffectiveAt, ValueQuantity = o.ValueQuantity, ValueUnit = o.ValueUnit, Status = o.Status });
        foreach (var m in s.Medications)
            dto.Medications.Add(new MedicationDto { Id = m.Id, RxNormCode = m.RxNormCode, Display = m.Display, Dosage = m.Dosage, Frequency = m.Frequency, StartedOn = m.StartedOn, EndedOn = m.EndedOn, Status = m.Status });
        foreach (var a in s.Allergies)
            dto.Allergies.Add(new AllergyDto { Id = a.Id, Allergen = a.Allergen, Criticality = a.Criticality, Reaction = a.Reaction, RecordedAt = a.RecordedAt });
        return dto;
    }
}

// ─── Mapperly ────────────────────────────────────────────────────────────────

[Riok.Mapperly.Abstractions.Mapper]
public static partial class HealthcareMapperly
{
    public static partial ObservationDto MapObservation(ObservationEntity source);
    public static partial MedicationDto MapMedication(MedicationEntity source);
    public static partial AllergyDto MapAllergy(AllergyEntity source);
    public static partial CoverageDto MapCoverage(CoverageEntity source);
    public static partial PatientDto MapPatient(PatientEntity source);
}

// ─── AutoMapper profile ──────────────────────────────────────────────────────

public class HealthcareAutoMapperProfile : Profile
{
    public HealthcareAutoMapperProfile()
    {
        CreateMap<ObservationEntity, ObservationDto>();
        CreateMap<MedicationEntity, MedicationDto>();
        CreateMap<AllergyEntity, AllergyDto>();
        CreateMap<CoverageEntity, CoverageDto>();
        CreateMap<PatientEntity, PatientDto>();
    }
}

// ─── Mapster registration ────────────────────────────────────────────────────

public static class HealthcareMapsterConfig
{
    public static void Register()
    {
        Mapster.TypeAdapterConfig<ObservationEntity, ObservationDto>.NewConfig();
        Mapster.TypeAdapterConfig<MedicationEntity, MedicationDto>.NewConfig();
        Mapster.TypeAdapterConfig<AllergyEntity, AllergyDto>.NewConfig();
        Mapster.TypeAdapterConfig<CoverageEntity, CoverageDto>.NewConfig();
        Mapster.TypeAdapterConfig<PatientEntity, PatientDto>.NewConfig();
    }
}
