namespace ForgeBenchmarks.RealWorld.BankingLedger;

// ─── Source entities ─────────────────────────────────────────────────────────

public class AccountEntity
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public AccountType Type { get; set; }
    public string Currency { get; set; } = "";
    public decimal CurrentBalance { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal PendingDebits { get; set; }
    public decimal PendingCredits { get; set; }
    public DateTime OpenedOn { get; set; }
    public DateTime? ClosedOn { get; set; }
    public AccountStatus Status { get; set; }
    public string OwnerName { get; set; } = "";
    public List<TransactionEntity> Transactions { get; set; } = new();
}

public class TransactionEntity
{
    public Guid Id { get; set; }
    public DateTime PostedAt { get; set; }
    public DateTime EffectiveDate { get; set; }
    public TransactionDirection Direction { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
    public string Counterparty { get; set; } = "";
    public string Reference { get; set; } = "";
    public TransactionCategory Category { get; set; }
    public TransactionStatus Status { get; set; }
    public decimal RunningBalance { get; set; }
}

public enum AccountType { Checking, Savings, MoneyMarket, Cd, Loan }
public enum AccountStatus { Open, Frozen, Closed }
public enum TransactionDirection { Debit, Credit }
public enum TransactionCategory { Payroll, BillPay, Atm, Transfer, Pos, Interest, Fee, Check, Wire, Other }
public enum TransactionStatus { Pending, Posted, Reversed }

// ─── DTOs ────────────────────────────────────────────────────────────────────

public class AccountDto
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public AccountType Type { get; set; }
    public string Currency { get; set; } = "";
    public decimal CurrentBalance { get; set; }
    public decimal AvailableBalance { get; set; }
    public decimal PendingDebits { get; set; }
    public decimal PendingCredits { get; set; }
    public DateTime OpenedOn { get; set; }
    public DateTime? ClosedOn { get; set; }
    public AccountStatus Status { get; set; }
    public string OwnerName { get; set; } = "";
    public List<TransactionDto> Transactions { get; set; } = new();
}

public class TransactionDto
{
    public Guid Id { get; set; }
    public DateTime PostedAt { get; set; }
    public DateTime EffectiveDate { get; set; }
    public TransactionDirection Direction { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
    public string Counterparty { get; set; } = "";
    public string Reference { get; set; } = "";
    public TransactionCategory Category { get; set; }
    public TransactionStatus Status { get; set; }
    public decimal RunningBalance { get; set; }
}

[Facet.Facet(typeof(TransactionEntity))]
public partial class TransactionFacetDto;

[Facet.Facet(typeof(AccountEntity), NestedFacets = [typeof(TransactionFacetDto)])]
public partial class AccountFacetDto;
