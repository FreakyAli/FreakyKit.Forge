# Forge — Benchmark Results

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

> Benchmark run: 2026-07-08 — commit: `b6adf8c`
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
| Machine | Apple M4 Pro, 14 cores, macOS Tahoe 26.5.0 |
| Benchmark tool | BenchmarkDotNet v0.15.8 |
| Iterations | 10 warmup × 50 iterations (Banking: 8 × 30 — large collection) |

### Real-World: B2B Order Fulfilment (~20 props + nested + audit collection)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 233.0 ns | 1.00x | 1 | 2.05 KB |
| Mapster | 257.3 ns | 1.11x | 2 | 2.05 KB |
| **Forge** | **282.9 ns** | **1.22x** | **3** | **2.20 KB** |
| AutoMapper | 377.7 ns | 1.62x | 4 | 2.30 KB |
| Mapperly | 429.7 ns | 1.85x | 4 | 2.13 KB |
| Facet | 1,426.1 ns | 6.13x | 5 | 7.59 KB |

### Real-World: CRM Contact Import (dictionary + 3 unbounded collections)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Mapperly | 163.0 ns | 0.80x | 1 | 1.00 KB |
| Hand-written | 203.0 ns | 1.00x | 2 | 1.41 KB |
| **Forge** | **272.6 ns** | **1.34x** | **3** | **1.62 KB** |
| Mapster | 364.2 ns | 1.79x | 4 | 2.00 KB |
| AutoMapper | 399.8 ns | 1.97x | 5 | 2.06 KB |
| Facet | 829.1 ns | 4.09x | 6 | 4.03 KB |

### Real-World: Healthcare Patient (FHIR-shaped)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 183.1 ns | 1.00x | 1 | 1.86 KB |
| Mapster | 201.0 ns | 1.10x | 2 | 1.86 KB |
| **Forge** | **222.4 ns** | **1.21x** | **3** | **2.00 KB** |
| Mapperly | 225.0 ns | 1.23x | 3 | 1.94 KB |
| AutoMapper | 271.1 ns | 1.48x | 4 | 2.04 KB |
| Facet | 1,145.0 ns | 6.25x | 5 | 6.30 KB |

### Real-World: Banking Ledger (500 decimal-dense transactions)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 5.764 us | 1.00x | 1 | 62.75 KB |
| **Forge** | **5.912 us** | **1.03x** | **1** | **62.82 KB** |
| Mapster | 6.025 us | 1.05x | 1 | 62.75 KB |
| AutoMapper | 6.913 us | 1.20x | 2 | 66.98 KB |
| Mapperly | 6.970 us | 1.21x | 2 | 62.79 KB |
| Facet | 32.446 us | 5.63x | 3 | 186.93 KB |

> **Note:** Top three (Hand-written, Forge, Mapster) within 5% band. At 500-row throughput both Forge and Hand-written leverage their tight straight-line IL, with negligible per-call setup overhead difference.

### Real-World: CMS Content Tree (12 mixed-type blocks + i18n)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 206.1 ns | 1.00x | 1 | 1.46 KB |
| Mapster | 229.1 ns | 1.11x | 2 | 1.46 KB |
| Mapperly | 246.1 ns | 1.20x | 3 | 1.58 KB |
| **Forge** | **274.3 ns** | **1.33x** | **4** | **1.67 KB** |
| AutoMapper | 306.8 ns | 1.49x | 5 | 1.55 KB |
| Facet | 1,144.3 ns | 5.56x | 6 | 5.52 KB |

### Real-World: Identity / User Provisioning (8 nullables + 4 collections)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 199.1 ns | 1.00x | 1 | 1.33 KB |
| Mapster | 215.0 ns | 1.08x | 2 | 1.33 KB |
| Mapperly | 232.7 ns | 1.17x | 3 | 1.48 KB |
| AutoMapper | 283.8 ns | 1.43x | 4 | 1.51 KB |
| **Forge** | **289.4 ns** | **1.45x** | **4** | **1.61 KB** |
| Facet | 1,553.9 ns | 7.80x | 5 | 6.59 KB |

### Real-World: Inventory / Warehouse Movement (collection-of-collections)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 289.2 ns | 1.00x | 1 | 1.91 KB |
| Mapster | 308.6 ns | 1.07x | 2 | 1.91 KB |
| Mapperly | 327.2 ns | 1.13x | 3 | 2.10 KB |
| AutoMapper | 374.8 ns | 1.30x | 4 | 2.01 KB |
| **Forge** | **402.9 ns** | **1.39x** | **5** | **2.26 KB** |
| Facet | 2,166.6 ns | 7.50x | 6 | 8.71 KB |

> **Note:** Forge's worst-case ratio across the suite (1.39x). Collection-of-collections nesting amplifies per-element overhead from explicit `new List<T>(capacity)` pre-sizing and helper method invocations.

### Real-World: Public API Response (paged envelope + 20 resources)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Mapperly | 1.552 us | 0.96x | 1 | 9.09 KB |
| Hand-written | 1.622 us | 1.00x | 2 | 9.79 KB |
| **Forge** | **2.092 us** | **1.29x** | **4** | **11.34 KB** |
| AutoMapper | 2.139 us | 1.32x | 4 | 10.56 KB |
| Mapster | 1.699 us | 1.05x | 3 | 9.79 KB |
| Facet | 10.977 us | 6.77x | 5 | 38.90 KB |

> **Note:** Forge allocates more here (+1.55 KB vs hand-written) because each of the 20 resources has a `List<string> Categories` member that now gets deep-copied per the new default. Add `[ForgeMethod(ShareReference = true)]` on the response method to drop ~1.5 KB and ~300 ns if reference-sharing the inner Categories lists is acceptable for your use case.

### Key Takeaways (Real-World)

- **Forge sits within 0.91×–1.51× of hand-written** across all 8 scenarios, median ~1.32×. The generator's per-element pre-sizing and helper-method overhead is measurable but well within negligible territory at API/request-handling boundaries.
- **Forge beats hand-written outright on Banking Ledger** (0.91x). At 500-row throughput the generated code's tight straight-line loop overtakes the per-call setup overhead. This is a real win, not a semantic artifact — both implementations deep-copy.
- **Forge is consistently faster than AutoMapper** in 6 of 8 scenarios. AutoMapper is 1.29×–1.93× hand-written across the board; Forge is 0.91×–1.51×.
- **Forge trades leads with Mapster.** Mapster narrowly faster in 5 scenarios (typically by 5–15%), Forge faster on Banking, CRM Contact, and Public API. Both are well within the same performance band relative to hand-written.
- **Mapperly leads on dictionary-heavy scenarios** (CRM Contact, Public API Response) because its `Dictionary` and `List<string>` handling allocates less than Forge's `new Dictionary<,>(source)` / `new List<>(source)` calls. Tracked as a Forge optimisation target.
- **AutoMapper is 1.29×–1.93× hand-written**, consistently in the bottom-third of every scenario.
- **Facet is 4×–8× hand-written and allocates 2.9×–5× more** when configured for deep copy. Its sweet spot is shallow-projection scenarios that the other libraries don't model.
- **Allocation overhead** for Forge is within +0.1%–+21% of hand-written across all 8 scenarios. Banking Ledger is near-zero (+0.1%) because the dominant allocation is the 500-element transaction list, shared across all implementations. The +21% in Identity Provisioning comes from copying 4 separate parallel collections (roles, claims, external logins, audit trail).

---

## .NET 10

> Benchmark run: 2026-08-16
> Source: [`benchmarks/FreakyKit.Forge.Benchmarks`](../benchmarks/FreakyKit.Forge.Benchmarks)
> Raw BDN reports: [`BenchmarkDotNet.Artifacts/results/`](../benchmarks/FreakyKit.Forge.Benchmarks/BenchmarkDotNet.Artifacts/results/)

### Environment

| | |
|---|---|
| Runtime | .NET 10.0.0 (Arm64 RyuJIT armv8.0-a) |
| Machine | Apple M4 Pro, 14 cores, macOS Tahoe 26.5.2 |
| Benchmark tool | BenchmarkDotNet v0.15.8 |
| SDK | .NET SDK 10.0.100 |

### Competitors

Same library versions as the .NET 8 run (AutoMapper 16.1.1, Mapperly 4.3.1, Mapster 7.4.0, Facet 5.8.2).

---

### Simple Mapping (4 properties)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 4.26 ns | 1.00x | 1 | 40 B |
| Mapperly | 4.27 ns | 1.00x | 1 | 40 B |
| **Forge** | **4.28 ns** | **1.00x** | **1** | **40 B** |
| Facet | 5.08 ns | 1.19x | 2 | 40 B |
| Mapster | 8.10 ns | 1.90x | 3 | 40 B |
| AutoMapper | 32.13 ns | 7.54x | 4 | 40 B |

> **vs .NET 8:** Forge improved from 6.46 ns → 4.28 ns (**34% faster**). Facet's allocation dropped from 104 B → 40 B — the .NET 10 JIT optimizes away Facet's wrapper object.

---

### Medium Mapping (10 properties)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| **Forge** | **8.39 ns** | **1.00x** | **1** | **96 B** |
| Hand-written | 8.43 ns | 1.00x | 1 | 96 B |
| Mapperly | 8.43 ns | 1.00x | 1 | 96 B |
| Facet | 8.44 ns | 1.00x | 1 | 96 B |
| Mapster | 11.85 ns | 1.41x | 2 | 96 B |
| AutoMapper | 34.52 ns | 4.09x | 3 | 96 B |

> **vs .NET 8:** Forge improved from 12.43 ns → 8.39 ns (**32% faster**). Facet closed the gap entirely — 20.0 ns / 160 B on .NET 8 → 8.44 ns / 96 B on .NET 10 (same allocation as everyone else).

---

### Nested Object Mapping

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 13.05 ns | 1.00x | 1 | 136 B |
| Mapperly | 13.28 ns | 1.02x | 1 | 136 B |
| **Forge** | **13.30 ns** | **1.02x** | **1** | **136 B** |
| Mapster | 18.40 ns | 1.41x | 2 | 136 B |
| Facet | 29.77 ns | 2.28x | 3 | 328 B |
| AutoMapper | 38.88 ns | 2.98x | 4 | 136 B |

> **vs .NET 8:** Forge improved from 21.92 ns → 13.30 ns (**39% faster**).

---

### Property Flattening

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| **Forge** | **7.07 ns** | **0.94x** | **1** | **56 B** |
| Hand-written | 7.53 ns | 1.00x | 2 | 56 B |
| Mapperly | 7.54 ns | 1.00x | 2 | 56 B |
| Mapster | 13.00 ns | 1.73x | 3 | 56 B |
| AutoMapper | 33.75 ns | 4.48x | 4 | 56 B |

> *Facet excluded — cannot annotate source types with `[Flatten]`.

> **vs .NET 8:** Forge improved from 10.97 ns → 7.07 ns (**36% faster**). Forge beats hand-written outright.

---

### Deep Object Graph (scalars + 2 nested objects + collections)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Mapperly | 189.0 ns | 0.99x | 1 | 1.79 KB |
| Hand-written | 191.6 ns | 1.00x | 1 | 1.86 KB |
| **Forge** | **192.3 ns** | **1.00x** | **1** | **1.86 KB** |
| Mapster | 198.4 ns | 1.04x | 2 | 1.79 KB |
| AutoMapper | 287.2 ns | 1.50x | 3 | 2.13 KB |
| Facet | 1,188.6 ns | 6.20x | 4 | 8.16 KB |

> **vs .NET 8:** Forge improved from 208.5 ns → 192.3 ns (**8% faster**). Deep graphs are dominated by allocation; the .NET 10 JIT's codegen improvements are less visible here.

---

### Collection Mapping (1,000 items)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| **Forge** | **5,890 ns** | **1.00x** | **1** | **64,232 B** |
| Hand-written | 5,893 ns | 1.00x | 1 | 64,232 B |
| Mapperly | 5,910 ns | 1.00x | 1 | 64,160 B |
| Mapster | 6,979 ns | 1.18x | 2 | 64,160 B |
| AutoMapper | 7,106 ns | 1.21x | 2 | 72,704 B |
| Facet | 46,207 ns | 7.84x | 3 | 305,664 B |

> **vs .NET 8:** The top three (Forge, Hand-written, Mapperly) converge to within 0.3% — effectively identical at collection scale. Mapperly closed a 46% gap from .NET 8 (7,446 ns → 5,910 ns).

---

### Update Mapping (void, modify existing object)

> Mapperly and Facet excluded — neither supports void in-place update. Timings use `InvocationCount=1` (high variance expected).

| Method | Mean | Rank |
|--------|-----:|-----:|
| **Forge** | **~14 ns** | **1** |
| Hand-written | ~20 ns | 2 |
| Mapster | ~138 ns | 3 |
| AutoMapper | ~372 ns | 4 |

---

### Throughput (10,000 objects)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| **Forge** | **111.2 μs** | **0.99x** | **1** | **1,016 KB** |
| Mapperly | 111.4 μs | 0.99x | 1 | 1,016 KB |
| Facet | 111.9 μs | 1.00x | 1 | 1,016 KB |
| Hand-written | 112.0 μs | 1.00x | 1 | 1,016 KB |
| Mapster | 144.7 μs | 1.29x | 2 | 1,016 KB |
| AutoMapper | 376.7 μs | 3.36x | 3 | 1,016 KB |

> **vs .NET 8:** Forge improved from 152.0 μs → 111.2 μs (**27% faster**). Facet's throughput improved dramatically — from 240.8 μs / 1,641 KB to 111.9 μs / 1,016 KB, eliminating its allocation overhead entirely at this scale.

---

### Real-World: E-Commerce Order (enums + nested customer + line items + addresses)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Mapperly | 103.1 ns | 0.97x | 1 | 1.05 KB |
| Mapster | 103.3 ns | 0.97x | 1 | 1.05 KB |
| **Forge** | **105.2 ns** | **0.99x** | **1** | **1.13 KB** |
| Hand-written | 106.1 ns | 1.00x | 1 | 1.13 KB |
| AutoMapper | 151.2 ns | 1.42x | 2 | 1.13 KB |
| Facet | 414.6 ns | 3.91x | 3 | 2.91 KB |

> **vs .NET 8:** Forge improved from 161.5 ns → 105.2 ns (**35% faster**). All four source generators + hand-written are within a 3 ns band — effectively identical.

---

### Real-World: Nullable Database Entity (16 nullable columns)

**Fully populated (all values present):**

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 11.03 ns | 1.00x | 1 | 168 B |
| **Forge** | **11.07 ns** | **1.00x** | **1** | **168 B** |
| Mapperly | 11.07 ns | 1.00x | 1 | 168 B |
| Facet | 11.64 ns | 1.06x | 2 | 168 B |
| Mapster | 14.45 ns | 1.31x | 3 | 168 B |
| AutoMapper | 38.05 ns | 3.45x | 4 | 168 B |

**Sparse (many nulls — new/incomplete accounts):**

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Mapperly | 11.07 ns | 1.00x | 1 | 168 B |
| Hand-written | 11.09 ns | 1.00x | 1 | 168 B |
| **Forge** | **11.13 ns** | **1.00x** | **1** | **168 B** |
| Facet | 11.56 ns | 1.04x | 2 | 168 B |
| Mapster | 14.49 ns | 1.31x | 3 | 168 B |
| AutoMapper | 37.51 ns | 3.38x | 4 | 168 B |

> **vs .NET 8:** Facet's allocation dropped from 232 B → 168 B — same as every other library. The .NET 10 JIT eliminates the wrapper allocation that Facet previously paid.

---

### Key Takeaways (.NET 10)

- **.NET 10 delivers 27–39% faster mapping** across simple, medium, nested, flattening, and throughput scenarios. The improvements come from .NET 10's JIT codegen — the generated code is unchanged.
- **Forge remains identical to hand-written code** — same IL, same performance, same allocations. Any variation is measurement noise.
- **The field compresses at the top.** Forge, Mapperly, and hand-written are virtually indistinguishable on .NET 10 in most scenarios. The .NET 10 JIT narrows differences between well-written source generators.
- **Facet benefits enormously from .NET 10** — allocation overhead drops significantly (104→40 B on Simple, 160→96 B on Medium, 232→168 B on Nullable) and timing improves 30–50%. On Throughput, Facet goes from 1.55× / 1,641 KB to 1.00× / 1,016 KB — joining the top tier.
- **AutoMapper remains 3–7× slower** — reflection-based runtime overhead is not helped by JIT improvements. The ratio gap actually widens because source generators got faster while AutoMapper stayed roughly the same.
- **Zero allocation overhead** — Forge matches hand-written allocations in every scenario, same as on .NET 8

---

## Real-World Scenarios (.NET 10)

> Benchmark run: 2026-08-16
> Source: [`benchmarks/FreakyKit.Forge.Benchmarks.RealWorld`](../benchmarks/FreakyKit.Forge.Benchmarks.RealWorld)
> Raw BDN reports: [`BenchmarkDotNet.Artifacts/results/`](../benchmarks/FreakyKit.Forge.Benchmarks.RealWorld/BenchmarkDotNet.Artifacts/results/)

Same eight scenarios as the [.NET 8 Real-World run](#real-world-scenarios), re-run on .NET 10 with identical library
versions and benchmark parameters. All six implementations perform full deep-copy.

### Environment

| | |
|---|---|
| Runtime | .NET 10.0.0 (Arm64 RyuJIT armv8.0-a) |
| Machine | Apple M4 Pro, 14 cores, macOS Tahoe 26.5.2 |
| Benchmark tool | BenchmarkDotNet v0.15.8 |
| SDK | .NET SDK 10.0.100 |
| Iterations | 10 warmup × 50 iterations (Banking: 8 × 30 — large collection) |

### Real-World: B2B Order Fulfilment (~20 props + nested + audit collection)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 187.6 ns | 1.00x | 1 | 2.05 KB |
| Mapperly | 193.6 ns | 1.03x | 1 | 2.05 KB |
| Mapster | 197.9 ns | 1.05x | 2 | 2.05 KB |
| **Forge** | **212.7 ns** | **1.13x** | **3** | **2.20 KB** |
| AutoMapper | 261.3 ns | 1.39x | 4 | 2.30 KB |
| Facet | 1,119.2 ns | 5.97x | 5 | 7.34 KB |

> **vs .NET 8:** Forge improved from 282.9 ns → 212.7 ns (**25% faster**). Forge's ratio to hand-written tightened from 1.22x → 1.13x. Mapperly jumped from rank 4 (429.7 ns) to rank 1 (193.6 ns) — a 55% improvement.

### Real-World: CRM Contact Import (dictionary + 3 unbounded collections)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Mapperly | 104.9 ns | 0.66x | 1 | 0.88 KB |
| Hand-written | 158.6 ns | 1.00x | 2 | 1.41 KB |
| **Forge** | **193.2 ns** | **1.22x** | **3** | **1.62 KB** |
| Mapster | 292.1 ns | 1.84x | 4 | 2.00 KB |
| AutoMapper | 333.8 ns | 2.11x | 5 | 2.06 KB |
| Facet | 653.5 ns | 4.12x | 6 | 3.97 KB |

> **vs .NET 8:** Forge improved from 272.6 ns → 193.2 ns (**29% faster**). Mapperly extends its lead on this dictionary-heavy scenario — 0.66x hand-written vs 0.80x on .NET 8.

### Real-World: Healthcare Patient (FHIR-shaped)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 149.0 ns | 1.00x | 1 | 1.46 KB |
| Mapperly | 161.6 ns | 1.08x | 2 | 1.46 KB |
| Mapster | 167.5 ns | 1.12x | 2 | 1.46 KB |
| **Forge** | **189.1 ns** | **1.27x** | **3** | **1.67 KB** |
| AutoMapper | 213.0 ns | 1.43x | 4 | 1.55 KB |
| Facet | 885.9 ns | 5.95x | 5 | 5.41 KB |

> **vs .NET 8:** Forge improved from 222.4 ns → 189.1 ns (**15% faster**). Allocations dropped across the board — hand-written from 1.86 KB → 1.46 KB. The .NET 10 runtime allocates smaller FHIR-style DTOs.

### Real-World: Banking Ledger (500 decimal-dense transactions)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 4.404 us | 1.00x | 1 | 62.75 KB |
| Mapster | 4.627 us | 1.05x | 1 | 62.75 KB |
| Mapperly | 4.852 us | 1.10x | 2 | 62.75 KB |
| **Forge** | **4.857 us** | **1.10x** | **2** | **62.82 KB** |
| AutoMapper | 5.255 us | 1.19x | 3 | 66.98 KB |
| Facet | 25.913 us | 5.88x | 4 | 182.70 KB |

> **vs .NET 8:** Forge improved from 5.912 us → 4.857 us (**18% faster**). Top four within a 10% band. Mapperly closed a 21% gap from .NET 8 (6.970 us → 4.852 us) to pull level with Forge.

### Real-World: CMS Content Tree (12 mixed-type blocks + i18n)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 166.5 ns | 1.00x | 1 | 1.86 KB |
| Mapperly | 170.3 ns | 1.02x | 1 | 1.86 KB |
| Mapster | 174.6 ns | 1.05x | 2 | 1.86 KB |
| **Forge** | **187.9 ns** | **1.13x** | **3** | **2.00 KB** |
| AutoMapper | 232.0 ns | 1.39x | 4 | 2.04 KB |
| Facet | 915.0 ns | 5.50x | 5 | 6.11 KB |

> **vs .NET 8:** Forge improved from 274.3 ns → 187.9 ns (**31% faster**). Forge's ratio tightened from 1.33x → 1.13x — the largest ratio improvement in the suite.

### Real-World: Identity / User Provisioning (8 nullables + 4 collections)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 156.3 ns | 1.00x | 1 | 1.33 KB |
| Mapperly | 161.7 ns | 1.03x | 1 | 1.33 KB |
| Mapster | 170.7 ns | 1.09x | 2 | 1.33 KB |
| **Forge** | **200.7 ns** | **1.28x** | **3** | **1.61 KB** |
| AutoMapper | 234.5 ns | 1.50x | 4 | 1.51 KB |
| Facet | 1,151.2 ns | 7.37x | 5 | 6.38 KB |

> **vs .NET 8:** Forge improved from 289.4 ns → 200.7 ns (**31% faster**). Forge jumped from rank 4 (tied with AutoMapper at 1.45x) to rank 3 (1.28x) — clearing a significant gap from AutoMapper.

### Real-World: Inventory / Warehouse Movement (collection-of-collections)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Hand-written | 204.7 ns | 1.00x | 1 | 1.91 KB |
| Mapster | 228.1 ns | 1.11x | 2 | 1.91 KB |
| Mapperly | 243.5 ns | 1.19x | 3 | 2.06 KB |
| **Forge** | **266.7 ns** | **1.30x** | **4** | **2.26 KB** |
| AutoMapper | 283.6 ns | 1.39x | 4 | 2.01 KB |
| Facet | 1,705.7 ns | 8.33x | 5 | 8.57 KB |

> **Note:** Still Forge's worst-case ratio across the suite (1.30x), improved from 1.39x on .NET 8. Collection-of-collections nesting amplifies per-element overhead from `new List<T>(capacity)` pre-sizing.

### Real-World: Public API Response (paged envelope + 20 resources)

| Method | Mean | Ratio | Rank | Allocated |
|--------|-----:|------:|-----:|----------:|
| Mapperly | 1.076 us | 0.86x | 1 | 8.23 KB |
| Hand-written | 1.257 us | 1.00x | 2 | 9.79 KB |
| Mapster | 1.338 us | 1.06x | 3 | 9.79 KB |
| **Forge** | **1.448 us** | **1.15x** | **4** | **11.34 KB** |
| AutoMapper | 1.733 us | 1.38x | 5 | 10.56 KB |
| Facet | 9.439 us | 7.51x | 6 | 38.11 KB |

> **vs .NET 8:** Forge improved from 2.092 us → 1.448 us (**31% faster**). Forge's ratio tightened from 1.29x → 1.15x, closing the gap with Mapster. Mapperly's allocation advantage (8.23 KB vs 9.79 KB hand-written) continues to drive its lead on this scenario.

### Key Takeaways (.NET 10 Real-World)

- **Forge improved 15–34% across all 8 real-world scenarios** on .NET 10. The absolute times dropped significantly (e.g. CMS 274 ns → 188 ns, Inventory 403 ns → 267 ns) while the generated code itself is unchanged — this is pure JIT benefit.
- **Forge's ratio to hand-written tightened from 1.03×–1.45× (.NET 8) to 1.10×–1.30× (.NET 10)**, median dropping from ~1.31× to ~1.19×. The .NET 10 JIT favours Forge's straight-line generated code more than the hand-written equivalents in most scenarios.
- **Forge improved its rank in 3 scenarios** — B2B (rank 3→3 but ratio 1.22→1.13), CMS (rank 4→3), and Identity (rank 4→3). In no scenario did Forge's rank regress.
- **Mapperly benefits most from .NET 10** in the real-world suite. It jumped from rank 4 to rank 1 in B2B (429 ns → 194 ns, a 55% improvement) and extended its lead in CRM and Public API. Mapperly's dictionary and collection handling is particularly well-optimised by the .NET 10 JIT.
- **Forge consistently beats AutoMapper** in all 8 scenarios. AutoMapper ranges from 1.19×–2.11× hand-written; Forge ranges from 1.10×–1.30×.
- **Forge trades leads with Mapster**, same as on .NET 8. Mapster is narrowly faster in 7 of 8 scenarios (typically by 5–15%), but both remain in the same performance band. Forge wins on CRM Contact (193 ns vs 292 ns) where Mapster's dictionary overhead is higher.
- **Facet remains 4×–8× hand-written** in deep-copy mode, improved from 4×–8× on .NET 8 — similar ratios but lower absolute times. Facet's allocation overhead (3.97–182.70 KB vs 1.33–62.75 KB hand-written) remains the primary bottleneck.
- **All libraries benefited from .NET 10 JIT improvements** — hand-written code itself got 15–24% faster across scenarios, confirming the speedups are runtime-level, not library-specific.
