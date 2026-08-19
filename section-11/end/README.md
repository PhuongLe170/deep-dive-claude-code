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

## API

The API listens on `http://localhost:5033` and needs no authentication. `expenses.http` at the root of this folder holds the same requests, runnable from an editor's REST client.

Wire format, everywhere:

- Property names are camelCase in both directions. Request binding is case insensitive.
- Enums are strings, not numbers: `"Monthly"`, not `1`.
- `date` and `renewalDate` are `yyyy-MM-dd`. `createdAt` and `updatedAt` are ISO 8601.
- Amounts are `numeric(18,2)`, so they carry up to two decimals.

| Method | Route | Returns |
| --- | --- | --- |
| GET | `/` | 200, plain-text greeting |
| POST | `/api/expenses` | 201 + expense |
| GET | `/api/expenses` | 200 + expense array |
| PUT | `/api/expenses/{id}` | 200 + expense, or 404 |
| DELETE | `/api/expenses/{id}` | 204, or 404 |
| GET | `/api/expenses/totals/monthly` | 200 + totals array |
| GET | `/api/expenses/budgets` | 200 + budget array |
| PUT | `/api/expenses/budgets` | 200 + budget |
| GET | `/api/expenses/budgets/comparison` | 200 + comparison array |
| POST | `/api/subscriptions` | 201 + subscription |
| GET | `/api/subscriptions` | 200 + subscription array |
| PUT | `/api/subscriptions/{id}/status` | 200 + subscription, or 404 |
| DELETE | `/api/subscriptions/{id}` | 204, or 404 |
| POST | `/api/companies` | 201 + company |
| GET | `/api/companies` | 200 + company array |
| DELETE | `/api/companies/{id}` | 204, or 404 |

### Expenses

`POST /api/expenses` creates one. `note` is optional.

```bash
curl -X POST http://localhost:5033/api/expenses \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 42.50,
    "currency": "EUR",
    "description": "Team lunch",
    "date": "2026-07-20"
  }'
```

```json
{
  "id": "0d6d4a3f-1c5e-4a0b-9a7e-1f2b3c4d5e6f",
  "amount": 42.50,
  "currency": "EUR",
  "description": "Team lunch",
  "date": "2026-07-20",
  "createdAt": "2026-07-20T14:23:11.123456+00:00",
  "note": null
}
```

The `201` carries a `Location` header of `/api/expenses/{id}`. There is no `GET` on that path, only `PUT` and `DELETE`.

`GET /api/expenses` returns every expense, newest date first, newest created first as the tiebreak.

`PUT /api/expenses/{id}` replaces every editable field of one expense and returns `200` with the updated row. It is a full replacement, not a patch, so send all of them. Omitting `note` clears it.

```bash
curl -X PUT http://localhost:5033/api/expenses/0d6d4a3f-1c5e-4a0b-9a7e-1f2b3c4d5e6f \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 51.00,
    "currency": "EUR",
    "description": "Team lunch (corrected)",
    "date": "2026-07-20",
    "note": "Split with the design team"
  }'
```

`id` and `createdAt` are preserved, so an edit keeps the row's identity and its original creation time. The body is validated before the row is looked up, so a bad payload is a `400` whether or not the id exists. An unknown id is a `404`.

`DELETE /api/expenses/{id}` returns `204 No Content`. Deleting the same id twice gives `204` and then `404 Not Found`.

### Monthly totals

`GET /api/expenses/totals/monthly` sums expenses per month and currency.

```bash
curl http://localhost:5033/api/expenses/totals/monthly
```

```json
[
  { "year": 2026, "month": 8, "currency": "EUR", "total": 412.75 },
  { "year": 2026, "month": 8, "currency": "USD", "total": 89.99 },
  { "year": 2026, "month": 7, "currency": "EUR", "total": 1284.50 },
  { "year": 2026, "month": 6, "currency": "EUR", "total": 967.20 }
]
```

The month is two integers, `year` and `month` (1 to 12, not zero padded). There is no `"2026-07"` string in the payload. One row comes back per year, month, and currency, so a month with both EUR and USD spending produces two rows. Ordering is year descending, then month descending, then currency ascending.

Pass `year` and `month` to narrow the result to a single month. They are all or nothing: send both, or neither.

```bash
curl "http://localhost:5033/api/expenses/totals/monthly?year=2026&month=7"
```

```json
[
  { "year": 2026, "month": 7, "currency": "EUR", "total": 1284.50 },
  { "year": 2026, "month": 7, "currency": "GBP", "total": 45.00 }
]
```

Anything else is a `400`:

| Condition | Key | Message |
| --- | --- | --- |
| `year` without `month` | `month` | Month is required when year is supplied. |
| `month` without `year` | `year` | Year is required when month is supplied. |
| `year` outside 1 to 9999 | `year` | Year must be between 1 and 9999. |
| `month` outside 1 to 12 | `month` | Month must be between 1 and 12. |
| Month later than the current UTC month | `month` | Month cannot be in the future. |

The current month is allowed. Only a strictly future month is rejected.

### Budgets

A budget is one recurring amount per currency, applied to every month.

`GET /api/expenses/budgets` lists them, currency ascending.

`PUT /api/expenses/budgets` upserts on currency. It answers `200` for a create as well as an update, never `201`.

```bash
curl -X PUT http://localhost:5033/api/expenses/budgets \
  -H "Content-Type: application/json" \
  -d '{ "currency": "EUR", "amount": 1500.00 }'
```

```json
{
  "id": "7b1e2c9d-4f8a-4c3b-8e5d-6a7b8c9d0e1f",
  "currency": "EUR",
  "amount": 1500.00,
  "updatedAt": "2026-08-01T09:12:44.870231+00:00"
}
```

`GET /api/expenses/budgets/comparison` pairs each month's spending with the budget for that currency. `budget` and `remaining` are `null` when no budget row exists for it. A month with a budget but no expenses does not appear, since expenses drive the grouping.

```json
[
  { "year": 2026, "month": 8, "currency": "EUR", "spent": 412.75, "budget": 1500.00, "remaining": 1087.25 },
  { "year": 2026, "month": 8, "currency": "USD", "spent": 89.99, "budget": null, "remaining": null }
]
```

### Subscriptions

`POST /api/subscriptions` creates one. `billingCycle` is `"Monthly"` or `"Annual"`. `status` is
`"Active"` or `"Cancelled"` and is optional, defaulting to `"Active"`.

```bash
curl -X POST http://localhost:5033/api/subscriptions \
  -H "Content-Type: application/json" \
  -d '{
    "vendor": "Figma",
    "amount": 21.00,
    "currency": "EUR",
    "billingCycle": "Monthly",
    "renewalDate": "2026-08-01"
  }'
```

```json
{
  "id": "3f9c8b7a-2d1e-4f0a-9b8c-7d6e5f4a3b2c",
  "vendor": "Figma",
  "amount": 21.00,
  "currency": "EUR",
  "billingCycle": "Monthly",
  "renewalDate": "2026-08-01",
  "status": "Active"
}
```

A subscription has no `createdAt`.

`GET /api/subscriptions` lists them, soonest renewal first, vendor ascending as the tiebreak.

Pass `status` to return only the subscriptions in that state. Unlike `currency`, the match is case
insensitive, so `?status=active` and `?status=Active` are the same request. Omit it and every row
comes back.

```bash
curl "http://localhost:5033/api/subscriptions?status=Active"
```

A value that is neither `Active` nor `Cancelled` is a `400` with an `errors` dictionary keyed by the
lowercase `status`, matching how monthly totals reports a bad query parameter.

`PUT /api/subscriptions/{id}/status` changes the status of one subscription and returns `200` with
the updated row. It takes only the status, so the rest of the subscription is left alone.

```bash
curl -X PUT http://localhost:5033/api/subscriptions/3f9c8b7a-2d1e-4f0a-9b8c-7d6e5f4a3b2c/status \
  -H "Content-Type: application/json" \
  -d '{ "status": "Cancelled" }'
```

The body is validated before the row is looked up, so an unknown status is a `400` whether or not the
id exists, keyed by the PascalCase `Status`. An unknown id is a `404`.

`DELETE /api/subscriptions/{id}` returns `204`, or `404` if it is already gone.

### Companies

`POST /api/companies` takes `name` and `website`, both required and 200 characters or fewer. The website is not format checked, so any non-empty string passes.

```bash
curl -X POST http://localhost:5033/api/companies \
  -H "Content-Type: application/json" \
  -d '{ "name": "Dometrain", "website": "https://dometrain.com" }'
```

```json
{
  "id": "5c4b3a2d-1e0f-4a9b-8c7d-6e5f4a3b2c1d",
  "name": "Dometrain",
  "website": "https://dometrain.com",
  "createdAt": "2026-08-01T09:15:02.441908+00:00"
}
```

`GET /api/companies` lists them newest first. `DELETE /api/companies/{id}` returns `204`, or `404`.

### Validation errors

Every `400` is `application/problem+json`. Handler validation adds an `errors` dictionary keyed by field.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Currency": ["Currency must be one of: EUR, USD, GBP, JPY."]
  },
  "traceId": "00-0738f1f19e1e39a134cd3089b5d45ceb-0277747873ae0164-00"
}
```

Keys follow the handler, so most are PascalCase: `Amount`, `Description`, `Currency`, `Vendor`. Monthly totals validates query parameters instead of a body, so its keys are the lowercase `year` and `month`.

Request binding runs before the handler, so a body the JSON reader cannot bind never reaches that validation and comes back **without** an `errors` member:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "traceId": "00-0738f1f19e1e39a134cd3089b5d45ceb-0277747873ae0164-00"
}
```

That is what you get for `"date": ""`, for `"billingCycle": "Weekly"`, and for malformed JSON. Read `errors` defensively; it is absent on binding failures.

Currency must be `EUR`, `USD`, `GBP`, or `JPY`, and the check is case sensitive. `"eur"` is rejected.

Amount must be greater than zero. On expenses it must also be no more than `9999999999999999.99`, the largest value the `numeric(18,2)` column holds, so anything larger is a `400` rather than a database overflow. Subscriptions and budgets sit on the same column type but check only that the amount is positive, so an oversized amount there reaches Postgres as a `22003` overflow and surfaces as a `500`.

