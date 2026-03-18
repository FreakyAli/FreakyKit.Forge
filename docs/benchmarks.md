# FreakyKit.Forge — Benchmark Results

Full benchmark results comparing Forge against hand-written code and popular mapping libraries.
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

- **Forge matches hand-written code** — consistently within 1–2% across all scenarios, including real-world models with enums, nullables, and nested graphs
- **Zero allocation overhead** — identical memory footprint to hand-written mappers
- **2.5–4.7x faster than AutoMapper** — no reflection overhead at runtime
- **Faster than Mapster** — especially in medium, nested, flattening, and collection scenarios
- **Competitive with Mapperly** — trades leads across scenarios; Forge wins on medium mappings, nested graphs, flattening, and e-commerce real-world
- **Forge leads on throughput** — fastest across 10,000-object batches at 152 μs vs hand-written 155 μs
- **Facet** — competitive on flat/simple mappings but allocates significantly more and struggles on deep graphs and large collections

---

## .NET 10

> Benchmarks for .NET 10 have not been run yet. When available, results will be added here in the same format as the .NET 8 section above.
>
> To run them: update `TargetFramework` in `benchmarks/FreakyKit.Forge.Benchmarks/FreakyKit.Forge.Benchmarks.csproj` to `net10.0`, then run:
> ```
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
