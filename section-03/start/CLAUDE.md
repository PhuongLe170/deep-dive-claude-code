# Expense Tracker

A small expense tracker. An API persists expenses and a single-page app creates, lists, and deletes them and shows monthly totals.

## Stack

- **API** (`app/api/Api/`): .NET 10 Minimal API, C#, EF Core with the Npgsql provider.
- **Web** (`app/web/`): React + TypeScript, built with Vite, data fetching via TanStack Query. Package manager is pnpm.
- **Tests** (`app/tests/Api.Tests/`): xUnit v3 integration tests using Testcontainers for Postgres.
- **Database**: PostgreSQL, run via the root `docker-compose.yml`.

## Run it

From the repository root:

1. `docker compose up -d`
2. `dotnet tool restore && dotnet ef database update --project app/api/Api`
3. `dotnet run --project app/api/Api` (API on http://localhost:5033)
4. `pnpm --dir app/web install && pnpm --dir app/web dev` (web on http://localhost:5173)

Run the tests with `dotnet test` (Docker must be running).

## Conventions

- API features are vertical slices under `Features/` (e.g. `Features/Expenses`); shared infrastructure like the DbContext lives in `Data/`.
- Request and response DTOs are `record` types; map to and from entities explicitly (no AutoMapper).
- The frontend fetches through TanStack Query, not raw `fetch` in components.
- Integration tests are named in snake_case `Should_When` style, e.g. `Post_Should_Return_400_When_Description_Empty`.
- Warnings fail the build (`TreatWarningsAsErrors`). Fix them, don't suppress.
