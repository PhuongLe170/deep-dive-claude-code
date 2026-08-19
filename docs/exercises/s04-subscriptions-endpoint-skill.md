# S4 Solo - Build a `new-endpoint` Skill and Scaffold the Subscriptions Slice, API and Web

> Section 4, Custom Skills. Solo. Same shared project. Hints and troubleshooting, not a full hand-hold.

## Mission

Build a `new-endpoint` Agent Skill of your own, then run it to scaffold the first slice of the **Subscriptions** area, its Minimal API endpoint group, a Testcontainers test, and the React page that puts it on screen, and harden the skill against whatever breaks along the way.

Subscriptions is your parallel track for the rest of the course. It is the company's recurring software spend (think the GitHub, Figma, and Slack bills that renew every month). It mirrors Expenses (create, list, delete, multi-currency) but it has fields Expenses does not, a billing cycle and a renewal date, and it lives in its own files, so it never collides with the Expenses code I build on screen. From here on, when I build something on Expenses, you build the equivalent on Subscriptions.

By the end, `POST /api/subscriptions` and `GET /api/subscriptions` work against the database, a green Testcontainers test proves it, the Subscriptions tab in the browser lists and creates real rows, and you own a `new-endpoint` skill that is not welded to the entity it was written from.

**Learning Objectives**

- Author an Agent Skill that takes an argument and derives everything from it.
- Drive a skill you authored on a surface it was not first written against.
- Find and close the gaps a scaffolder has when its conventions were inferred from a single example.
- Extend a skill across a stack boundary, where the naming rules and the conventions both change.
- Establish the Subscriptions area as your disjoint solo track, API and web.

---

## Prerequisites

- You are working inside the course project, whatever you named the folder you cloned it into. Every path below is relative to that project root.
- The app boots and the existing tests pass from a clean state (`dotnet test`).
- The Subscriptions stubs are present: `app/api/Api/Features/Subscriptions/`, `app/tests/Api.Tests/Subscriptions/`, and `app/web/src/pages/SubscriptionsPage.tsx`.
- The web app installs and runs: `pnpm --dir app/web install`, then `pnpm --dir app/web dev`.

---

## What we're solving

A scaffolding skill is only as good as the conventions it learned. Yours will learn them from the Expenses code, the only complete slice in the repo, so it will quietly hardcode assumptions: the entity name, the exact DTO fields, the route prefix, maybe the test's arrange block. Run it on a second entity and those assumptions surface as bugs. That is the point of this exercise. You do not really know your skill works until it has built something other than the thing you wrote it for.

Subscriptions is a deliberately good second surface because it is *almost* the same shape as Expenses but not quite. It is a money record with an amount and a currency, so most of the pattern carries. But a subscription also has a **billing cycle** (monthly or annual) and a **renewal date**, and it has no notion of a one-off transaction date the way an expense does. A skill that hardcoded the Expense DTO will generate a Subscription that is missing those fields, or worse, carries Expense-only fields that make no sense here. Finding and fixing that is the real work.

How likely you are to actually *see* that failure depends on how your skill learns an entity's fields. A skill that bakes in a "read `<Res>.cs`" instruction will probably come out right on the first run; one that learned its DTO shape from Expenses alone will not, and that is where the lesson bites hardest. Either way the question is the same, and Step 2 is where you answer it: does the `SKILL.md` *guarantee* the entity gets read, or did the model just happen to?

Then you push the same skill across the stack boundary into the React app, where the conventions are different, the naming forms multiply, and the two tracks are forced to share a file. Assumptions that survived the API half rarely survive that.

The most important part of this exercise is not the Subscriptions feature. It is what the skill gets wrong on its first run, and what you change in the `SKILL.md` so it gets it right.

---

## The Subscription shape

The project already ships the intended entity as a stub at `app/api/Api/Features/Subscriptions/Subscription.cs`. It is deliberately not wired into `ExpenseDbContext`, any endpoint, or a migration. Decide the DTO and route shape before you run anything, so you can tell whether the skill got it right:

| Field | Type | Note |
|-------|------|------|
| `Id` | Guid | same as Expenses |
| `Vendor` | string | e.g. "GitHub", "Figma" |
| `Amount` | decimal | per billing cycle |
| `Currency` | string (ISO) | reuse the Expenses currency handling |
| `BillingCycle` | enum | `Monthly` or `Annual`, new vs Expenses |
| `RenewalDate` | date | next renewal, new vs Expenses |

Note what is *not* in that table. `Expense` has a `CreatedAt`, and `Subscription` deliberately does not. If your generated DTO has one, the skill copied it from the Expense response instead of reading the entity. That is the same defect as a missing `BillingCycle`, just pointing the other way.

"Reuse the Expenses currency handling" means calling the existing `Currencies` helper from the Expenses slice, not duplicating it. That is a small, deliberate Subscriptions to Expenses reference, and it is fine here.

Routes: `POST /api/subscriptions`, `GET /api/subscriptions`, `DELETE /api/subscriptions/{id:guid}`.

One thing to know before you get to the web half: by default `System.Text.Json` writes a C# enum as a **number**. A created subscription comes back as

```json
{ "id": "...", "vendor": "GitHub", "amount": 21, "currency": "EUR", "billingCycle": 1, "renewalDate": "2026-08-01" }
```

`"billingCycle": 1`, not `"Annual"`. Storing the enum as a string in the database does not change this, because that setting is EF Core's, not the serializer's. You will decide what to do about it in step 5.

---

## The skill you're building

This exercise is a Custom Skills exercise. The thing you build, and then the thing under test, is a `new-endpoint` **Agent Skill**, invoked as `/new-endpoint Subscription`. It scaffolds a vertical slice, the contracts, the endpoints, and a Testcontainers test, following the `Features/Expenses` slice as its pattern.

Create it at `.claude/skills/new-endpoint/SKILL.md` in the project, folder-local, not in your global `~/.claude`. You build it in six moves in **Step 0 of the Full path below**, starting from the frontmatter and adding one section at a time. That step is the followable spine, with the skeleton you are aiming for. This section and the two that follow it are the *why* behind those moves: the argument-substitution rule, the requirements, and the web half. Read them once here, then again when Step 0 tells you to write each piece. (The frontmatter you write carries these fields: `name`, `description`, `argument-hint`, `arguments: [resource]`, `model`, `allowed-tools`.) The one field to think hardest about is `model`: a scaffolder reads an entity, mirrors a multi-file slice, derives English plurals, and handles the enum trap, so it is not the place for a small model. Give it a capable model (`claude-sonnet-5`) or you invite exactly the flaky first runs Steps 1 through 3 then teach you to blame on the skill.

### How arguments actually substitute

Read this before you write a line. It is the thing most likely to waste your afternoon.

You declare `arguments: [resource]` in the frontmatter. In the body, **`$resource` is substituted only when the name ends there.** The parser reads the longest run of word characters after the `$` and looks that whole run up as an argument. If the run is not exactly `resource`, nothing is substituted and the text passes through untouched:

| You write | The model receives | Why |
|-----------|--------------------|-----|
| `$resource` | `Subscription` | the name ends at a space |
| `` `$resource` `` | `` `Subscription` `` | the name ends at a backtick |
| `$resource.cs` | `Subscription.cs` | the name ends at a dot |
| `$resources` | `$resources` | reads as an argument named `resources` |
| `$resourceContracts.cs` | `$resourceContracts.cs` | reads as `resourceContracts` |
| `Create$resourceRequest` | `Create$resourceRequest` | reads as `resourceRequest` |
| `Map$resourceEndpoints` | `Map$resourceEndpoints` | reads as `resourceEndpoints` |
| `${resource}` | `${resource}` | braces are not argument syntax |
| `$0` | `Subscription` | positional forms resolve too; `$0` is the first argument, the same value as `$resource` |

Three consequences worth internalising:

- **Positional forms do resolve** (`$0` is the first argument, `$1` the second, `$ARGUMENTS[N]` the same by index). `$0` here is exactly `$resource`. Named arguments read clearer in a scaffolder full of type names, so prefer `$resource`.
- **You cannot glue anything onto the end of `$resource`.** This is the one that bites hardest in a scaffolder, because a scaffolder is mostly type names and file names. `$resourceEndpoints.cs`, `Create$resourceRequest`, and `Map$resourceEndpoints` all reach the model as literal text with a dollar sign in them. Only a following space, punctuation, or backtick ends the name safely.
- **So there is also no pluralisation idiom.** `$resources` is just the same rule seen from another angle. If your skill needs `Subscriptions` or `subscriptions`, you cannot splice it inline either.

The fix for all of these is the same: substitute once, then derive. Define the forms in prose near the top of the body, then use placeholders of your own everywhere else. For example:

> `<Res>` is `$resource`, singular PascalCase. `<Ress>` is `<Res>` pluralised by English rules, not a bare `+ s`: `-y → -ies` (Category → Categories), `-s`/`-x`/`-ch`/`-sh` → `+ es`, otherwise `+ s`. `<ress>` is `<Ress>` lower-cased.

Then write `<Res>Endpoints.cs`, `Create<Res>Request`, and `/api/<ress>` for the rest of the file. The model expands them, and there is nothing left for the argument parser to get wrong.

Braces are not a workaround. `${CLAUDE_SKILL_DIR}` expands because it is an environment variable, not an argument.

If an unsubstituted placeholder shows up in a generated file path or type name, this is why.

### The requirements

At minimum, your skill must:

- Take the resource name as an argument (`Subscription`), not a hardcoded entity.
- **Stay entity-agnostic in its own body.** The trap the guide keeps naming is leftover *`Expense`* literals, the surface you copied from. But the *target* is a trap too: a `SKILL.md` littered with `Subscription`, `Vendor`, `BillingCycle` reads like a scaffolder yet is really a one-shot template for this one entity, and it will fail the moment you point it at anything else. The word `Subscription` should not appear in the skill at all, not even in an example. Carry the logic in placeholders (`<Res>`, `<Ress>`, `<ress>`) and, where you want a worked example, use a neutral throwaway entity (a `Product`, a `Category`) that is not the thing you are about to generate.
- Derive everything from that argument: the folder, the file names, the test path, and the route. For `Subscription` that means `app/api/Api/Features/Subscriptions/`, `SubscriptionContracts.cs`, `SubscriptionEndpoints.cs`, `app/tests/Api.Tests/Subscriptions/`, and `/api/subscriptions`.
- Generate `record` DTOs with explicit `ToEntity()` / `ToResponse()` mapping, a static `MapSubscriptionEndpoints` group with private handlers, and a matching Testcontainers test, all in the style of the Expenses slice.
- Learn the entity's real fields by reading the entity file `Features/Subscriptions/Subscription.cs`, rather than copying the Expense fields. This is what lets it emit `Vendor`, `BillingCycle`, and `RenewalDate` instead of `Description` and `Date`.

A first version will not get all of that right. That is expected, and hardening it is Step 3.

---

## The web half

An endpoint nobody can reach is half a feature. The app already has a Subscriptions tab: `App.tsx` routes `/subscriptions` to `app/web/src/pages/SubscriptionsPage.tsx`, which today is a "coming soon" placeholder. Your job is to make that page real, the same way `ExpensesPage` is real.

The Expenses web slice is the pattern:

| File | What it does |
|------|--------------|
| `lib/api.ts` | typed `interface`s mirroring the C# DTOs, plus one `fetch` function per route |
| `components/ExpenseForm.tsx` | `useMutation`, local validation, invalidates the query on success |
| `components/ExpenseTable.tsx` | `useQuery` to list, `useMutation` to delete, loading and error and empty states |
| `pages/ExpensesPage.tsx` | composes the components, no data fetching of its own |

So the Subscriptions equivalent is `SubscriptionForm.tsx`, `SubscriptionTable.tsx`, three new functions in `lib/api.ts`, one per route, and a `SubscriptionsPage` that composes them. There is no monthly-totals equivalent, so leave that out rather than inventing one.

`.claude/rules/frontend.md` is binding here. The two rules that catch people: never call raw `fetch` from a component, it goes in `lib/api.ts` and reaches the component through TanStack Query; and pnpm only, never npm.

### Why this is the interesting half for a skill

Extending `new-endpoint` across the stack boundary breaks the naming scheme you just fixed. The API half needed `<Res>`, `<Ress>`, and `<ress>`. The web half needs a **camelCase** form too, because JSON properties, query keys, and api function names are not PascalCase: `subscriptions` as a query key, `createSubscription` as a function, `billingCycle` as a field. A skill whose derivation section stops at three forms will produce `CreateSubscription` in TypeScript and a query key that does not match the one the mutation invalidates.

The second trap is the one flagged in the shape section: the API sends `billingCycle` as a **number**. If your skill mirrors the C# `record` field-for-field into a TypeScript `interface`, it will write `billingCycle: BillingCycle` with a string union and the table will render nothing useful. You have two honest fixes, and the skill should state which one it assumes:

- Add `JsonStringEnumConverter` on the API so enums go over the wire as `"Annual"`, and type the TS side as a string union. The DTO and the interface then genuinely match, which is what the frontend rule asks for. **This one has a cost on the API side, and it is not obvious: it turns your green Subscriptions test red.** See step 5.
- Leave the number and type the TS side as a numeric enum or union, mapping to labels for display.

Either works. Picking one silently is what fails.

---

## If you're comfortable, do this

Seven steps. Skip the rest if it works on the first try.

0. Write `.claude/skills/new-endpoint/SKILL.md`, using the frontmatter skeleton in Step 0 and the `Features/Expenses` slice for the code conventions.
1. Run your skill for the Subscriptions resource, e.g. `/new-endpoint Subscription`. Let it scaffold the endpoint group under `app/api/Api/Features/Subscriptions/` and the matching Testcontainers test under `app/tests/Api.Tests/Subscriptions/`.
2. Read the output against the shape table above *before* you run anything. Look for what the skill copied from Expenses that does not belong (a leftover `Expense` type or field, the wrong route) and what it is missing (`BillingCycle`, `RenewalDate`).
3. Fix the skill, not just the generated files. Hardcoded `Expense` literals become the argument. A DTO that cannot express new fields means the skill's template is too rigid. Regenerate to confirm.
4. Register the new endpoints in `Program.cs`, add the `DbSet<Subscription>` and a migration, then run the new Subscriptions test. Green means the slice works end to end.
5. Extend the skill with a web half, then run it again. It should add the `lib/api.ts` functions and types, a `SubscriptionForm` and `SubscriptionTable`, and replace the `SubscriptionsPage` placeholder.
6. Boot both sides and use the page in the browser. Add a subscription, see it in the table, delete it. `pnpm --dir app/web typecheck` and `lint` clean.

**Done when**: `/new-endpoint Subscription` produces a compiling, passing Subscriptions slice on both sides, including `BillingCycle` and `RenewalDate`, with no manual `Expense`-to-`Subscription` edits left, the skill itself containing zero `Subscription` literals (grep it), `dotnet test` green, `pnpm --dir app/web typecheck` clean, the Subscriptions tab creating and listing real rows, **and you have read the generated diff yourself and confirmed the endpoints, the test, and the page are actually right, not just green.** A passing test on scaffolded code is necessary, not sufficient, and a page that renders is not the same as a page that is correct. Review it the way the Section 2 "Watching It Work" lecture showed.

---

## Full path

Only if the fast path did not just work.

### Step 0: Write the skill

This is the exercise. Everything after it just runs, reads, and hardens what you build here, so build it the way you would build the C# file it generates: start from the frontmatter skeleton below, then add one section at a time. Create `.claude/skills/new-endpoint/SKILL.md`.

Here is the shape you are aiming for. It is a spine, not a finished skill. The code templates for the contracts and endpoints you derive from the Expenses slice, not from this guide:

```markdown
---
name: new-endpoint
description: Use when adding a new API resource or endpoint to this project. Scaffolds contracts, endpoints, and a Testcontainers test, following the Features/Expenses slice.
argument-hint: <ResourceName>
arguments: [resource]
model: claude-sonnet-5 
allowed-tools: Read, Write, Edit, Bash
---

# new-endpoint

Scaffold a vertical slice for `$resource`, following `app/api/Api/Features/Expenses/` as the pattern.

## Name forms
<Res>  = `$resource`, singular PascalCase.
<Ress> = <Res> pluralised (-y → -ies, -s/-x/-ch/-sh → +es, otherwise +s).
<ress> = <Ress> lower-cased.
Substitute `$resource` once, here. Use the placeholders everywhere below.

## Before generating
Read Features/<Ress>/<Res>.cs and use its properties as the DTO fields. Do not carry Expense's fields over.

## Files to write (pattern: app/api/Api/Features/Expenses/)
- app/api/Api/Features/<Ress>/<Res>Contracts.cs       # record DTOs + ToEntity()/ToResponse()
- app/api/Api/Features/<Ress>/<Res>Endpoints.cs        # Map<Res>Endpoints: POST/GET/DELETE /api/<ress>
- app/tests/Api.Tests/<Ress>/<Res>EndpointsTests.cs   # Testcontainers, mirrors the Expenses test

## Load on demand
- entity has an enum property → read ${CLAUDE_SKILL_DIR}/references/enums.md
- generating the web side     → read ${CLAUDE_SKILL_DIR}/references/web-slice.md

## Finish
Confirm every file exists and the DTO fields match the entity that was read.
Emit the DbSet + migration + Program.cs wiring as one copy-paste block ending in:
    dotnet build && dotnet test --filter FullyQualifiedName~<Res>
For the web side, run `pnpm --dir app/web typecheck` and lint; fix red before reporting done.
```

Build it in six moves. Each one writes a piece of that skeleton, and each has a gotcha the skeleton alone does not tell you:

**0.1 Frontmatter.** Write the frontmatter from the skeleton above and adjust the fields. Three of them bite later if you get them wrong now. `description` is matched on *intent*, not output, so write when to reach for the skill ("Use when adding a new API resource...") rather than what it emits. A description that only names its output fires less reliably. `allowed-tools` must include `Bash` (the skill checks its own work in 0.5 and cannot run `pnpm typecheck` without it) and `Read` (your skill reads the entity file and the Expenses slice rather than carrying them inline, so a Write/Edit/Bash-only restriction would starve it). And `model`: do not reach for a small one. A skill that greps once and writes one file can run small; yours reads an entity, mirrors a multi-file slice, pluralises by English rules, and reasons about the enum trap. On too small a model the plural derivation and the read-the-entity step are the first things to slip, and you will mistake a model limitation for a skill bug in Step 2. Set `model: claude-sonnet-5`.

**0.2 The derivation block.** Write the name-forms block near the top, before any file it generates. It has to come first because everything below is spelled in `<Res>`, `<Ress>`, and `<ress>`, and those are defined here. The reason you cannot just write `$resources` or `Create$resourceRequest` inline is the argument-substitution rule in the "How arguments actually substitute" section above. Write the block now, and let that section explain why on the second read.

**0.3 The files-to-write list.** Name each file the skill produces and point at `app/api/Api/Features/Expenses/` as the pattern, rather than pasting the slice in. A skill is instructions, not a code dump. The model opens the Expenses slice and mirrors its shape, which is what keeps the body short and the conventions honest.

**0.4 Tell it how to learn the fields.** A skill that copies the Expense DTO emits `Description` and `Date`, never `Vendor` and `RenewalDate`. So it needs a way to learn the real fields of the entity it is scaffolding. The cleanest instruction is "read `Features/<Ress>/<Res>.cs` and use its properties." An argument that lists the fields, or an interactive prompt, are the other legitimate choices. Pick one and write it down. Steps 1 through 3 are where you find out whether your choice actually holds, or whether the model just happened to read the entity on its own.

**0.5 End with a check, not a hope.** After it writes the files, have the skill verify what it can and hand you the rest. It confirms every file exists and the DTO fields match the entity it read, so no `Expense`-only field leaked in and none of the entity's fields dropped. The API half it cannot build on its own by design, because the endpoints call `db.<Ress>`, which needs the `DbSet` you add by hand in Step 4. So rather than building and reporting the inevitable red, it emits the Step 4 wiring as one ordered, copy-pasteable block ending in `dotnet build && dotnet test --filter FullyQualifiedName~<Res>`. The web half it can verify itself, because nothing there waits on the manual wiring: it runs `pnpm --dir app/web typecheck` and `pnpm --dir app/web lint` and fixes anything red before it reports done. A green typecheck there is a real signal, not a deferred one.

**0.6 Split the references.** Keep the base body lean by pushing anything not needed on every run into a `references/` folder, loaded on demand through `${CLAUDE_SKILL_DIR}/references/`:

```
.claude/skills/new-endpoint/
├── SKILL.md              # always-on: name forms, files to write, read-the-entity, done-check
└── references/
    ├── enums.md          # the enum-over-the-wire trap; read only when the entity has an enum
    └── web-slice.md      # the web half; read when you generate the React side
```

A resource with no enum never pays for `enums.md`. An API-only run never loads `web-slice.md`. The base stays small and the model pulls in the rest only when the entity in front of it needs it. That is progressive disclosure, and it is why the skeleton's "Load on demand" section is two one-line pointers rather than the content itself. You flesh out `web-slice.md` in Step 5 and `enums.md` in the enum sections. For now the pointers are enough.

Confirm it registers before you use it. Type `/` and look for `new-endpoint` in the list. It should show up as soon as the file is saved, with no restart.

### Step 1: Run the skill against Subscriptions

```
/new-endpoint Subscription
```

Your skill should create the Subscriptions endpoint group and its test in their own files, disjoint from the Expenses code:

```
app/api/Api/Features/Subscriptions/
├── SubscriptionEndpoints.cs      # POST /api/subscriptions, GET, DELETE /{id:guid}
├── SubscriptionContracts.cs      # record DTOs + mapping, same style as Expenses
└── Subscription.cs               # already present as a stub; reuse it
app/tests/Api.Tests/Subscriptions/
└── SubscriptionEndpointsTests.cs # Testcontainers, same shape as the Expenses test
```

If the skill created files under an Expenses path, or overwrote an Expenses file, stop. That is the disjointness rule broken, and it is a skill bug (see Troubleshooting).

### Step 2: Read against the shape table

Open the generated files and check them against the Subscription shape above. Two kinds of defect:

**Carried over from Expenses (should be removed or renamed):**

- A DTO or handler still typed as `Expense` / `CreateExpenseRequest`.
- The route left as `/api/expenses`.
- An Expense-only field (the one-off `Date`, a description validation that does not fit) copied in blindly.
- The test's arrange block seeding an expense or asserting on an expense field.

**Missing because the skill never looked (the more interesting failure):**

The `Subscription.cs` entity stub already declares `BillingCycle` and `RenewalDate`, so the fields exist in the domain. The failure to watch for is a skill whose DTO template was learned from Expenses not carrying them into the generated contracts:

- No `BillingCycle` in `CreateSubscriptionRequest` / `SubscriptionResponse`, because the Expense DTO the template copied has no such field.
- No `RenewalDate`, for the same reason.

The second kind is the lesson. A scaffolder that only reproduces the one example it was built from stays blind to fields the new entity actually has, even when they are sitting right there in `Subscription.cs`. The fix is to make the skill read that entity file. Note every gap.

There is a real chance both fields came out fine, because `Subscription.cs` lives in the folder your skill is writing into and the model often opens it without being told to. Do not read that as a pass. Ask the sharper question instead: **does my `SKILL.md` actually instruct anything to read the entity, or did I get lucky?** If the instruction is not written down, the behaviour is not reproducible, and Step 3 still applies to you.

### Step 3: Fix the skill, then regenerate

This is the step that matters. For every defect, change the *skill*, not just the generated file:

- Replace hardcoded `Expense` occurrences in `SKILL.md` with `$resource`, or with your own placeholder for a derived form. There is no `$resources`, so state the plural and route-segment rule once in prose and use a placeholder like `<Ress>` everywhere else (see "How arguments actually substitute" above).
- If the skill embeds a whole example DTO or test, make the entity name, fields, seed data, and assertions parameterised, not copied. Decide how the skill should learn an entity's fields: an argument, a prompt to the user, or a convention it reads from an existing model file. The `Subscription.cs` stub is right there to read.
- For the fields Expenses never had: the honest fix is that a from-one-example skill cannot invent `BillingCycle` on its own. Give the skill a way to take the field list (an argument or an interactive step), or point it at the entity file so it reads the real fields, or scope its description so it is clear it scaffolds the *plumbing* and you fill the domain fields. Each is a legitimate design choice. Pick one and make the skill state it.

Then delete the generated Subscriptions files and run `/new-endpoint Subscription` again. A clean second run that produces the right shape is the proof your skill is now surface-independent.

### Step 4: Wire it in and prove the slice

The Expenses endpoints are registered in `Program.cs` with `app.MapExpenseEndpoints();`. Subscriptions needs the same connections Expenses has, plus one the skill cannot know about:

1. A `DbSet<Subscription>` and its configuration in `ExpenseDbContext` (precision on `Amount`, max length on `Currency`, the enum stored as you prefer).
2. An EF Core migration: `dotnet ef migrations add AddSubscriptions --project app/api/Api`.
3. A `app.MapSubscriptionEndpoints();` line in `Program.cs`.

   Both of those files also need `using ExpenseTracker.Api.Features.Subscriptions;` at the top. Neither is covered by implicit usings, so without it the build fails on `MapSubscriptionEndpoints` and on `Subscription` before you ever reach a test.
4. A line in `ExpenseApiFactory.ResetAsync()` clearing the new table:

   ```csharp
   await db.Subscriptions.ExecuteDeleteAsync();
   ```

   That method only truncates `Expenses` today. Every test calls it in `InitializeAsync`, so without this line the rows your POST tests create survive into the next test in the class, and any assertion about the full list (an ordering check, a count) fails against leftovers. Skip it and you get a red test with a confusing message. This one is easy to miss because it lives in the test factory, not in the slice, so your skill has no reason to touch it.

If you already settled the enum decision in step 0's `references/enums.md` and chose `JsonStringEnumConverter`, then two more edits belong in *this* block, not deferred to step 5: register the converter in `Program.cs` (the skill reports it, it does not edit `Program.cs`), and give the generated test its own `JsonSerializerOptions` carrying that converter (the block in step 5). Wired that way the slice is green here and stays green, and the "went red" surprise step 5 describes never happens. That is the correct outcome, not a sign you skipped something. The staged path below, number over the wire now and the converter in step 5, is the other legitimate choice; it just trades an early decision for a red test you fix later.

Then run just your slice:

```bash
dotnet test --filter FullyQualifiedName~Subscription
```

Green means the Subscriptions endpoints work against a real Testcontainers database. To see it live, boot the app and `POST /api/subscriptions` with a small JSON body (vendor, amount, currency, billing cycle, renewal date), then `GET /api/subscriptions`. Look at the raw response before you move on. The `billingCycle` you get back is the thing step 5 has to deal with.

### Step 5: Teach the skill the web half

Do not start a second skill unless you want to. Put the web half in `${CLAUDE_SKILL_DIR}/references/web-slice.md` and have SKILL.md load it when a run reaches the web side. It reuses everything the base already defines, the argument, the derivation rules, and the "read the entity" instruction, so the reference file carries only what is web-specific. Keeping it out of the base body is what lets an API-only run skip it entirely.

Three things to change:

1. **Add the camelCase forms to your derivation block.** You have `<Res>`, `<Ress>`, `<ress>`. Add `<res>`, the singular camelCase (`subscription`), and be explicit about which form each artifact uses. The route segment and the query key are `<ress>`. The api functions are `create<Res>` and `get<Ress>`. The components are `<Res>Form.tsx` and `<Res>Table.tsx`. Getting this wrong is not a compile error, it is a query key mismatch, so the list silently fails to refresh after a create.

2. **Point it at the web pattern, and at the frontend rules.** Same move as the API half: name `app/web/src/lib/api.ts`, `components/ExpenseForm.tsx`, `components/ExpenseTable.tsx`, and `pages/ExpensesPage.tsx` as the reference, and `.claude/rules/frontend.md` as binding. Do not paste them in.

3. **State the enum decision, and put it in `references/enums.md`.** This is the textbook case for progressive disclosure: most resources have no enum, so the whole trap belongs in `${CLAUDE_SKILL_DIR}/references/enums.md`, read only when the entity has an enum property. Have SKILL.md say "if the entity has an enum, read `references/enums.md`" and keep the detail there. Whichever of the two options from "The web half" you pick, write it down, including the consequence: if you choose `JsonStringEnumConverter`, the skill has to report that `Program.cs` needs it, since the skill does not edit `Program.cs`.

   And there is a second consequence that is easy to miss, because (if you deferred the enum decision to here rather than wiring it back in step 4) it shows up as a **test suite that was green in step 4 and is red now**. `WebApplicationFactory.CreateClient()` hands you a plain `HttpClient`. `PostAsJsonAsync`, `GetFromJsonAsync`, and `ReadFromJsonAsync` use their own default `JsonSerializerOptions`, not the ones you configured on the server. So the moment the API starts speaking `"Annual"`, the test posts a `1` and then throws trying to read a string back into the enum. The generated test has to opt into the same converter:

   ```csharp
   private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
   {
       Converters = { new JsonStringEnumConverter() },
   };
   ```

   and pass `Json` to every `PostAsJsonAsync` / `GetFromJsonAsync` / `ReadFromJsonAsync` call, before the `CancellationToken`. Push that into the skill too, next to the enum decision, so the next resource with an enum comes out of the box with a test that passes.

The skill should generate:

```
app/web/src/lib/api.ts                        # extended, not replaced: Subscription types + 3 functions
app/web/src/components/SubscriptionForm.tsx   # useMutation, invalidates ["subscriptions"]
app/web/src/components/SubscriptionTable.tsx  # useQuery + delete useMutation, loading/error/empty states
app/web/src/pages/SubscriptionsPage.tsx       # placeholder replaced, composes the two
```

Two of these files are not blank canvases. `SubscriptionsPage.tsx` holds the coming-soon placeholder, so tell the skill to replace it only when it still is that placeholder, and otherwise to show its diff and ask rather than clobber real work. And `lib/api.ts` is shared with Expenses, so it is the one file the skill appends to rather than creates. Say so explicitly, or you will get the Expenses functions deleted. That is the same disjointness rule as the API half, applied to the one file where the two tracks genuinely have to share.

Then delete the generated web files, restore the `SubscriptionsPage` placeholder, and run `/new-endpoint Subscription` once more. The proof is a clean run that produces both halves.

### Step 6: See it in the browser

```bash
docker compose up -d
dotnet ef database update --project app/api/Api
dotnet run --project app/api/Api
pnpm --dir app/web dev
```

Open http://localhost:5173, click **Subscriptions**, and actually use it. Add one. Check it appears in the table without a manual refresh, which is the query-key invalidation working. Delete it. Check the empty state comes back.

Then:

```bash
pnpm --dir app/web typecheck
pnpm --dir app/web lint
```

Both clean. A page that renders in the browser can still be a page that fails `typecheck`, because Vite's dev server does not typecheck.

---

## Troubleshooting

### The skill overwrote or edited an Expenses file

Your skill is writing to a path derived from something other than the argument, or it hardcoded the Expenses path. Find where the skill decides the output folder and make it a function of `$resource`. Disjoint files are what keep your Subscriptions track from ever conflicting with my Expenses track, so this one is worth fixing properly.

### Generated code still says "Expense" somewhere

The skill used a literal instead of the argument at that spot. Search the `SKILL.md` body for `Expense` and replace each with `$resource` or the right derived form. If it is inside an `@`-included example file, parameterise the example or stop including it literally.

### My skill says "Subscription" all over it

Then it is not a scaffolder, it is a template for one entity, and it only looked generic because the entity you tested it on is the one you wrote it for. This is the target-side version of the `Expense` leak above, and it is easier to miss because the output looks right. Search the whole skill, `SKILL.md` and every `references/*.md`, for `Subscription`, `Vendor`, `BillingCycle`, `RenewalDate`; there should be zero hits. Replace them with placeholders (`<Res>`, `<Ress>`, `<ress>`) for the logic, and with a neutral entity (`Product`, `Category`) for any worked example. A quick proof it is really generic: run it once for `Subscription` and once for some other resource and confirm both come out right.

### A literal `$1` or `$1s` or `${resource}` ended up in a path or a type name

The placeholder was never substituted, so the model took it as text. Only the bare `$resource` form works. Rewrite those spots and re-read "How arguments actually substitute" above.

### The Subscription is missing BillingCycle and RenewalDate

A likely first-run failure, though not a guaranteed one. If the skill learned its DTO shape from Expenses, which has neither field, its generated contracts drop them. Decide how the skill should acquire an entity's real fields, the cleanest option here is to have it read `Subscription.cs` (an argument or an interactive prompt also work), and build that in. Or narrow the skill's stated job to "scaffold the plumbing, you add the domain fields."

If the fields came out correct anyway, do not skip this: that is the luck-versus-guarantee question from Step 2. A correct DTO is not proof the `SKILL.md` instructs the read; if it does not, the next resource is a coin flip.

### The route collides with Expenses

Both areas mapped to the same prefix. The route must derive from the argument (`/api/subscriptions`, not `/api/expenses`). Fix it in the skill's endpoint-mapping template.

### The migration fails or the test cannot find the table

The entity is not on the `DbContext`, or you added the migration but did not update the database. The test factory calls `MigrateAsync` on startup, so a missing or unapplied migration shows up as a missing table. Add the `DbSet<Subscription>`, create the migration, and let the test's `MigrateAsync` apply it.

### `dotnet ef database update` fails with `relation "Subscriptions" already exists`

Only bites if you (or a classmate on the same machine) ran a version of this exercise before: the dev Postgres volume persists across `docker compose` restarts, so an earlier run's `AddSubscriptions` table and its `__EFMigrationsHistory` row survive, and your fresh migration has a different id, so EF tries to `CREATE TABLE` a table that is already there. The Testcontainers tests never see this (each run gets a clean throwaway container), so it only surfaces at the Step 6 `database update`. Drop the stale table and clear its history row, then re-apply:

```bash
docker exec expense-tracker-db psql -U expense -d expenses \
  -c 'DROP TABLE IF EXISTS "Subscriptions";' \
  -c 'DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" LIKE '"'"'%AddSubscriptions%'"'"';'
dotnet ef database update --project app/api/Api
```

The Postgres credentials are `expense` / `expenses` (from `docker-compose.yml`), not the `postgres` default, if you reach for `psql` directly.

### One Subscriptions test passes alone but fails in the suite

Almost always the missing `ExpenseApiFactory.ResetAsync()` line: add `await db.Subscriptions.ExecuteDeleteAsync();` next to the existing `Expenses` line. Step 4 explains why.

### `/new-endpoint` isn't found

The skill is not loading. Confirm it is at `.claude/skills/new-endpoint/SKILL.md` relative to the project root you launched Claude Code from, that the frontmatter parses, and that `name` and `description` are both present, since a skill without a description does not register. A new skill is normally picked up straight away, so if `/` still does not list it, suspect the frontmatter before you suspect the session. Restarting is a last resort, not the first thing to try.

### The build fails with CS1061 on `MapSubscriptionEndpoints`

```
error CS1061: 'WebApplication' does not contain a definition for 'MapSubscriptionEndpoints'
```

A missing `using ExpenseTracker.Api.Features.Subscriptions;` in `Program.cs`. The extension method is real, it is just not in scope. `ExpenseDbContext.cs` needs the same using for the `DbSet<Subscription>`. Your skill deliberately does not edit either file, so this one is on you (see step 4).

### The billing cycle shows as 1 instead of Annual

Working as designed, and the decision you were told to make in step 5. `System.Text.Json` writes enums as numbers by default, and EF Core's `HasConversion<string>` only affects the database column, not the wire. Either add `JsonStringEnumConverter` on the API and type the TS side as a string union, or keep the number and map it to a label for display. Fix it in the skill so the next resource with an enum comes out consistent.

### The Subscriptions tests went red right after I added `JsonStringEnumConverter`

```
System.Text.Json.JsonException: The JSON value could not be converted to
ExpenseTracker.Api.Features.Subscriptions.BillingCycle.
```

Nothing is wrong with the endpoints. The test client is the problem. `CreateClient()` returns a plain `HttpClient`, and `PostAsJsonAsync` / `GetFromJsonAsync` / `ReadFromJsonAsync` serialise with their own defaults, not with the options you registered in `Program.cs`. Server and client are now speaking different dialects: the test posts `"billingCycle": 1` and chokes reading `"Annual"` back.

The fix is the test's own `JsonSerializerOptions`, passed on every call. It is the `Json` block in step 5. Fix it in the skill, not just in the file: a skill that emits the converter decision without the matching test setup ships a red suite every time.

### The table does not refresh after adding a subscription

The query key the mutation invalidates does not match the key the query uses. `useQuery({ queryKey: ["subscriptions"] })` and `invalidateQueries({ queryKey: ["subscription"] })` are different keys, and nothing warns you. This is the camelCase and plural derivation from step 5 leaking. Fix the derivation rule in the skill, not just the string in the file.

### The page fails to load data and the browser console shows a CORS or network error

Usually the API is not running, or it is not on 5033. The web app reads `VITE_API_BASE` and defaults to `http://localhost:5033`, and the API only allows the Vite origin when it is running in Development. Start the API first, then reload the page.

### `pnpm typecheck` fails but the page looked fine

Vite's dev server does not typecheck, so a wrong TS interface renders happily until you build. This is why step 6 runs `typecheck` separately. If the failure is on the shape of the response, compare the interface against the C# `record` field by field. They are supposed to match, and the frontend rules say so.

### The Expenses functions disappeared from `lib/api.ts`

Your skill treated a shared file as one it creates instead of one it appends to. `lib/api.ts` is the single place the two tracks share. Make the skill say "add to" rather than "write", and re-read the disjointness gotcha in step 5.

---

## You can now

Author a scaffolding skill, run it on a surface it was not written for, spot the assumptions it baked in, including whole fields it could not know about, and push the fix back into the skill so the next surface is free. You have taken that skill across a stack boundary, where the naming rules and the conventions both change and the seams between the two sides are where the bugs live. You have also stood up the Subscriptions area end to end, database to browser, your parallel track for every solo exercise from here on.

---

## Summary

You have added:

- **`Features/Subscriptions/` endpoints**: the first slice of your Subscriptions area, endpoints plus DTOs with `BillingCycle` and `RenewalDate`, disjoint from Expenses and wired into `Program.cs` and the `DbContext`.
- **A Subscriptions Testcontainers test**: green, proving the slice works against a real database.
- **The Subscriptions page**: the placeholder replaced with a real form and table, fetching through TanStack Query, sharing `lib/api.ts` with Expenses without trampling it.
- **A `new-endpoint` skill**: authored by you from the Expenses slice, then hardened, so the `Expense`-specific literals it started with are gone, it has a defensible answer for how it learns an entity's fields, and it scaffolds both sides of the stack from one argument.

---

## What's next

In Section 5 I build the safety-net hooks on the repo. Your solo challenge there is repo-global too (hooks do not belong to one area), so it follows the hint model rather than the two-area split. The two-area track picks back up in Section 6, when you build a reviewer that checks your Subscriptions code the way mine checks Expenses.

---

## Reference

Everything you need is on disk. The frontmatter skeleton in Step 0 is your reference for *how a skill is written*.

For *what the skill should generate*:

- API: `app/api/Api/Features/Expenses/`, plus `app/tests/Api.Tests/ExpenseApiTests.cs` and `ExpenseApiFactory.cs`. Conventions in `.claude/rules/backend.md`.
- Web: `app/web/src/lib/api.ts`, `components/ExpenseForm.tsx`, `components/ExpenseTable.tsx`, `pages/ExpensesPage.tsx`. Conventions in `.claude/rules/frontend.md`.

Two layout quirks worth knowing before you derive paths from them. The Expenses tests sit flat in `app/tests/Api.Tests/`, not in an `Expenses/` subfolder, while your Subscriptions test goes in `app/tests/Api.Tests/Subscriptions/` to keep the two tracks disjoint. And the web app is not sliced by feature the way the API is: components live together in `components/`, so the two tracks share a folder and share `lib/api.ts`.

If you get stuck on the feature rather than the skill, compare against the Expenses slice. Just do not copy its files, derive the Subscriptions equivalents.
