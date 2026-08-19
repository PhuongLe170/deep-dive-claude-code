---
name: verify
description: Launch the expense tracker (Postgres + API + web) and drive it in a browser to confirm a change works end to end. Use when verifying UI or API behavior in the running app.
---

# Verify the expense tracker

Bring the full stack up, then drive the web app with the Playwright MCP tools.

## Launch

From the repository root:

1. `docker compose up -d`
2. `dotnet tool restore && dotnet ef database update --project app/api/Api`
3. `dotnet run --project app/api/Api` (background, logs to a file) - http://localhost:5033
4. `pnpm --dir app/web install && pnpm --dir app/web dev` (background) - http://localhost:5173

Readiness poll (the API needs ~10s to build and boot):

```bash
curl -s -o /dev/null -w "%{http_code}" http://localhost:5033/api/expenses   # 200
curl -s -o /dev/null -w "%{http_code}" http://localhost:5173                # 200
```

## Gotchas

- **API routes are under `/api`**: `/api/expenses`, `/api/expenses/totals/monthly`,
  `/api/expenses/budgets`, `/api/expenses/budgets/comparison`. Hitting `/expenses`
  returns 404 and looks like a dead API. `expenses.http` at the root lists them all.
- **`docker compose up -d` can fail with a name conflict** on `expense-tracker-db`,
  because sibling worktrees in this repo define the same `container_name`. Check
  `docker ps --filter name=expense-tracker-db` first. If one is already up and healthy
  on 5432, reuse it. Same image and credentials. Do not remove the user's container.
- **Playwright MCP screenshots do not land on the local filesystem.** The
  `filename` argument returns a path that does not exist here. Use
  `browser_snapshot` for evidence instead. It is also more precise than a screenshot.
- Pass `target` to `browser_snapshot` to scope it to one region. Full-page snapshots
  of this app are long. Refs are invalidated by navigation and get an `f1e` prefix
  after a reload, so recapture rather than reusing old refs.

## Flows worth driving

- **Add expense**: fill Amount / Currency / Description / Date, click "Add expense".
  Verify a new row appears at the top of the table, "Monthly totals" increases for
  that currency, and "Spend vs budget" remaining decreases. Reload to confirm it persisted.
- **Delete expense**: "Delete" button on any row. No confirmation dialog fires.
- **Set budget**: sidebar form, per currency. Updates the "Spend vs budget" list.
- **Subscriptions**: separate route at `/subscriptions`.

Confirm the write actually reached the server with
`browser_network_requests` (filter `/api/expenses`) rather than trusting the UI.
A `POST => [201] Created` followed by refetches of the list, totals, and comparison
is the healthy signature.

## Validation behavior

Amount and Description are enforced client side by native HTML5 constraints. Empty
or negative values block submission with **no POST and no in-page error text**. The
only feedback is the native browser tooltip, which is invisible to the accessibility
snapshot. Confirm a rejected submit via the absence of a POST, not by looking for an
error message in the DOM.
