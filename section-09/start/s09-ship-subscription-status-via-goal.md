# S9 Solo - Hand Off a Subscriptions Feature via `/goal`

> Section 9, Autonomous and Remote Workflows. Solo. Same shared project. Hints and troubleshooting, not a full hand-hold.

## Mission

In the lecture I handed off a `category` feature on Expenses to a `/goal` loop and let the accumulated machinery run it: the skill scaffolding, the hooks formatting and testing, the reviewer checking, the permission rules holding it in bounds. Now you do the same on your Subscriptions area. Hand off a parallel feature as a `/goal` loop, run under supervision first, and let the loop stop itself when the feature is actually done.

The suggested feature is a `Status` on subscriptions (`Active` or `Cancelled`) that you can **edit and filter by in the browser**, backed by integration tests. Same entity-to-UI spread as the guided `category` feature, stated as a capability rather than a checklist of layers.

By the end, the loop stops on its own because your completion condition is met, and your `convention-reviewer` and hooks pass the result.

**Learning Objectives**

- Write a `/goal` completion condition as a user-facing capability rather than a list of files, so the loop cannot satisfy it layer by layer without the feature working, and phrase it so the evaluator can judge it from the conversation rather than from your filesystem.
- Supervise an autonomous loop and intervene at the right moments, not constantly.
- Run a feature through your own accumulated machinery (reviewer, hooks, rules) end to end.

---

## Prerequisites

- S9L6 finished. You watched the `category` handoff run to completion on Expenses, and you have a `/goal` condition you understand. That handoff is not committed to the `end/` snapshot, so the lecture is your reference for it, not the repo.
- **You are not in plan mode.** This repo ships `"defaultMode": "plan"` in `.claude/settings.json`, so a fresh session starts where Claude cannot write a single file. A loop launched there re-plans every turn and ships nothing. Shift+Tab into accept-edits first. This is the one prerequisite that silently costs you the whole run.
- Your Subscriptions area exists from Section 4. Check for `app/api/Api/Features/Subscriptions/` and `app/tests/Api.Tests/Subscriptions/`. If either is missing, do the Section 4 solo first. This exercise adds a field to a slice that has to already be there.
- Your `convention-reviewer` from Section 6 works. Type `@` and look for `convention-reviewer` in the list. If it is not there, rebuild it before you start, because Step 3 is where the loop's output gets checked and a missing reviewer turns that step into "it said done, so it is done."
- The safety-net hooks are active. Open `.claude/settings.json` and confirm two things: a `PostToolUse` hook on `Edit|Write` that formats, and a `Stop` hook that runs `dotnet test`. Docker has to be up for that Stop hook to pass, so `docker compose up -d` before you launch anything. With Docker down, every turn of the loop ends on a hook failure that has nothing to do with your feature, and you will spend the run debugging the wrong thing.
- **The app is running before you launch.** The condition is a browser capability, so the loop needs something to drive: `dotnet run --project app/api/Api` on 5033 and `pnpm --dir app/web dev` on 5173, both up in their own terminals. A loop that has to boot servers mid-run burns turns on process management instead of on the feature.
- **There are subscriptions in the dev database.** A fresh checkout has none, and `GET /api/subscriptions` answers `[]`. The condition asks the loop to change an existing subscription's status and to filter a list down to the matching rows, and neither can be demonstrated against an empty page. Create two or three first, through the form on `http://localhost:5173/subscriptions` or with a `curl` like the one below, so the loop spends its turns on the feature instead of on fixtures.

  ```bash
  curl -s -X POST http://localhost:5033/api/subscriptions \
    -H 'Content-Type: application/json' \
    -d '{"vendor":"Netflix","amount":9.99,"currency":"EUR","billingCycle":"Monthly","renewalDate":"2026-08-15"}'
  ```
- The Playwright MCP server is connected. `.mcp.json` ships it and `.claude/settings.json` already allows `browser_navigate`, `browser_snapshot`, `browser_click`, `browser_type` and `browser_select_option`, so the loop can drive the page without a permission prompt on every click. Type `/mcp` and confirm `playwright` is listed. Without it the browser half of your condition can never be demonstrated, and the loop runs to the turn ceiling.
- `/goal` is available in the session. If the command is missing, see the end of "How `/goal` actually works" below for the two things it depends on.
- Migrations are generated, never typed. `.claude/settings.json` denies `Edit(./app/api/Api/Migrations/**)`, so the loop has to reach the column through `dotnet ef migrations add`. Expect it to ask before running that, and read the file it produces.

---

## What we're solving

Everything you have built so far was one component at a time under your hand. Autonomy is where they run together without you driving each step. A `/goal` loop keeps going, turn after turn, until a condition you wrote evaluates true. That makes the condition the most important thing in the exercise.

A vague goal ("add a status field") never clearly finishes, so the loop either stops too early or wanders. But a merely *itemised* goal ("the field exists on the entity, in a migration, in the API, in React, and in a test") has the opposite failure: every clause can be true while the feature does nothing. A column lands, a dropdown renders, a test asserts on a list of one, and the loop declares done on a page where changing the status silently does nothing.

So state it functionally, as something a person can do: **you can open the Subscriptions page, change a subscription's status between Active and Cancelled, filter the list by status, and there are integration tests covering both.** That is one sentence, it has an unambiguous end state, and it cannot be satisfied a layer at a time, because the only way to demonstrate it is to actually do it.

The feature is deliberately small and shaped like the guided one, because the point is not the feature. The point is feeling what it is like to define "done" precisely enough that a loop can recognise it, then supervising a run rather than typing every edit. Your machinery does the quality enforcement: the hooks format and test each change, the reviewer checks the diff, your permission rules keep it in bounds. You watch, and you step in only when the loop needs a nudge.

Running it under supervision first is not a training-wheels caveat. It is how you learn where the loop tends to drift, so you know what to constrain before you would ever run one unwatched.

---

## The feature shape

The capability is: **edit and filter subscriptions by status in the browser.** That is what you write into the condition. The layers below are not the goal, they are what the capability turns out to need, and they are here so you can tell whether the loop is on track:

| Layer | What the capability needs |
|-------|---------------------------|
| Entity | `Status` on `Subscription` (an enum: `Active` or `Cancelled`) |
| Database | a migration that adds the column |
| API | create and list handle `Status`; list filters by it; **an update endpoint changes it** on an existing subscription |
| Web | the Subscriptions page shows status, lets you change it on a row, and filters the list by it |
| Test | integration tests prove the update and prove the filter in both directions, named snake_case `Should_When` |

Two of those are where this stops being a column. Filtering means `GET /api/subscriptions?status=active` (or your chosen shape) returns only the matching rows. Editing means there is no update endpoint on this slice yet: `SubscriptionEndpoints.cs` maps `POST`, `GET` and `DELETE` and nothing else, so "edit the status" is a new route the loop has to add, not a field it can bolt onto an existing one. Expect it to be the layer that takes the most turns.

---

## How `/goal` actually works

Read this before you write the condition. It is the part that decides whether your loop can recognise its own finish line.

After every turn a small fast model reads the conversation and decides whether your condition is met. It does not run commands and it does not read your files. That single fact drives everything below. "The migration file exists" gives it nothing to go on. "Run `dotnet test` and show the suite green" does, because the result lands in the transcript where it can see it.

So a condition is four parts, and each does a different job:

| Part | Filled in for this feature | What it decides |
|------|----------------------------|-----------------|
| End state | "I can edit a subscription's status between Active and Cancelled and filter the list by it, in the browser" | what "done" means, as a capability rather than a file list |
| Stated check | "drive it in the browser with Playwright and show the list changing, and run `dotnet test` green" | **whether the evaluator can see done.** The only part it actually reads |
| Constraint | "no existing test weakened, no Expenses, Companies or MonthlyBudget code touched" | what must stay true the whole way, not just at the end |
| Turn ceiling | "or stop after 30 turns" | the floor under a run that is going nowhere |

The two to think hardest about are the stated check and the constraint. The stated check is the one people leave out, and leaving it out is exactly what produces a loop that will not stop. The constraint is the one that catches drift, and without it nothing keeps the loop out of Expenses.

A functional end state raises the bar on the stated check, and that is the point. `dotnet test` alone cannot prove "in the browser", so the check has to name the browser too: navigate to the page, change a row's status, filter, and show the snapshot. Playwright is what puts that evidence in the transcript. Ask for a snapshot or a screenshot after the interaction, not just the click, because a click with no observed result reads as done to the evaluator and proves nothing to you.

Write the constraint as the areas that stay untouched, not as the files you are allowed to edit. "Nothing outside the Subscriptions files" sounds tighter and is actually broken: a migration always writes `Migrations/` and `ExpenseDbContextModelSnapshot.cs`, and the entity's EF configuration lives in `ExpenseDbContext` next to `BillingCycle`. A condition that forbids the only way to satisfy its own migration clause leaves the loop stuck between two of its own rules. Name Expenses, Companies and MonthlyBudget as off limits instead. Those are the neighbours drift would actually damage.

The invocation is one line, the four parts in order:

```
/goal <capability a person can perform>, verified by <checks whose output lands in this conversation>, without <constraint>, or stop after 30 turns
```

The session surface around it is small:

| Command | What it does |
|---------|--------------|
| `/goal <condition>` | sets the condition and starts the loop |
| `/goal` | prints the status: the condition, the turns spent, and the evaluator's latest reason for saying not yet |
| `/goal clear` | stops the loop |

There is one goal per session. Setting a second condition replaces the first rather than adding to it.

One consequence of it being a Stop hook is worth knowing before you launch: this repo already has a Stop hook running `dotnet test`. The two stack. Every turn of your loop pays a full Testcontainers run, so turns are slow, and the suite's result lands in the transcript whether Claude thinks to show it or not. That is half of your stated check being satisfied for free, and it is why a missing migration cannot hide here: the test factory calls `MigrateAsync()`, so a column with no migration turns the suite red on its own. The browser half is the half you have to ask for explicitly, because nothing in the repo produces it on its own.

And one thing `/goal` does **not** do: it does not change permissions. It removes the per-turn prompt, not the per-tool one, so Claude still stops and asks before anything your rules have not allowed. That is what makes a supervised run supervised. `/goal` itself runs as a session-scoped Stop hook, which is why it needs two things to exist at all: the workspace trust dialog accepted in this project, and hooks switched on. A `disableAllHooks` anywhere in your settings chain takes `/goal` away with them.

---

## If you're comfortable, do this

Four steps. Skip the rest if it works on the first try.

1. Start Postgres, the API and the web dev server, then write a `/goal` condition in the four parts from the table above, stated as a capability. Something like:

   ```
   /goal I can open the subscriptions page, change an existing subscription's status between Active and Cancelled and see it stick after a reload, and filter the list by status so only matching rows show while clearing the filter brings them all back, with integration tests covering the status update and the filter in both directions, verified in this conversation by a Playwright walkthrough that snapshots the list after each action and by a green `dotnet test`, without weakening or deleting any existing test and without touching Expenses, Companies or MonthlyBudget code, or stop after 30 turns
   ```

   Read it before you paste it. If you copy it unchanged you inherit my end state instead of writing one, and writing one is the exercise.

2. Launch the loop and supervise it. Let the hooks format and test each change, and keep your own attention on one question: is it still heading toward the capability. Type `/goal` on its own at any point to see where it stands.
3. When it stops, review the result. Run your `convention-reviewer` over the diff, confirm the hooks are green, and drive the page yourself once.
4. Accept or refine. If the loop declared done but you cannot change a status in the browser, your condition was not measurable enough. Tighten it and rerun.

**Done when**: the loop stops on its own because the completion condition is met, you can edit and filter by status in the browser yourself, and your `convention-reviewer` and hooks pass the result. *(Optional stretch: write a `/goal` condition for a feature on a repo of your own.)*

---

## Full path

Only if the fast path did not just work.

### Step 1: Write the completion condition

Model it on the category condition from the lecture. Filled in for this feature, the four parts are:

- **End state:** on the Subscriptions page, I can change an existing subscription's status between `Active` and `Cancelled` and see the change stick after a reload, and I can filter the list by status so only matching rows show and clearing the filter brings them all back. Integration tests cover both: one that updates a status and reads it back, one that asserts the filter returns only matching rows *and* that the unfiltered list still returns everything.
- **Stated check:** two things visible in the session. A Playwright walkthrough that navigates to the page, performs each action, and snapshots the list after it. And a `dotnet test` run whose green result appears in the transcript.
- **Constraint:** no existing test is weakened or deleted, and no Expenses, Companies or MonthlyBudget code is touched. The Subscriptions slice, `ExpenseDbContext` and generated migrations are in bounds; those last two are where the column has to be configured and created.
- **Turn ceiling:** `or stop after 30 turns`. Claude reports progress against it each turn.

Put together, those four parts are the one-line condition in the fast path above.

Notice what the functional phrasing buys you. "See the change stick after a reload" rules out a dropdown wired to React state and nothing else. "Clearing the filter brings them all back" rules out a filter that quietly returns an empty list. Neither clause names a file, and both are things the loop has to perform rather than claim.

Then check the whole thing against the harder bar: could the evaluator confirm it from the transcript alone, without opening a single file? If a clause fails that test, rewrite it as something Claude has to demonstrate.

### Step 2: Launch and supervise

Start the loop against your condition. Then watch, do not type. The hooks will format and run tests after each change, so you will see the loop self-correct when a test fails. Your job during the run is narrow: notice if it drifts out of the Subscriptions area, starts editing Expenses, or declares progress on a layer it has not actually finished. Intervene on those, and only those.

One thing to watch for specifically with a browser-verified condition: the loop will reach the browser step before the dev database has the new column, and the page will error. That is not drift, it is the loop discovering it has to run `dotnet ef database update` and restart the API. Give it a turn or two to work that out before you step in.

When you cannot tell drift from slow progress, ask:

```
/goal
```

The evaluator's latest reason is the tell. "No test output in the conversation yet" is slow progress, so leave it alone. "Modified `ExpenseEndpoints.cs`" is drift, and drift is what you stop the loop for.

Leave the permission mode alone while you do this. Auto mode is the switch you would reach for to run a loop unattended, and this run is not that. Leave it for when you trust your condition.

### Step 3: Review what came back

When the loop stops, do not trust "it said done." Run the reviewer:

```
Use the convention-reviewer to review the Status feature changes.
```

Confirm it passes, and confirm the hooks were green on the final change. Read what it flags before you act on it: the reviewer reads whole files, not just your diff, so a violation it reports may predate your change.

Then work the page yourself. The loop's own Playwright run is evidence, not proof, and there is a difference between a snapshot that shows the right rows and a feature that behaves. If the API is not already restarted on the new schema, do that first, because the test factory migrates a throwaway Testcontainers database while your dev database may still have the old one:

```bash
dotnet ef database update --project app/api/Api
dotnet run --project app/api/Api
```

Open `http://localhost:5173/subscriptions` and do three things by hand:

1. Change a subscription from `Active` to `Cancelled`, then reload the page. If it flips back, the edit never reached the API.
2. Filter to `Cancelled`, then to `Active`, then clear the filter. The cleared list has to show everything again.
3. Change a status while a filter is applied. A row that no longer matches should leave the list. This is the one that catches a filter applied once on mount and never re-run.

Then cross-check the API underneath it. Create one active and one cancelled subscription, and hit the endpoint every way:

```bash
curl -s "http://localhost:5033/api/subscriptions"
curl -s "http://localhost:5033/api/subscriptions?status=Active"
curl -s "http://localhost:5033/api/subscriptions?status=active"
```

The filtered calls should return the active row and only the active row. Run the unfiltered one too, and check it still returns both. That call is what catches a filter that quietly drops everything, because a test asserting only on the filtered result passes happily against an empty list.

The lower-case call is the third check, and what it proves depends on how the loop bound the parameter. Bound straight to a `SubscriptionStatus?`, an unknown or badly-cased value is the framework's problem and you get a bare 400 with no message. Bound to a `string?` and parsed with `Enum.TryParse(..., ignoreCase: true)`, casing stops mattering and a bad value returns the slice's usual `ValidationProblem`. The second is what `.claude/rules/backend.md` asks for, so if `?status=Paused` comes back without a readable error, that is a real finding, not a nitpick.

### Step 4: Accept or refine the condition

If the loop stopped but the feature is not really done (the status resets on reload, the filter returns everything, the edit control is read-only), that is a condition problem, not a model problem. The loop met the letter of a condition that was too loose. Add the missing clause as an action rather than an artefact, and rerun. Learning to write the condition tightly is the actual skill here.

The pattern is always the same. Every gap you find has a sentence that would have closed it, and that sentence describes something a person does, not something a file contains. "The status survives a reload" instead of "the update endpoint exists". Write the next one that way from the start.

---

## Troubleshooting

### The loop plans and re-plans but never edits a file

You are in plan mode. This repo sets `"defaultMode": "plan"`, and a goal does not change permissions, so the loop inherits it and burns turns producing plans it is not allowed to execute. Shift+Tab into accept-edits and relaunch.

### The new column breaks rows that already existed

Adding a non-nullable enum column generates `defaultValue: ""`, and no `SubscriptionStatus` member matches an empty string, so every pre-existing row throws on read. Tests miss it because their database starts empty. You cannot patch the migration either, since `Edit(Migrations/**)` is denied. Fix it where it belongs, in `ExpenseDbContext`, with `.HasDefaultValue(SubscriptionStatus.Active)` on the property, then `dotnet ef migrations remove` and add it again. Read the regenerated file and check the default says `"Active"`.

### The build breaks at call sites you did not touch

`CreateSubscriptionRequest` is a positional record, so a new required parameter breaks every existing construction of it, including in tests. A defaulted parameter (`SubscriptionStatus Status = SubscriptionStatus.Active`) keeps them compiling and gives the API a sane default. That is not weakening a test, it is not making one carry a value it has no opinion about.

### The update endpoint returns 404 from the page but works in tests

The route the web app calls and the route the API maps have drifted apart, which is easy when the loop writes both in the same turn. Read the request in `browser_network_requests` and compare it to the `MapPut`/`MapPatch` line. Whichever shape it settled on, the slice's rules still apply: an unknown id is a `404`, an invalid status value is a `ValidationProblem` with a field-to-messages dictionary, not a bare `400`.

### The browser step fails: a 500, a missing status column, or no page at all

Both causes are environmental, and the loop cannot tell either one apart from a broken feature. A 500 or a missing column means the dev database has not been migrated: the Testcontainers suite went green on a throwaway database while `localhost:5432` still has the old schema, so run `dotnet ef database update --project app/api/Api` and restart the API. Vite picks up the web changes on its own, so only the API needs the restart. A connection error from `browser_navigate` means the app is not running, and the loop tends to respond by rechecking its own code rather than by starting the server. Confirm both processes are alive, then nudge it with the actual error. This is what the "app is running before you launch" prerequisite exists to prevent.

### It clicks things but never shows the result

`browser_click` on its own proves nothing to you or to the evaluator, and a loop that only clicks can loop forever against a condition it has already satisfied. Your condition has to ask for the observation, not just the action: "snapshot the list after changing the status". If you left that out, stop the loop and put it in.

### The status changes on screen but resets on reload

The dropdown is wired to local React state and the update never reaches the API, or it reaches it and the response is ignored. This is the single most common way a status feature looks done and is not, which is why "survives a reload" belongs in the condition rather than in your head.

### The loop never stops

Almost always a clause the evaluator cannot see. Read your condition back and find the one that points at your filesystem rather than at the transcript. Rewrite it as something Claude demonstrates with a command, and drop any clause that cannot be demonstrated at all. Then add a turn ceiling, so the next bad condition costs you thirty turns instead of an afternoon.

### It stops and asks permission on every other command

Working as designed, and for a supervised run it is the point. If you want it to run without you, switch into auto mode, where the classifier clears routine calls while your deny and ask rules still block. Do not reach for `--dangerously-skip-permissions`.

### `/goal` is not available

Check the two things it depends on, in that order: the workspace trust dialog accepted in this project, then hooks switched on. A `disableAllHooks` in any file in your settings chain is the usual culprit, and it is easy to miss because it is often set in a file you did not edit today.

### It edits Expenses instead of Subscriptions

The disjointness constraint is not in the condition, or not being honoured. State explicitly that Expenses, Companies and MonthlyBudget stay untouched. If it still strays, stop the loop and add the constraint before rerunning. This is exactly the kind of drift supervision exists to catch. Do not phrase it as "only Subscriptions files", or you forbid the `ExpenseDbContext` and migration edits the feature genuinely needs.

### The API will not start, or `curl` returns yesterday's shape

Something is already listening on 5033, usually a dev server you left running, and it is running the code from before the loop. Stop it, or start a second copy with `dotnet run --project app/api/Api --urls http://localhost:5034`. Use the `--urls` flag, not `ASPNETCORE_URLS`: `launchSettings.json` sets `applicationUrl`, which overrides the environment variable, so the env var silently gets you a port collision on 5033 instead of the port you asked for.

### The reviewer flags the result

Good, that is the machinery working. Fix what it flags and rerun the relevant part. A loop that produces reviewer-clean code on the first try is a bonus, not the expectation.

Enums on the wire are the classic flag on a feature like this, but this repo has already paid that bill: `Program.cs` registers `JsonStringEnumConverter`, which is why `BillingCycle` comes back as `"Monthly"` and not `1`. Your new `Status` inherits it for free on the API side. What is still yours to get right is the other end of that wire: the TypeScript union has to spell the members exactly as C# does, and the integration test has to read a string back into the enum rather than an int. `.claude/skills/new-endpoint/references/enums.md` is the write-up if either misbehaves.

Also check what the reviewer flags is actually yours, because it reads whole files and will report violations that were already in the baseline.

### Tests go red mid-loop and stay red

The hooks are surfacing a real break the loop is not recovering from. Pause, read the failure, and either nudge the loop with the specific error or fix it yourself and let the loop continue from green.

---

## You can now

Define "done" as a capability rather than a checklist, precisely enough that an autonomous loop can recognise it and cannot fake it a layer at a time, and phrase it so the thing doing the recognising can actually see it, then hand a real feature off and supervise rather than drive. You have run a feature through your own stack, the reviewer and hooks you built earlier, and seen them enforce quality on code you did not type. That is the payoff of building the machinery: it works on autonomous output, not just on yours.

---

## Summary

You have added:

- **An editable, filterable `Status` on Subscriptions**: enum field, migration, API create/list/update with filtering, a React page you can change status on and filter, and passing integration tests for both, all inside the Subscriptions files.
- **A functional `/goal` condition**: a capability as the end state, a browser walkthrough and a green suite as the checks that prove it in the conversation, a constraint, and a turn ceiling, concrete enough for the loop to stop itself on.
- **A supervised autonomous run**: reviewed by your `convention-reviewer` and passed by your hooks.

---

## What's next

In Section 10 I build a PR-review pipeline with GitHub Actions and run it on an Expenses PR. Your solo challenge stays on the two-area track: extend the review workflow to also run the tests and fail the check when they break, then exercise it by opening a Subscriptions PR that breaks one on purpose.

---

## Reference

Your on-screen `category` handoff from S9L6 is the reference for the shape of the condition and the supervision rhythm. Your Section 4 Subscriptions slice and Section 6 reviewer are what the loop builds on and gets checked by.
