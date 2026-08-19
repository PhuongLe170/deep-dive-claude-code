# Migrations

EF Core migrations for `ExpenseDbContext`, running against PostgreSQL through Npgsql. They live in
`Api/Migrations/` and are applied in timestamp order, oldest first.

For the resulting schema, see `db.md` at the repository root.

## The list

| # | Migration | Date | What it does |
| --- | --- | --- | --- |
| 1 | `20260713213503_InitialCreate` | 2026-07-13 | Creates `Expenses` with `Id`, `Amount`, `Currency`, `Description`, `Date`, `CreatedAt`. |
| 2 | `20260721094705_AddSubscriptions` | 2026-07-21 | Creates `Subscriptions` with `Id`, `Vendor`, `Amount`, `Currency`, `BillingCycle`, `RenewalDate`. |
| 3 | `20260721155840_AddExpenseNote` | 2026-07-21 | Adds the nullable `Note` column to `Expenses`. |
| 4 | `20260722135502_AddMonthlyBudget` | 2026-07-22 | Creates `MonthlyBudgets` with a unique index on `Currency`. |
| 5 | `20260722155417_AddCompanies` | 2026-07-22 | Creates `Companies` with `Id`, `Name`, `Website`, `CreatedAt`. |
| 6 | `20260801143258_AddSubscriptionStatus` | 2026-08-01 | Adds the `Status` column to `Subscriptions`, defaulting to `Active`. |

Each one has a matching `.Designer.cs` holding the model snapshot at that point.
`ExpenseDbContextModelSnapshot.cs` holds the current model and is rewritten on every
`migrations add`.

## Notes worth carrying

- `CreatedAt` on `Expenses` and `Companies` has a database default of `now()`. `UpdatedAt` on
  `MonthlyBudgets` does not, so the code sets it.
- The unique index `IX_MonthlyBudgets_Currency` is what makes `PUT /api/expenses/budgets` an upsert
  on currency.
- Enums are stored as strings in `varchar(16)` columns: `BillingCycle` and `Status`.
- `AddSubscriptionStatus` adds a non-nullable column with a default, so existing rows come out as
  `Active`.
- There are no foreign keys and no join tables. Every table stands alone.

## Commands

From the repository root, with Postgres up (`docker compose up -d`):

```bash
dotnet tool restore                                     # once, pins the EF Core 10 CLI
dotnet ef database update --project app/api/Api         # apply
dotnet ef migrations add <Name> --project app/api/Api   # add
dotnet ef migrations remove --project app/api/Api       # drop the last one, if unapplied
```
