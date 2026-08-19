---
name: http-samples
description: Generate a requests.http REST-client file with a sample request for the API endpoints available inside a file, so you can hit the API by hand.
allowed-tools: Read, Write, Bash(git *), Bash(node *), Grep
model: claude-haiku-4-5
argument-hint: "[name of the API Resource]"
arguments: [resource]
---

Git PR Diff: !`git diff`

Generate the `.http` for the **$resource** resource.

Its slice lives at `app/api/Api/Features/$1s/` (folder pluralised, files prefixed with `$resource`), following the Expenses convention.

Find the live routes currently mapped for this resource: grep `app/api/Api/Features/$resources/` for `MapGet`, `MapPost`, `MapPut`, `MapDelete` and `MapGroup`.

For every route in the group, add one labelled request:
- Follow the REST-client conventions in `${CLAUDE_SKILL_DIR}/references/http-conventions.md`.
- Name the files based on the API Resource name. Example: `clients.http` for /clients endpoints.

Keep it to the endpoints that exist today. Do not invent routes.

After writing the file, validate it:

```
node ${CLAUDE_SKILL_DIR}/scripts/validate-http-file.js <resource>.http
```

Fix every reported error and re-run until it exits 0. Warnings are advisory.


## Gotchas
-  When the resource doesn't exist, propose to create a .http file for that resource based on REST API best practices.