using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Facet.Extensions;
using ForgeBenchmarks.RealWorld.HealthcarePatient;
using Mapster;

namespace ForgeBenchmarks.RealWorld.Benchmarks;

/// <summary>
/// Healthcare patient summary scenario — FHIR-shaped patient record with observations,
/// medications, allergies, and coverage. Heavy nullable usage, coded values via enums, and a
/// nullable nested object (Coverage). See Scenarios/HealthcarePatient.md.
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 50, warmupCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class HealthcarePatientBenchmark
{
    private PatientEntity _patient = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterSetup.Configure();
        _ = AutoMapperSetup.Mapper;

        _patient = new PatientEntity
        {
            Id = "patient-7b3a92e1",
            Mrn = "MRN-2847391",
            FamilyName = "Iyer",
            GivenName = "Priya",
            BirthDate = new DateTime(1978, 6, 14),
            Gender = AdministrativeGender.Female,
            MaritalStatus = "Married",
            PreferredLanguage = "en-US",
            Deceased = false,
            DeceasedDate = null,
            Coverage = new CoverageEntity
            {
                PayerName = "BlueShield Federal",
                MemberId = "BS-77281903",
                PlanType = "PPO",
                EffectiveFrom = new DateTime(2024, 1, 1),
                EffectiveTo = null,
            },
            Observations = new List<ObservationEntity>
            {
                new() { Id = "obs-1", Code = "8302-2", CodeSystem = "http://loinc.org", Display = "Body Height", EffectiveAt = new DateTime(2024, 9, 1, 9, 0, 0), ValueQuantity = 165.1m, ValueUnit = "cm", Status = ObservationStatus.Final },
                new() { Id = "obs-2", Code = "29463-7", CodeSystem = "http://loinc.org", Display = "Body Weight", EffectiveAt = new DateTime(2024, 9, 1, 9, 0, 0), ValueQuantity = 63.5m, ValueUnit = "kg", Status = ObservationStatus.Final },
                new() { Id = "obs-3", Code = "8480-6", CodeSystem = "http://loinc.org", Display = "Systolic BP", EffectiveAt = new DateTime(2024, 9, 1, 9, 5, 0), ValueQuantity = 118m, ValueUnit = "mmHg", Status = ObservationStatus.Final },
                new() { Id = "obs-4", Code = "8462-4", CodeSystem = "http://loinc.org", Display = "Diastolic BP", EffectiveAt = new DateTime(2024, 9, 1, 9, 5, 0), ValueQuantity = 76m, ValueUnit = "mmHg", Status = ObservationStatus.Final },
                new() { Id = "obs-5", Code = "2339-0", CodeSystem = "http://loinc.org", Display = "Glucose", EffectiveAt = new DateTime(2024, 9, 1, 7, 30, 0), ValueQuantity = 92m, ValueUnit = "mg/dL", Status = ObservationStatus.Final },
                new() { Id = "obs-6", Code = "718-7", CodeSystem = "http://loinc.org", Display = "Hemoglobin", EffectiveAt = new DateTime(2024, 9, 1, 7, 30, 0), ValueQuantity = 13.8m, ValueUnit = "g/dL", Status = ObservationStatus.Final },
            },
            Medications = new List<MedicationEntity>
            {
                new() { Id = "med-1", RxNormCode = "314076", Display = "Lisinopril 10 MG Oral Tablet", Dosage = "10 mg", Frequency = "Once daily", StartedOn = new DateTime(2022, 4, 10), EndedOn = null, Status = MedicationStatus.Active },
                new() { Id = "med-2", RxNormCode = "856852", Display = "Atorvastatin 20 MG Oral Tablet", Dosage = "20 mg", Frequency = "Once daily at bedtime", StartedOn = new DateTime(2023, 1, 5), EndedOn = null, Status = MedicationStatus.Active },
                new() { Id = "med-3", RxNormCode = "1182772", Display = "Metformin HCl 500 MG Extended Release", Dosage = "500 mg", Frequency = "Twice daily with meals", StartedOn = new DateTime(2024, 3, 20), EndedOn = null, Status = MedicationStatus.Active },
            },
            Allergies = new List<AllergyEntity>
            {
                new() { Id = "all-1", Allergen = "Penicillin G", Criticality = AllergyCriticality.High, Reaction = "Anaphylaxis", RecordedAt = new DateTime(2010, 8, 12) },
                new() { Id = "all-2", Allergen = "Latex", Criticality = AllergyCriticality.Low, Reaction = "Contact dermatitis", RecordedAt = new DateTime(2015, 3, 4) },
            },
        };
    }

    [Benchmark(Baseline = true, Description = "Hand-written")]
    [BenchmarkCategory("HealthcarePatient")]
    public PatientDto HandWritten() => HealthcareHandWritten.MapPatient(_patient);

    [Benchmark(Description = "Forge")]
    [BenchmarkCategory("HealthcarePatient")]
    public PatientDto ForgeGenerated() => HealthcareForges.MapPatient(_patient);

    [Benchmark(Description = "Mapperly")]
    [BenchmarkCategory("HealthcarePatient")]
    public PatientDto Mapperly() => HealthcareMapperly.MapPatient(_patient);

    [Benchmark(Description = "AutoMapper")]
    [BenchmarkCategory("HealthcarePatient")]
    public PatientDto AutoMapper() => AutoMapperSetup.Mapper.Map<PatientDto>(_patient);

    [Benchmark(Description = "Mapster")]
    [BenchmarkCategory("HealthcarePatient")]
    public PatientDto Mapster() => _patient.Adapt<PatientDto>();

    [Benchmark(Description = "Facet")]
    [BenchmarkCategory("HealthcarePatient")]
    public PatientFacetDto Facet() => _patient.ToFacet<PatientEntity, PatientFacetDto>();
}
