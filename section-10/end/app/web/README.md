# Web

The front end of the Expense Tracker. React 19 and TypeScript, built with Vite, data fetching through TanStack Query. Package manager is pnpm.

It talks to the API at `http://localhost:5033`, which must be running. The API allows the Vite dev origin through CORS in Development only.

## Scripts

```bash
pnpm install
pnpm dev          # dev server on http://localhost:5173
pnpm typecheck    # tsc -b --noEmit
pnpm lint         # oxlint, config in .oxlintrc.json
pnpm build        # tsc -b && vite build
pnpm preview      # serve the production build
```

## Layout

- `src/App.tsx`: two routes, `/` for Expenses and `/subscriptions` for the solo track.
- `src/pages/`: one component per route.
- `src/components/`: the forms and tables each page composes.
- `src/lib/api.ts`: every fetch call to the API. New endpoints go here, not inline in components.
- `src/lib/currency.ts`: mirrors the allowed currency list in `Features/Expenses/Currencies.cs`. Extend both together.

The wire format the API expects is documented in the `## API` section of the repository root `README.md`, from Section 9 on.
