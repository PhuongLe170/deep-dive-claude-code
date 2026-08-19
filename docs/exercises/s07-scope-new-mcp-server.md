# S7 Solo - Add and Scope a New MCP Server

> Section 7, MCP Servers. Solo. Same shared project. Hints and troubleshooting, not a full hand-hold.

## Mission

In the lecture I wired the Playwright MCP server into the workflow and locked it down: registered at project scope, allowlisted the tools I wanted, denied the one that runs arbitrary code, verified trust before use. Now you add a new MCP server of your own and scope it *before* you trust it.

Your server is a database MCP server pointed at the dev Postgres this repo already runs. It is the self-contained choice, because `docker-compose.yml` already ships the database and nothing external is involved. It is also the sharper exercise, because it forces you into a limit the Playwright server let you skate past: **a permission rule cannot see inside a SQL string.**

MCP config is repo-global. It does not belong to the Expenses or Subscriptions area, so this is a parallel task with a hint and an acceptance criterion, not a second feature area.

By the end, the new server shows up in `/mcp`, a schema question is answered from the live database, and a write attempt is stopped **twice**: once by a rule you wrote, and once by the server itself after you deliberately approve it.

**Learning Objectives**

- Register a new MCP server at project scope alongside an existing one.
- Write allow and ask rules for a server before granting it trust, not after.
- Discover where your permission rules stop, and configure the server's own guardrail to cover the gap.
- Prove a constraint holds by testing it from the denied side, not just the allowed one.

---

## Prerequisites

- S7L7 finished. You have a working `.mcp.json` with the Playwright server registered and scoped, and you have used `/mcp` to confirm a server loaded and to read its tool list before writing rules. Check `.claude/settings.json` too, not just whether `.mcp.json` exists: S7L7's result is only fully in place if `browser_evaluate` is explicitly denied there. If instead you see a wildcard `mcp__playwright__*` allow with only `browser_close`/`browser_tabs` denied (the shape S7L4 leaves behind), or there is no `.mcp.json` at all, S7L7's result was not committed: rebuild it yourself first (Playwright registered at project scope, its tools allowlisted, `browser_evaluate` denied) so this exercise starts from the state the lecture actually leaves you in.

  While you rebuild it, read the tool list in `/mcp` rather than copying the lecture's deny list from memory. The Playwright server now ships a **second** arbitrary-code tool, `browser_run_code_unsafe`, which did not exist when S7L7 was recorded. Deny both. This is the same lesson as the rest of the exercise arriving early: a deny list is only as good as your last look at the actual surface, and servers grow tools between releases.
- `docker compose up -d` has been run and Postgres is healthy. Check with `docker ps --filter name=expense-tracker-db`, which reports the container whether or not Compose is the thing that started it.
- The database has its schema. If you have not run migrations yet: `dotnet tool restore && dotnet ef database update --project app/api/Api`.
- Node 18 or newer, so `npx` can fetch the server.

**The connection details**, from `docker-compose.yml`:

| | |
| --- | --- |
| user | `expense` |
| password | `expense` |
| database | `expenses` |
| host and port | `localhost:5432` |

---

## What we're solving

Claude does not know your database. It knows your *migrations*, which is a different thing and a worse one. Ask it about the `expenses` table today and it reconstructs an answer by reading EF Core migration files in order and applying them in its head. That works until a migration was applied out of band, or a column was changed by hand, or the answer simply matters enough that "probably" is not good enough.

A database MCP server closes that gap the same way the Playwright server closed the "does the UI work" gap: by letting Claude look at the real thing instead of inferring it from source.

Then it opens a more interesting problem. A browser server has separate tools for separate actions, so a permission rule can draw a clean line between them. A database server does not. The tool that runs `SELECT` is the same tool that runs `DROP TABLE`, because both are just a string handed to `execute_sql`. **MCP permission rules match tool names. There is no argument specifier. Nothing you can write in `settings.json` can allow a read and refuse a write through the same tool.**

That is not a gap in your knowledge, it is a real limit of the layer. The fix lives one level down, in the server's own configuration, and finding it is the exercise.

---

## The task

Wire a database MCP server into the same `.mcp.json` as the Playwright server, at project scope, and constrain it on **two** levels before you trust it with anything.

**Hint**: the current package is `@bytebase/dbhub`, run over stdio. The older `server-postgres` package you will find in blog posts is archived, so ignore it. dbhub's surface is small: **two tools**, `search_objects` for exploring schemas and objects, `execute_sql` for running SQL. Confirm both names in `/mcp` before you write a single rule.

Do not run it as bare `npx -y @bytebase/dbhub`. Postgres support is an *optional* dependency (`pg`), and a plain `npx` install of dbhub does not reliably pull it in: the server starts, but fails to connect with `No connector found for DSN`. Force npm to install it alongside by naming both packages: `npx -y --package=@bytebase/dbhub@1.0.0 --package=pg@8 -- dbhub ...`.

**Pin the version, and pin dbhub exactly.** This guide is written against dbhub 1.0.0, and that specific release is the one whose behaviour every instruction below depends on. Between 0.x and 1.0 the tool names changed *and* the `--readonly` flag was removed in favour of the config file, so an unpinned `@latest` is one publish away from making the steps here wrong in exactly the way that is hardest to debug: your config looks right and the server disagrees. `pg` is only the driver underneath, so pinning its major (`pg@8`) is enough.

Layer one is yours. Allow the exploration tool outright. Put the SQL execution tool behind an `ask` rather than a `deny`, because you do want to run queries, you just want to see each one first.

Layer two is the server's. dbhub takes a `readonly` setting per tool in a TOML config file, which makes it refuse anything that writes, regardless of what you approved at the prompt. There is no shortcut here: the `--readonly` flag you will find in older blog posts was removed in dbhub 1.0, and passing it now is a hard error that tells you to use the config file. So copy this into `dbhub.toml` at the repository root, next to `.mcp.json`:

```toml
[[sources]]
id = "dev"
dsn = "${DATABASE_URL}"

[[tools]]
name = "execute_sql"
source = "dev"
readonly = true

[[tools]]
name = "search_objects"
source = "dev"
```

Point at it with `--config ./dbhub.toml`. The server is launched with the project root as its working directory, so a repo-relative path resolves. Pass the flag explicitly: dbhub's help text claims it also picks up `./dbhub.toml` on its own, but it does not, and the error you get is the generic "database connection configuration is required" rather than anything about the file.

Every line above is load-bearing, and it is worth understanding why before you move on:

- Drop the `source` field from a `[[tools]]` entry and dbhub refuses to start.
- Drop the second `[[tools]]` block and `search_objects` silently disappears from `/mcp`. **Declaring any `[[tools]]` entry is exclusive, not additive**: only the tools you name are exposed. That is the same failure shape as a misspelled permission rule, one layer down, and it is the one mistake here that fails quietly instead of loudly.

Keep the connection string out of any committed file, with `${DATABASE_URL:-...}` expansion and the compose credentials as the default.

> **You will hit a fork here, and noticing it is part of the exercise.** dbhub's `--config` and `--dsn` are mutually exclusive. `readonly` lives in the config file, so taking layer two means dropping `--dsn` from `.mcp.json`. The `${DATABASE_URL:-...}` default-value expansion, though, does **not** simply move into the TOML: dbhub's own config-file interpolation only resolves a bare `${VAR_NAME}`, with no fallback-default syntax. So the default has to keep living in `.mcp.json`, just relocated from the (now-gone) `--dsn` arg into the server's `env` block, and the TOML references the bare variable name that `env` block resolves at launch time. What changes is which file the viewer of your repo has to read to find out where the credential comes from, and that both files now play a part.
>
> Do it in that order and watch it happen: start with `--dsn` and the expansion in `.mcp.json`, then add the config file and see the flag have to go. **Do not treat this as a problem to route around.** Adding a security control cost you a bit of clarity elsewhere, which is what security controls usually do, and being able to state the trade-off is worth more than picking the "right" side of it.

**Restart the session before you check any of this.** Edits to `.mcp.json` are read at startup and do not take effect in the session you made them in. Skip the restart and `/mcp` will not show the new server no matter how correct your config is, and every criterion below fails for a reason that has nothing to do with your work.

**Done when**:

1. The server appears in `/mcp` with both its tools listed.
2. `What's the Expense table schema currently on the dev env?` is answered from the live database, not from the migration files. Push back if the answer looks reconstructed.

   Notice the two words doing the work. **"Currently"** and **"dev env"** both point at a running system, and that is usually enough to make Claude reach for the tool instead of reading migrations. A flatter phrasing like `What columns does the Expenses table have?` is a coin flip: the migration files are already in context and answering from them is cheaper. Try both and watch which one makes the tool call. Steering by naming the source of truth, rather than by ordering a tool call, is the transferable half of this step.

   Two details worth catching. The read is answered by `search_objects` alone, so **no `ask` prompt appears** here. That is your allow rule working, not your rule being skipped, and it is what makes the prompt in step 3 meaningful when it does show up. And EF Core created the table as `"Expenses"`, PascalCase and quoted. If you follow up with SQL of your own, unquoted `expenses` fails with `relation does not exist`, which is a casing problem and not an MCP one.
3. `Insert a test expense row directly into the dev database.` reaches the SQL execution tool and raises your `ask` prompt instead of running. Decline it. (Your rule did not refuse the write; it made sure a human got to. That distinction matters for step 4.)
4. Ask again, and this time **approve** it at the prompt. It still fails, because the server refuses to write. That refusal, the one that happens after a human said yes, is the whole point of the exercise.

---

## Pointers if you get stuck

- **One config file.** Keep the new server in the same `.mcp.json` as the Playwright server so the whole MCP surface is in one place. That is also what makes it clean to fold into the plugin in Section 12.
- **Read the tool list first.** Same habit as the lecture. Run `/mcp`, read the two tool names off the screen, and write your rules against those exact strings. A rule naming a tool that does not exist fails silently: your allow list looks correct and does nothing. dbhub's tool names have already changed once between releases.
- **`ask`, not `deny`, on the execution tool.** A `deny` would end the exercise early and you would never reach the interesting half. The lesson is what happens when a human approves something the server still will not do.
- **The two layers do different jobs, and this is the takeaway.** Your rule decides *which tool may run*. The server decides *what that tool is capable of*. Neither substitutes for the other: drop your rule and every query runs unseen, drop `readonly` and one approved prompt is all that stands between Claude and a `DELETE`.
- **Expect the config file to evict `--dsn`.** They are mutually exclusive. The default-value expansion does not follow `--dsn` into the TOML, though, because dbhub's own interpolation has no default syntax: keep the `${DATABASE_URL:-...}` in `.mcp.json`'s `env` block for the server, and let the TOML reference the bare variable name. A literal password in either file is the thing to avoid, and which file it lives in is the thing to have an opinion about.
- **The banner tells you whether layer two landed.** On startup dbhub prints its source and tool list, with a 🔒 next to every tool it considers read-only. Before you add the config you will see a bare `execute_sql` and a locked `search_objects`. After, both are locked. That is the fastest confirmation that your `readonly` setting is actually being read, and you can see it without leaving the terminal by running the configured command directly.
- **Test from the denied side.** A constraint you only ever tested on the happy path is a constraint you are hoping works. Acceptance criteria 3 and 4 exist to force both refusals.

---

## Troubleshooting

### The server does not appear in `/mcp`

Three usual causes, in order of likelihood. The `--` separator is missing from your `claude mcp add` command, so the server's own arguments were eaten by the Claude CLI. The JSON is malformed, so compare the new entry's shape against the Playwright entry you know loads. Or you edited `.mcp.json` inside a running session, which does not take effect. Restart the session after any config change.

To isolate it, run the configured command directly in your terminal. If it starts and sits waiting for input, the server is fine and your config is the problem. If it errors, the message names what is missing.

### It appears but shows as failed or disconnected

Postgres is probably not running or not ready. `docker compose ps` should show it healthy. If the container is up but the server still cannot connect, your DSN is wrong: check it against the credentials table above, and remember the database name is `expenses` while the user is `expense`.

### `docker compose up -d` fails on the container name

`Conflict. The container name "/expense-tracker-db" is already in use`. Every section of this course ships the same `docker-compose.yml`, so a container you started from a different section directory is still around under the same name. Compose treats it as a foreign container and refuses to recreate it. Start the existing one with `docker start expense-tracker-db`, or remove it with `docker rm -f expense-tracker-db` and bring it up fresh.

Check it landed healthy with `docker ps --filter name=expense-tracker-db`, not `docker compose ps`. If you took the `docker start` route, Compose still does not recognise the container, so `docker compose ps` prints an empty table. That reads like the container failed to start when it is actually up and healthy.

### The schema answer looks like it came from the migrations

It probably did. Claude will happily answer from files it has already read rather than making a tool call. Say so explicitly: `Query the live database, not the migration files, and show me the schema you got back.` If it still will not, check that the exploration tool is actually in your allow list and spelled correctly.

### The write attempt succeeds

Your fence has a gap, and it is worth finding out which layer failed rather than fixing both blindly. If no prompt appeared at all, your `ask` rule does not match the tool name. If a prompt appeared and you approved it and the write went through, the server's `readonly` setting is not in effect: check that the config file is actually being loaded (`--config` present, `--dsn` removed) and that the setting is on the right tool.

### Every query is blocked, including reads

You put the execution tool behind a `deny` instead of an `ask`, or `readonly` is stricter than you expected, or you denied the exploration tool by accident with an over-broad glob. Narrow it and re-test the read first.

### Claude used the server before you trusted it

Trust was granted implicitly, or a prior session already approved it. Revoke it, restart, and confirm you get the approval prompt before the first call. The habit is what you are practising, so make the flow explicit even if it worked by accident.

### The package will not run

Check the package name and that Node is 18 or newer. If `npx` is slow on first run, that is the download, and it is a one-off. If it times out entirely, `MCP_TIMEOUT` is in milliseconds: `MCP_TIMEOUT=60000 claude`.

If it starts, prints its banner, and then dies with `No connector found for DSN`, that is not a bad DSN: it is the optional `pg` driver missing. Run the configured command directly in your terminal to confirm, then fix `.mcp.json` to install both packages together: `npx -y --package=@bytebase/dbhub@1.0.0 --package=pg@8 -- dbhub <your args>`.

---

## Going further, if you want it

Nothing below is required.

- **A free protocol callback.** dbhub's docs say it serves the stateless 2026-07-28 MCP protocol natively and falls back to the older revision for older clients, on the same endpoint. So the server you just wired up is already on the new spec, and nothing in your config acknowledges that. That is what "the protocol changed and your workflow didn't" looks like in practice.
- **`max_rows`.** dbhub can cap how many rows a query returns. Set it and think about which lecture that belongs to: it is a context-cost control, not a security control. Both layers of this exercise had a security reading. This one does not, and noticing the difference is the skill.
- **Count what you now pay.** Run `/context` with both servers loaded. Playwright is the expensive one by a wide margin. Ask yourself honestly whether you would keep both configured in a repo you worked in every day, and what you would do instead.

---

## You can now

Add an MCP server and constrain it before trusting it, using both your own permission rules and the server's own configuration, and prove the constraint holds by testing it from the denied side. More specifically, you can recognise the case where a permission rule *cannot* express what you want, because the dangerous thing is an argument rather than a tool name, and you know to go looking one layer down instead of writing a rule that quietly does nothing.

That is the shape you will meet again: `oauth.scopes` under your tool rules for a remote server, `--caps` and origin lists under your rules for Playwright, `readonly` under your rules here. Your rules decide which tool runs. The server decides what that tool is capable of.

---

## Summary

You have added:

- **A new MCP server in `.mcp.json`**: registered at project scope next to the Playwright server, credentials kept out of the committed file.
- **A two-layer constraint**: an `ask` rule on SQL execution, plus the server's own `readonly` guardrail underneath it.
- **A boundary you tested both ways**: a read answered from the live database, and a write stopped twice, first by your rule putting a human in the loop, then by the server after that human said yes.

---

## What's next

In Section 8 I lock down the whole project with a permission and sandbox policy. Your solo challenge there is repo-global too: extend the policy with one more guardrail you can defend, and write the one-line answer to "is it safe to let Claude run this unattended on this repo?" backed by it.

---

## Reference

Your on-screen Playwright MCP setup from S7L7 is the reference for registration, scoping, rule syntax, and the trust-before-use flow. The new part here is the second layer: finding the constraint your permission rules cannot express, and configuring it where it actually lives.
