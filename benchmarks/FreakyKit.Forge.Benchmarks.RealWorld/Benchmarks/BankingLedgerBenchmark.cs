using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Facet.Extensions;
using ForgeBenchmarks.RealWorld.BankingLedger;
using Mapster;

namespace ForgeBenchmarks.RealWorld.Benchmarks;

/// <summary>
/// Banking transaction ledger scenario — account header plus a high-volume collection of
/// transactions (500 rows). Stresses per-element mapping throughput on decimal-heavy value
/// records. See Scenarios/BankingLedger.md.
/// </summary>
[MemoryDiagnoser(displayGenColumns: true)]
[SimpleJob(RuntimeMoniker.Net80, iterationCount: 30, warmupCount: 8)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
[RankColumn]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class BankingLedgerBenchmark
{
    private AccountEntity _account = null!;

    [GlobalSetup]
    public void Setup()
    {
        MapsterSetup.Configure();
        _ = AutoMapperSetup.Mapper;

        var rnd = new Random(42);
        var balance = 12_500.00m;
        var transactions = new List<TransactionEntity>(500);
        for (int i = 0; i < 500; i++)
        {
            var isCredit = rnd.Next(0, 3) == 0;
            var amount = Math.Round((decimal)(rnd.NextDouble() * 800 + 10), 2);
            if (isCredit) balance += amount; else balance -= amount;

            transactions.Add(new TransactionEntity
            {
                Id = Guid.NewGuid(),
                PostedAt = new DateTime(2024, 1, 1).AddHours(i * 3),
                EffectiveDate = new DateTime(2024, 1, 1).AddHours(i * 3).Date,
                Direction = isCredit ? TransactionDirection.Credit : TransactionDirection.Debit,
                Amount = amount,
                Description = isCredit ? "Payroll deposit" : $"POS purchase #{i}",
                Counterparty = isCredit ? "ACME PAYROLL" : "MERCHANT-INC",
                Reference = $"REF-{i:D8}",
                Category = isCredit ? TransactionCategory.Payroll : TransactionCategory.Pos,
                Status = TransactionStatus.Posted,
                RunningBalance = balance,
            });
        }

        _account = new AccountEntity
        {
            Id = Guid.Parse("3f8d2c1a-7b4e-4d8a-9f1c-2e6d5b8c4a3e"),
            AccountNumber = "1000-2384-7766",
            AccountName = "Personal Checking",
            Type = AccountType.Checking,
            Currency = "USD",
            CurrentBalance = balance,
            AvailableBalance = balance - 250.00m,
            PendingDebits = 250.00m,
            PendingCredits = 0m,
            OpenedOn = new DateTime(2018, 4, 12),
            ClosedOn = null,
            Status = AccountStatus.Open,
            OwnerName = "Sasha Patel",
            Transactions = transactions,
        };
    }

    [Benchmark(Baseline = true, Description = "Hand-written")]
    [BenchmarkCategory("BankingLedger")]
    public AccountDto HandWritten() => BankingHandWritten.MapAccount(_account);

    [Benchmark(Description = "Forge")]
    [BenchmarkCategory("BankingLedger")]
    public AccountDto ForgeGenerated() => BankingForges.MapAccount(_account);

    [Benchmark(Description = "Mapperly")]
    [BenchmarkCategory("BankingLedger")]
    public AccountDto Mapperly() => BankingMapperly.MapAccount(_account);

    [Benchmark(Description = "AutoMapper")]
    [BenchmarkCategory("BankingLedger")]
    public AccountDto AutoMapper() => AutoMapperSetup.Mapper.Map<AccountDto>(_account);

    [Benchmark(Description = "Mapster")]
    [BenchmarkCategory("BankingLedger")]
    public AccountDto Mapster() => _account.Adapt<AccountDto>();

    [Benchmark(Description = "Facet")]
    [BenchmarkCategory("BankingLedger")]
    public AccountFacetDto Facet() => _account.ToFacet<AccountEntity, AccountFacetDto>();
}
