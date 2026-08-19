# Claude Code Deep Dive - Section 3: Configuring It at Scale Prompts

These are the prompts run on screen during this section's lessons. Copy them as they are into Claude Code and follow along.

## L2 - The .claude Directory and Settings in Depth

Creates a shared .claude/settings.json that locks down sensitive files and gates destructive git commands.

```
Create a shared .claude/settings.json (checked in, not the .local one). It should:
- deny reads and edits of any appsettings*.json under app/api and any .env file
- deny edits to app/api/Api/Migrations/** so generated migrations aren't hand-edited
- ask before running destructive git (push, reset --hard)
```

## L3 - Modular CLAUDE.md at Scale

Splits a single CLAUDE.md into a lean root file plus API and web area files, wired together with @-imports.

```
Refactor our CLAUDE.md into a modular setup:
- a lean root CLAUDE.md: one-line project summary, the stack list, the run steps, and imports of the two area files
- app/api/CLAUDE.md: orientation for the API (vertical slices under Features/, shared infra in Data/, how to run, migrate, and test the API)
- app/web/CLAUDE.md: orientation for the web app (components/pages/lib layout, pnpm commands, dev server)
Use @-imports in the root.
```

## L5 - Modular Rules with .claude/rules/

Creates path-scoped rule files under .claude/rules/ for backend, frontend, and test conventions.

```
Create .claude/rules/ with three path-scoped rule files:
- backend.md, paths app/api/** : DTOs are record types; map to/from entities explicitly (no AutoMapper); async all the way with CancellationToken; features are vertical slices under Features/; warnings fail the build, fix don't suppress.
- frontend.md, paths app/web/** : fetch through TanStack Query, never raw fetch in components; pnpm is the package manager.
- tests.md, paths app/tests/** : integration tests are snake_case Should_When, e.g. Post_Should_Return_400_When_Description_Empty.
Use paths frontmatter on each file.
```
