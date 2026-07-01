# Scenario: Healthcare Patient Summary (FHIR-shaped)

**Domain:** Clinical / healthcare integration — patient summary record assembled from FHIR R4
resources for transfer between EHR systems or to a patient-facing app.

## What this represents

A simplified patient summary record matching the FHIR R4 resource shape (without the full
extension/profile metadata). Maps an internal patient entity to an outbound DTO with:

- 10 outer-level scalar properties (identifiers, demographics, deceased flag)
- An optional nested Coverage object (nullable — uninsured patients exist)
- Three collections of clinical resources: Observations, Medications, Allergies
- Heavy use of coded values (LOINC codes for observations, RxNorm for medications)
- Mixed nullable enum-like statuses and nullable timestamps

## Inspiration (hybrid attribution)

Shape is based on FHIR R4 resource definitions, simplified for benchmarking:

- **[Firely .NET SDK](https://github.com/FirelyTeam/firely-net-sdk)** (BSD-3-Clause) — the
  reference .NET implementation of FHIR. Its `Patient`, `Observation`, `MedicationStatement`,
  and `AllergyIntolerance` resources informed the property layouts, simplified to remove the
  full extension mechanism.
- **[FHIR R4 specification](https://hl7.org/fhir/R4/)** (Public — HL7 spec, CC0) — the
  authoritative resource shapes. We use only the core fields a patient-facing summary needs.
- **[OpenEHR](https://github.com/openEHR/specifications-ITS-REST)** patterns (CC-BY) — separately,
  the use of coded values with `system` + `code` + `display` triplets is universal across
  clinical interoperability standards.

This is intentionally NOT a complete FHIR mapping. A real FHIR-to-DTO pipeline has dozens more
fields per resource and handles polymorphic `value[x]` elements. The simplified shape captures
the mapping complexity without the FHIR-specific extension boilerplate.

## Why this is interesting to benchmark

- **Coded values + nullable measurements** stress nullable handling at scale (each Observation
  has `ValueQuantity decimal?` and `ValueUnit string?`).
- **Multiple parallel collections of related-but-distinct types** stress per-collection mapping
  overhead.
- **Optional nested Coverage** tests null-guarded nested forge handling.
- **Heavy enum usage** (status fields throughout) stresses enum-mapping codegen.

## Fixture rationale

The seeded patient represents a typical adult with stable chronic conditions: 6 observations
covering vitals + basic metabolic panel (LOINC codes are real), 3 active medications
(Lisinopril, Atorvastatin, Metformin — common combo for hypertension + dyslipidemia + early
T2DM), 2 documented allergies (one critical penicillin, one mild latex), PPO coverage that is
currently effective (no end date). Property values mirror what a SMART-on-FHIR patient summary
endpoint would emit.

## Modifications

None — POCO modelling only. Real FHIR `[Extension]` and `[Profile]` attributes are intentionally
omitted to keep the benchmark focused on the shape rather than FHIR's metadata machinery.
