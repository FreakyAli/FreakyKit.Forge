# Scenario: Banking Transaction Ledger

**Domain:** Personal / commercial banking — an account header with its associated transaction
history rendered for an account-detail view or monthly statement.

## What this represents

A bank account API response showing the account header (13 scalar fields covering identifiers,
balances, lifecycle dates, status) followed by a transaction collection of 500 rows. Each
transaction has 11 fields, most of them decimals or DateTime. Tests throughput on a
high-volume, decimal-dense, value-record-shaped collection.

## Inspiration (hybrid attribution)

The shape is representative of patterns found in:

- **[Plaid REST API responses](https://plaid.com/docs/api/products/transactions/)** (commercial
  but publicly documented) — the `Transaction` object has the same `amount`, `iso_currency_code`,
  `posted_at`, `merchant_name`, `category`, `status` shape. Our `TransactionEntity` is a near
  one-to-one with Plaid's transaction representation simplified.
- **[Open Banking Implementation Entity (OBIE) spec](https://standards.openbanking.org.uk/)**
  (Open licence) — the UK Open Banking transaction schema uses a similar `BookingDateTime`,
  `ValueDateTime`, `CreditDebitIndicator`, `TransactionInformation` quadruple.
- **[GnuCash](https://github.com/Gnucash/gnucash)** (GPL — shape inspiration only, not copied
  due to licence) — its double-entry split-and-running-balance model informed the
  `RunningBalance` denormalisation we keep on each transaction for fast statement rendering.

## Why this is interesting to benchmark

- **High-volume collection** (500 transactions per account is realistic for a month of activity
  on an active checking account). Stresses per-element mapping overhead.
- **Decimal-dense rows** — currency math is the bulk of the payload. Decimal copying is
  intrinsically slower than primitive copying, and library overhead matters less proportionally.
- **Value-record shape** — flat transaction rows with no nested objects test the simplest possible
  inner-loop mapping cost.
- **Guid keys throughout** — production banking systems use Guid (or string-encoded Guid)
  identifiers; tests how each library handles Guid copying.

## Fixture rationale

The seeded account is a personal checking account with 500 transactions covering ~62 days of
activity (one transaction every 3 hours). Direction is randomised but biased toward debits
(2:1 typical for a payroll-funded consumer account). Amounts use realistic distributions
($10-$810 range). The running balance is computed forward so the data is internally consistent.
Mirrors what a Plaid `/transactions/get` response would return for a 60-day window.

## Modifications

None — POCO modelling only. No bank-specific framework attributes used. Iteration count on the
benchmark is reduced to 30 (from the project default of 50) because each iteration maps 500
transactions and total wall time would otherwise be excessive.
