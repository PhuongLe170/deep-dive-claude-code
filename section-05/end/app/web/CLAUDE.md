# Web

React 19 + TypeScript single-page app, built with Vite. Creates, lists, and deletes expenses and shows monthly totals.

Conventions for writing web code live in `.claude/rules/frontend.md`.

## Commands

Package manager is pnpm. From the repository root, prefix with `--dir app/web`:

- Install: `pnpm --dir app/web install`
- Dev server: `pnpm --dir app/web dev` (http://localhost:5173, expects the API on 5033)
- Typecheck: `pnpm --dir app/web typecheck`
- Lint: `pnpm --dir app/web lint` (oxlint)
- Build: `pnpm --dir app/web build`
