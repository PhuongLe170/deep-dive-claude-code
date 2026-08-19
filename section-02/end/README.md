# Deep Dive: Claude Code - Expense Tracker

A small but realistic .NET 10 and React expense tracker. An API persists expenses and a single-page app creates, edits, lists, and deletes them and shows monthly totals. Subscriptions, the solo track, lives alongside it in its own files.

## Prerequisites

| Tool | Version | Pinned by |
| --- | --- | --- |
| .NET SDK | any 10.x, 10.0.100 or newer | `global.json` |
| Node.js | 24 (LTS) | `app/web/.nvmrc` |
| pnpm | 11.0.8 | `packageManager` in `app/web/package.json` |
| Docker | any current version | |

The course was recorded against SDK 10.0.203, Node 24, and pnpm 11.0.8. The SDK floor is deliberately lower so any .NET 10 install works.

Check them in one go:

```bash
dotnet --info | head -4
node --version
pnpm --version
docker compose version
```

## Quick start

Three steps, from this folder. Each one has a signal telling you it worked.

**1. Database and schema.**

```bash
docker compose up -d --wait
dotnet tool restore && dotnet ef database update --project app/api/Api
```

`--wait` blocks until Postgres passes its healthcheck, which the migration needs. Success looks like `Done.` from the migration. `dotnet tool restore` installs the pinned EF Core 10 CLI from `.config/dotnet-tools.json`, so migrations run against .NET 10 regardless of any globally installed `dotnet-ef`.

**2. Terminal A, the API.**

```bash
dotnet run --project app/api/Api
```

This holds the terminal. Leave it running. Success looks like `Now listening on: http://localhost:5033`. Confirm from a third terminal with `curl http://localhost:5033`, which answers with the greeting.

**3. Terminal B, the web app.**

```bash
pnpm --dir app/web install && pnpm --dir app/web dev
```

Also holds the terminal. Success looks like `Local: http://localhost:5173/`. Open that and you should see the expense list.

The database uses demo credentials (`expense` / `expense`, database `expenses`), defined in `docker-compose.yml` and read by the API from `ConnectionStrings:Postgres` in `app/api/Api/appsettings.Development.json`. When you are done, stop both terminals with `Ctrl+C` and run `docker compose down -v` to remove the container and its volume.

## Tests and checks

```bash
dotnet test                       # API integration tests
pnpm --dir app/web typecheck      # frontend type check
pnpm --dir app/web lint           # frontend lint (oxlint)
pnpm --dir app/web build          # frontend production build
```

The integration tests spin up a throwaway Postgres container with Testcontainers, so Docker must be running. The .NET build treats warnings as errors, so fix them rather than suppress them.

## Ports

| Service | URL |
| --- | --- |
| PostgreSQL | `localhost:5432` |
| API | http://localhost:5033 |
| Web app | http://localhost:5173 |

Open the app at **http://localhost:5173**. Subscriptions is at http://localhost:5173/subscriptions.
