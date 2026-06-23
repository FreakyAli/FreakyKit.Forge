using AutoMapper;

namespace ForgeBenchmarks.RealWorld.BankingLedger;

// ─── Forge ───────────────────────────────────────────────────────────────────

[global::FreakyKit.Forge.Forge]
public static partial class BankingForges
{
    public static partial TransactionDto MapTransaction(TransactionEntity source);

    [global::FreakyKit.Forge.ForgeMethod(AllowNestedForging = true)]
    public static partial AccountDto MapAccount(AccountEntity source);
}

// ─── Hand-written baseline ───────────────────────────────────────────────────

public static class BankingHandWritten
{
    public static AccountDto MapAccount(AccountEntity s)
    {
        var dto = new AccountDto
        {
            Id = s.Id,
            AccountNumber = s.AccountNumber,
            AccountName = s.AccountName,
            Type = s.Type,
            Currency = s.Currency,
            CurrentBalance = s.CurrentBalance,
            AvailableBalance = s.AvailableBalance,
            PendingDebits = s.PendingDebits,
            PendingCredits = s.PendingCredits,
            OpenedOn = s.OpenedOn,
            ClosedOn = s.ClosedOn,
            Status = s.Status,
            OwnerName = s.OwnerName,
            Transactions = new List<TransactionDto>(s.Transactions.Count),
        };
        foreach (var t in s.Transactions)
            dto.Transactions.Add(new TransactionDto
            {
                Id = t.Id,
                PostedAt = t.PostedAt,
                EffectiveDate = t.EffectiveDate,
                Direction = t.Direction,
                Amount = t.Amount,
                Description = t.Description,
                Counterparty = t.Counterparty,
                Reference = t.Reference,
                Category = t.Category,
                Status = t.Status,
                RunningBalance = t.RunningBalance,
            });
        return dto;
    }
}

// ─── Mapperly ────────────────────────────────────────────────────────────────

[Riok.Mapperly.Abstractions.Mapper]
public static partial class BankingMapperly
{
    public static partial TransactionDto MapTransaction(TransactionEntity source);
    public static partial AccountDto MapAccount(AccountEntity source);
}

// ─── AutoMapper profile ──────────────────────────────────────────────────────

public class BankingAutoMapperProfile : Profile
{
    public BankingAutoMapperProfile()
    {
        CreateMap<TransactionEntity, TransactionDto>();
        CreateMap<AccountEntity, AccountDto>();
    }
}

// ─── Mapster registration ────────────────────────────────────────────────────

public static class BankingMapsterConfig
{
    public static void Register()
    {
        Mapster.TypeAdapterConfig<TransactionEntity, TransactionDto>.NewConfig();
        Mapster.TypeAdapterConfig<AccountEntity, AccountDto>.NewConfig();
    }
}
