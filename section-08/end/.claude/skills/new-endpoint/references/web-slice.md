# Web slice

Read this when generating the React side. It reuses the base `<Res>` / `<Ress>` /
`<ress>` / `<res>` name forms and the "read the entity" rule. Only the web-specific
parts are here.

Pattern to follow (read them, do not paste them):

- `app/web/src/lib/api.ts`
- `app/web/src/components/ExpenseForm.tsx`
- `app/web/src/components/ExpenseTable.tsx`
- `app/web/src/pages/ExpensesPage.tsx`
- `.claude/rules/frontend.md` (binding)

## Name forms on the web (camelCase matters)

JSON properties, query keys, and api function names are **not** PascalCase. Get this
wrong and it is not a compile error, it is a silent query-key mismatch that stops the
list refreshing after a create.

- route segment / query key: `<ress>`  (e.g. `["products"]`, `/api/products`)
- api functions: `get<Ress>`, `create<Res>`, `delete<Res>`
- components: `<Res>Form.tsx`, `<Res>Table.tsx`, page `<Res>sPage.tsx`
- TS interface fields: camelCase, mirroring the C# `record` field for field.

## Files

- `app/web/src/lib/api.ts` — **append, do not replace.** This file is shared with
  Expenses. Add the `<Res>` and `Create<Res>Request` interfaces and three functions
  (`get<Ress>`, `create<Res>`, `delete<Res>`) alongside the existing Expenses exports.
  Reuse the existing `handle<T>` helper and `API_BASE`. Deleting the Expenses functions
  is a failure.
- `app/web/src/components/<Res>Form.tsx` — `useMutation` calling `create<Res>`, local
  validation, invalidates `["<ress>"]` on success.
- `app/web/src/components/<Res>Table.tsx` — `useQuery({ queryKey: ["<ress>"] })` to
  list, `useMutation` calling `delete<Res>` to delete, with loading / error / empty
  states. The delete mutation invalidates `["<ress>"]`.
- `app/web/src/pages/<Res>sPage.tsx` — replace the "coming soon" placeholder **only if
  it still is that placeholder**. If real work is already there, show the diff and ask
  rather than clobber it. Composes the form and table; does no data fetching itself.

There is no monthly-totals equivalent. Do not invent one.

## Rules that bite

- Never call raw `fetch` from a component. It goes in `lib/api.ts` and reaches the
  component through TanStack Query.
- pnpm only, never npm or yarn.
- If a field is an enum, see `${CLAUDE_SKILL_DIR}/references/enums.md`. With the string
  converter in place, type it as a string union of the enum members and render directly.
- If the entity reuses the shared currency list, import `CURRENCY_CODES` / `formatAmount`
  from `lib/currency.ts` rather than re-declaring currencies.

## Finish (web)

Run `pnpm --dir app/web typecheck` and `pnpm --dir app/web lint` and fix anything red
before reporting done. Vite's dev server does not typecheck, so a page that renders can
still fail the build.
