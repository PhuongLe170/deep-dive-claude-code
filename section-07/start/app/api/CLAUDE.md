# API

.NET 10 Minimal API over PostgreSQL. Persists expenses and serves them to the web app.

Conventions for writing API code live in `.claude/rules/backend.md`.

## Run, migrate, test

From the repository root:

- Run: `dotnet run --project app/api/Api` (http://localhost:5033)
- Apply migrations: `dotnet ef database update --project app/api/Api` (needs `dotnet tool restore` once)
- Add a migration: `dotnet ef migrations add <Name> --project app/api/Api`
- Test: `dotnet test` (Docker must be running)

Postgres must be up first: `docker compose up -d`.
