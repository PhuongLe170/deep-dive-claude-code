---
name: new-endpoint
description: Use when adding a new API resource or endpoint to this project. Scaffolds contracts, endpoints, and a Testcontainers test, following the Features/Expenses slice.
argument-hint: <ResourceName>
arguments: [resource]
model: claude-sonnet-5
allowed-tools: Read, Write, Edit, Bash
---

# new-endpoint

Scaffold a vertical slice for `$resource`, following `app/api/Api/Features/Expenses/`
as the pattern. Read the reference slice for the exact conventions; do not invent them.

## Name forms

Substitute `$resource` once, here, then use the placeholders everywhere below. Never
splice `$resource` inline into a longer identifier (e.g. `$resourceEndpoints`), the
argument parser will not substitute it.

- `<Res>`  = `$resource`, singular PascalCase. Example: `Product`.
- `<Ress>` = `<Res>` pluralised by English rules, not a bare `+ s`:
  - `-y` (after a consonant) → `-ies`  (Category → Categories)
  - `-s`, `-x`, `-z`, `-ch`, `-sh` → `+ es`  (Box → Boxes)
  - otherwise → `+ s`  (Product → Products)
- `<ress>` = `<Ress>` lower-cased. Example: `products`.
- `<res>`  = `<Res>` camelCase, i.e. first letter lower-cased. Example: `product`.
  Only the web half needs this one.

## Before generating

Read `app/api/Api/Features/<Ress>/<Res>.cs` (the entity) and use its **properties**
as the DTO fields. Do NOT copy the Expense fields. The entity is the source of truth
for what the resource is, so a field like `Date` or `CreatedAt` that exists on Expense
but not on this entity must not appear, and a field this entity has that Expense lacks
must appear.

Also read the reference slice so the generated code matches the house style exactly:

- `app/api/Api/Features/Expenses/ExpenseContracts.cs`
- `app/api/Api/Features/Expenses/ExpenseEndpoints.cs`
- `app/tests/Api.Tests/ExpenseApiTests.cs`
- `.claude/rules/backend.md` and `.claude/rules/tests.md` (binding)

If the entity reuses a shared helper the Expenses slice already owns (for example a
currency allow-list), call that helper. Do not duplicate it into the new slice.

## Files to write (pattern: app/api/Api/Features/Expenses/)

- `app/api/Api/Features/<Ress>/<Res>Contracts.cs`
  `record` DTOs (`Create<Res>Request`, `<Res>Response`) plus an explicit static
  mapping class with `ToEntity()` / `ToResponse()`. No AutoMapper.
- `app/api/Api/Features/<Ress>/<Res>Endpoints.cs`
  A static class exposing `Map<Res>Endpoints(this IEndpointRouteBuilder)`. Group with
  `app.MapGroup("/api/<ress>")`. Handlers are `private static` methods, async all the
  way, each taking a `CancellationToken` passed down to EF Core. Validate in the
  handler and return `Results.ValidationProblem(errors)` for a 400.
- `app/tests/Api.Tests/<Ress>/<Res>EndpointsTests.cs`
  Testcontainers integration test mirroring `ExpenseApiTests.cs`: a `ValidRequest`
  helper, a 201-and-persist test, a GET-ordering test, a delete-then-404 test, and at
  least one 400 validation test. Test names are snake_case `Should_When`. The class
  ctor takes `ExpenseApiFactory` and resets state in `InitializeAsync`.

Keep the new slice disjoint from Expenses. Every path is derived from `<Res>`/`<Ress>`.
Never write to or edit a file under `Features/Expenses/`.

## Load on demand

- entity has an `enum` property → read `${CLAUDE_SKILL_DIR}/references/enums.md`
- generating the web side       → read `${CLAUDE_SKILL_DIR}/references/web-slice.md`

## Finish

1. Confirm every file above exists and the DTO fields match the entity that was read,
   field for field: no Expense-only field leaked in, none of the entity's fields dropped.
2. The API half cannot build until the entity is registered on the DbContext, which
   this skill does not do. So emit the following as one copy-paste block for the user,
   with the real names substituted, ending in the verify command:

   - Add `public DbSet<<Res>> <Ress> => Set<<Res>>();` and an `OnModelCreating`
     configuration block to `app/api/Api/Data/ExpenseDbContext.cs`
     (precision on any decimal, max length on any short string, enum stored as you
     prefer), plus `using ExpenseTracker.Api.Features.<Ress>;`.
   - Add `app.Map<Res>Endpoints();` and the same `using` to `Program.cs`.
   - Add `await db.<Ress>.ExecuteDeleteAsync();` to `ExpenseApiFactory.ResetAsync()`
     so rows do not leak between tests.
   - `dotnet ef migrations add Add<Ress> --project app/api/Api`
   - `dotnet build && dotnet test --filter FullyQualifiedName~<Res>`

3. For the web side (only when the web reference was loaded), run
   `pnpm --dir app/web typecheck` and `pnpm --dir app/web lint` yourself and fix
   anything red before reporting done.
