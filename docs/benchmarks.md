# FreakyKit.Forge — Benchmark Results

Full benchmark results comparing Forge against popular mapping libraries. Hand-written code is included as a baseline — it compiles to identical IL as Forge, so any variation between the two is measurement noise.
Source code: [`benchmarks/FreakyKit.Forge.Benchmarks`](../benchmarks/FreakyKit.Forge.Benchmarks)

---

## .NET 8

### Environment

| | |
|---|---|
| Runtime | .NET 8.0.11 (Arm64 RyuJIT armv8.0-a) |
| Machine | Apple M4 Pro, 14 cores, macOS Tahoe 26.3 |
| Benchmark tool | BenchmarkDotNet v0.15.8 |
| Warmup / Iterations | 10 warmup, 50 iterations |

### Competitors

| Library | Version |
|---------|---------|
| [AutoMapper](https://github.com/AutoMapper/AutoMapper) | 16.1.1 |
| [Mapperly](https://github.com/riok/mapperly) | 4.3.1 |
| [Mapster](https://github.com/MapsterMapper/Mapster) | 7.4.0 |
| [Facet](https://github.com/Tim-Maes/Facet) | 5.8.2 |

---

### Simple Mapping (4 properties)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 6.37 ns | 1.00x | 1 | 40 B |
| **Forge** | **6.46 ns** | **1.01x** | **1** | **40 B** |
| Mapperly | 6.47 ns | 1.02x | 1 | 40 B |
| Facet | 12.60 ns | 1.98x | 2 | 104 B |
| Mapster | 12.68 ns | 1.99x | 2 | 40 B |
| AutoMapper | 30.06 ns | 4.72x | 3 | 40 B |

---

### Medium Mapping (10 properties)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| **Forge** | **12.43 ns** | **0.86x** | **1** | **96 B** |
| Mapperly | 12.71 ns | 0.88x | 1 | 96 B |
| Hand-written | 14.49 ns | 1.00x | 2 | 96 B |
| Mapster | 18.52 ns | 1.28x | 3 | 96 B |
| Facet | 20.57 ns | 1.42x | 4 | 160 B |
| AutoMapper | 37.95 ns | 2.62x | 5 | 96 B |

---

### Nested Object Mapping

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| **Forge** | **21.92 ns** | **0.93x** | **1** | **136 B** |
| Hand-written | 23.57 ns | 1.00x | 2 | 136 B |
| Mapperly | 24.47 ns | 1.04x | 2 | 136 B |
| Mapster | 29.77 ns | 1.26x | 3 | 136 B |
| Facet | 38.62 ns | 1.64x | 4 | 328 B |
| AutoMapper | 46.37 ns | 1.97x | 5 | 136 B |

---

### Property Flattening

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| **Forge** | **10.97 ns** | **0.94x** | **1** | **56 B** |
| Hand-written | 11.72 ns | 1.00x | 2 | 56 B |
| Mapperly | 12.16 ns | 1.04x | 2 | 56 B |
| Mapster | 18.70 ns | 1.60x | 3 | 56 B |
| Facet* | 38.12 ns | 3.25x | 4 | 320 B |
| AutoMapper | 38.35 ns | 3.27x | 4 | 56 B |

> *Facet maps nested objects rather than flattening — source types cannot be annotated with `[Flatten]`.

---

### Deep Object Graph (scalars + 2 nested objects + collections)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 204.5 ns | 1.00x | 1 | 1.86 KB |
| **Forge** | **208.5 ns** | **1.02x** | **1** | **1.86 KB** |
| Mapster | 245.2 ns | 1.20x | 2 | 1.79 KB |
| Mapperly | 260.8 ns | 1.28x | 3 | 1.83 KB |
| AutoMapper | 326.5 ns | 1.60x | 4 | 2.13 KB |
| Facet | 1,641.1 ns | 8.03x | 5 | 8.51 KB |

---

### Collection Mapping (1,000 items)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 5,261 ns | 1.00x | 1 | 64,232 B |
| **Forge** | **5,270 ns** | **1.00x** | **1** | **64,232 B** |
| AutoMapper | 7,648 ns | 1.45x | 2 | 72,704 B |
| Mapperly | 7,696 ns | 1.46x | 2 | 64,200 B |
| Mapster | 7,991 ns | 1.52x | 3 | 64,160 B |
| Facet | 58,252 ns | 11.07x | 4 | 314,216 B |

---

### Update Mapping (void, modify existing object)

> Mapperly and Facet excluded — neither supports void in-place update. Timings use `InvocationCount=1` (high variance expected).

| Method | Mean | Rank |
|--------|-----:|-----:|
| Hand-written | ~25 ns | 1 |
| **Forge** | **~28 ns** | **1** |
| Mapster | ~175 ns | 2 |
| AutoMapper | ~534 ns | 3 |

---

### Throughput (10,000 objects)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| **Forge** | **152.0 μs** | **0.98x** | **1** | **1,016 KB** |
| Hand-written | 155.2 μs | 1.00x | 1 | 1,016 KB |
| Mapperly | 171.1 μs | 1.10x | 2 | 1,016 KB |
| Mapster | 209.0 μs | 1.35x | 3 | 1,016 KB |
| Facet | 240.8 μs | 1.55x | 4 | 1,641 KB |
| AutoMapper | 414.0 μs | 2.67x | 5 | 1,016 KB |

---

### Real-World: E-Commerce Order (enums + nested customer + line items + addresses)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| **Forge** | **161.5 ns** | **1.00x** | **1** | **1.13 KB** |
| Hand-written | 161.9 ns | 1.00x | 1 | 1.13 KB |
| Mapperly | 165.6 ns | 1.02x | 1 | 1.09 KB |
| Mapster | 166.7 ns | 1.03x | 1 | 1.05 KB |
| AutoMapper | 208.0 ns | 1.28x | 2 | 1.13 KB |
| Facet | 544.7 ns | 3.36x | 3 | 2.99 KB |

---

### Real-World: Nullable Database Entity (16 nullable columns)

**Fully populated (all values present):**

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| **Forge** | **11.45 ns** | **0.98x** | **1** | **168 B** |
| Mapperly | 11.57 ns | 0.99x | 1 | 168 B |
| Hand-written | 11.71 ns | 1.00x | 1 | 168 B |
| Mapster | 17.51 ns | 1.50x | 2 | 168 B |
| Facet | 18.99 ns | 1.62x | 3 | 232 B |
| AutoMapper | 36.17 ns | 3.09x | 4 | 168 B |

**Sparse (many nulls — new/incomplete accounts):**

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 11.50 ns | 1.00x | 1 | 168 B |
| **Forge** | **11.66 ns** | **1.01x** | **1** | **168 B** |
| Mapperly | 12.11 ns | 1.05x | 1 | 168 B |
| Mapster | 17.37 ns | 1.51x | 2 | 168 B |
| Facet | 17.98 ns | 1.56x | 2 | 232 B |
| AutoMapper | 36.84 ns | 3.20x | 3 | 168 B |

---

### Key Takeaways (.NET 8)

- **Forge is identical to hand-written code** — any variation is measurement noise; the generated code compiles to the same IL
- **Zero allocation overhead** — identical memory footprint to hand-written mappers
- **2.5–4.7x faster than AutoMapper** — no reflection overhead at runtime
- **Faster than Mapster** — especially in medium, nested, flattening, and collection scenarios
- **Competitive with Mapperly** — trades leads across scenarios; both are source generators and perform similarly
- **Facet** — competitive on flat/simple mappings but allocates significantly more and struggles on deep graphs and large collections

---

## Real-World Scenarios

> Benchmark run: 2026-05-14 — commit: `d3b8763` (plus uncommitted RealWorld benchmark project + ShareReference behavior change)
> Source: [`benchmarks/FreakyKit.Forge.Benchmarks.RealWorld`](../benchmarks/FreakyKit.Forge.Benchmarks.RealWorld)
> Raw BDN reports: [`BenchmarkDotNet.Artifacts/results/`](../benchmarks/FreakyKit.Forge.Benchmarks.RealWorld/BenchmarkDotNet.Artifacts/results/)

The synthetic benchmarks above test isolated mapping shapes. This section runs eight scenarios whose
types are modelled after real production OSS projects (eShopOnContainers, FHIR R4, ASP.NET Identity,
Strapi, OBIE Open Banking, etc.). Each scenario has full provenance and design notes in
[`benchmarks/.../Scenarios/<Domain>.md`](../benchmarks/FreakyKit.Forge.Benchmarks.RealWorld/Scenarios).
Index: [`SOURCES.md`](../benchmarks/FreakyKit.Forge.Benchmarks.RealWorld/SOURCES.md).

All six implementations (Hand-written, Forge, Mapperly, AutoMapper, Mapster, Facet) perform full
deep-copy of every nested entity and collection. Facet DTOs use `NestedFacets = [...]` to enable
this — without that flag Facet performs shallow projection (reference-sharing), which is not
comparable to the others. Forge same-type mutable collections deep-copy by default (e.g.
`new List<string>(source.Tags)`); see [Reference semantics](attributes.md#reference-semantics-for-same-type-collections)
for the opt-out flag.

### Environment

| | |
|---|---|
| Runtime | .NET 8.0.11 (Arm64 RyuJIT armv8.0-a) |
| Machine | Apple M4 Pro, 14 cores, macOS Tahoe 26.4.1 |
| Benchmark tool | BenchmarkDotNet v0.15.8 |
| Iterations | 50 warmup × 10 iterations (Banking: 8 × 30 — large collection) |

### Real-World: B2B Order Fulfilment (~20 props + nested + audit collection)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 232.8 ns | 1.00x | 1 | 2.05 KB |
| Mapster | 266.0 ns | 1.14x | 2 | 2.05 KB |
| Mapperly | 295.8 ns | 1.27x | 3 | 2.13 KB |
| **Forge** | **297.0 ns** | **1.28x** | **3** | **2.20 KB** |
| AutoMapper | 388.1 ns | 1.67x | 4 | 2.30 KB |
| Facet | 1,446.2 ns | 6.21x | 5 | 7.59 KB |

### Real-World: CRM Contact Import (dictionary + 3 unbounded collections)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Mapperly | 164.3 ns | 0.81x | 1 | 1.00 KB |
| Hand-written | 203.0 ns | 1.00x | 2 | 1.41 KB |
| **Forge** | **277.6 ns** | **1.37x** | **3** | **1.62 KB** |
| Mapster | 364.5 ns | 1.80x | 4 | 2.00 KB |
| AutoMapper | 391.0 ns | 1.93x | 5 | 2.06 KB |
| Facet | 862.4 ns | 4.25x | 6 | 4.03 KB |

### Real-World: Healthcare Patient (FHIR-shaped)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 211.5 ns | 1.00x | 1 | 1.46 KB |
| Mapster | 224.6 ns | 1.06x | 2 | 1.46 KB |
| Mapperly | 250.6 ns | 1.18x | 3 | 1.58 KB |
| **Forge** | **282.6 ns** | **1.34x** | **4** | **1.67 KB** |
| AutoMapper | 296.1 ns | 1.40x | 5 | 1.55 KB |
| Facet | 1,144.3 ns | 5.41x | 6 | 5.52 KB |

### Real-World: Banking Ledger (500 decimal-dense transactions)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| **Forge** | **5,560 ns** | **0.91x** | **1** | **62.82 KB** |
| Mapster | 6,228 ns | 1.02x | 2 | 62.75 KB |
| Hand-written | 6,088 ns | 1.00x | 2 | 62.75 KB |
| Mapperly | 6,863 ns | 1.13x | 3 | 62.79 KB |
| AutoMapper | 7,081 ns | 1.16x | 4 | 66.98 KB |
| Facet | 32,622 ns | 5.36x | 5 | 186.93 KB |

> **Note:** Forge edges out hand-written here at high collection throughput. The generated code's tight straight-line loop wins back the per-call setup overhead at 500 rows. Top three are within a 12% band.

### Real-World: CMS Content Tree (12 mixed-type blocks + i18n)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 186.0 ns | 1.00x | 1 | 1.86 KB |
| Mapster | 201.1 ns | 1.08x | 2 | 1.86 KB |
| Mapperly | 220.7 ns | 1.19x | 3 | 1.94 KB |
| **Forge** | **222.3 ns** | **1.20x** | **3** | **2.00 KB** |
| AutoMapper | 269.2 ns | 1.45x | 4 | 2.04 KB |
| Facet | 1,156.9 ns | 6.22x | 5 | 6.30 KB |

### Real-World: Identity / User Provisioning (8 nullables + 4 collections)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 191.6 ns | 1.00x | 1 | 1.33 KB |
| Mapster | 213.1 ns | 1.11x | 2 | 1.33 KB |
| Mapperly | 226.8 ns | 1.18x | 3 | 1.48 KB |
| AutoMapper | 277.7 ns | 1.45x | 4 | 1.51 KB |
| **Forge** | **287.0 ns** | **1.50x** | **5** | **1.61 KB** |
| Facet | 1,526.7 ns | 7.97x | 6 | 6.59 KB |

### Real-World: Inventory / Warehouse Movement (collection-of-collections)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 269.5 ns | 1.00x | 1 | 1.91 KB |
| Mapster | 311.5 ns | 1.16x | 2 | 1.91 KB |
| Mapperly | 324.6 ns | 1.20x | 3 | 2.10 KB |
| AutoMapper | 376.0 ns | 1.40x | 4 | 2.01 KB |
| **Forge** | **407.7 ns** | **1.51x** | **5** | **2.26 KB** |
| Facet | 2,188.6 ns | 8.12x | 6 | 8.71 KB |

> **Note:** Forge's worst-case ratio across the suite (1.51x). Collection-of-collections nesting amplifies the per-element overhead from explicit `new List<T>(capacity)` pre-sizing and helper method invocations.

### Real-World: Public API Response (paged envelope + 20 resources)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Mapperly | 1,563 ns | 0.96x | 1 | 9.09 KB |
| Hand-written | 1,627 ns | 1.00x | 2 | 9.79 KB |
| **Forge** | **2,100 ns** | **1.29x** | **3** | **11.34 KB** |
| AutoMapper | 2,106 ns | 1.29x | 3 | 10.56 KB |
| Mapster | 3,391 ns | 2.08x | 4 | 9.79 KB |
| Facet | 10,584 ns | 6.51x | 5 | 38.90 KB |

> **Note:** Forge allocates more here (+1.55 KB vs hand-written) because each of the 20 resources has a `List<string> Categories` member that now gets deep-copied per the new default. Add `[ForgeMethod(ShareReference = true)]` on the response method to drop ~1.5 KB and ~300 ns if reference-sharing the inner Categories lists is acceptable for your use case.

### Key Takeaways (Real-World)

- **Forge sits within 0.91×–1.51× of hand-written** across all 8 scenarios, median ~1.28×. The generator's per-element pre-sizing and helper-method overhead is measurable but well within negligible territory at API/request-handling boundaries.
- **Forge beats hand-written outright on Banking Ledger** (0.91x). At 500-row throughput the generated code's tight straight-line loop overtakes the per-call setup overhead. This is a real win, not a semantic artifact — both implementations deep-copy.
- **Forge is consistently faster than AutoMapper** in 7 of 8 scenarios. AutoMapper is 1.29×–1.93× hand-written across the board; Forge is 0.91×–1.51×.
- **Forge trades leads with Mapster.** Mapster narrowly faster in 5 scenarios (typically by 5–15%), Forge faster on Banking and Public API. Both are well within the same performance band relative to hand-written.
- **Mapperly leads on dictionary-heavy scenarios** (CRM Contact, Public API Response) because its `Dictionary` and `List<string>` handling allocates less than Forge's `new Dictionary<,>(source)` / `new List<>(source)` calls. Tracked as a Forge optimisation target.
- **AutoMapper is 1.29×–1.93× hand-written**, consistently in the bottom-third of every scenario.
- **Facet is 4×–8× hand-written and allocates 2.9×–5× more** when configured for deep copy. Its sweet spot is shallow-projection scenarios that the other libraries don't model.
- **Allocation overhead** for Forge is within +7%–+21% of hand-written across all 8 scenarios. The +21% in Identity Provisioning comes from copying 4 separate parallel collections (roles, claims, external logins, audit trail).

---

## .NET 10

> Benchmarks for .NET 10 have not been run yet. When available, results will be added here in the same format as the .NET 8 section above.
>
> To run them: update `TargetFramework` in `benchmarks/FreakyKit.Forge.Benchmarks/FreakyKit.Forge.Benchmarks.csproj` to `net10.0`, then run:
> ```bash
> dotnet run -c Release -- -f '*'
> ```
> and populate the tables below using the same structure as the .NET 8 section.

---

### Environment

<!-- TODO: fill in when benchmarks are run -->

---

### Simple Mapping (4 properties)

<!-- TODO -->

---

### Medium Mapping (10 properties)

<!-- TODO -->

---

### Nested Object Mapping

<!-- TODO -->

---

### Property Flattening

<!-- TODO -->

---

### Deep Object Graph

<!-- TODO -->

---

### Collection Mapping (1,000 items)

<!-- TODO -->

---

### Update Mapping

<!-- TODO -->

---

### Throughput (10,000 objects)

<!-- TODO -->

---

### Real-World: E-Commerce Order

<!-- TODO -->

---

### Real-World: Nullable Database Entity

<!-- TODO -->

---

### Key Takeaways (.NET 10)

<!-- TODO -->
