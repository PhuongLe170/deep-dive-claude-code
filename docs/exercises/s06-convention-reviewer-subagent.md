# S6 Solo - Build the `convention-reviewer` Subagent

> Section 6, Subagents. Solo. Same shared project. Hints and troubleshooting, not a full hand-hold.

## Mission

This section has one build, and you do it with your own hands: the `convention-reviewer`. A read-only subagent, on a cheaper model, running in an isolated worktree, that reviews any change in this repo against the project's own conventions. Nothing like it exists in the repo before this section. You are creating it from scratch.

It reviews the whole codebase, not one feature. That is the point. The conventions it enforces (record DTOs, explicit mapping, TanStack Query, snake_case tests) come from `.claude/rules/` and apply to every slice, Expenses and Subscriptions alike. One reviewer for the repo, not a new agent per feature.

By the end, you point it at a change that breaks the conventions and it flags each violation with a file and line, and you point it at clean code and it stays quiet.

**Learning Objectives**

- Author a custom subagent from its frontmatter up: read-only tools, a cheaper model, and worktree isolation.
- Encode a project's real conventions as a review checklist the agent can apply.
- Scope a reviewer to the whole repo rather than a single feature, and know why that is the right call.
- Confirm a reviewer both catches real violations and stays quiet on clean code.

---

## Prerequisites

- You have L1 to L5 behind you (what a subagent sees, the Agent tool, the authoring surface, and the review pattern).
- Your working copy is at the Section 6 baseline, with `.claude/rules/{backend,frontend,tests}.md` in place. Those rules are the source of truth for the checklist.
- Docker is up. The repo's Stop hook builds and runs the test suite on every stop, so a stopped turn with Docker down will error. It also means any change you leave in the working tree must **compile**: if you plant a breaking change that does not build, the Stop hook fails on the build, not on a convention. Keep intentional violations compilable, or park them on a scratch branch and switch away before you stop.

---

## What we're solving

A generic code reviewer knows generic good taste. It does not know that this repo bans AutoMapper, insists on `record` DTOs, fetches only through TanStack Query, and names its tests in snake_case `Should_When`. So a generic reviewer passes code this project would reject in review. That gap is the whole reason to build a tuned one.

The tuning is not per feature. The conventions are repo-wide, so the reviewer is repo-wide. If you catch yourself about to build a second reviewer for the next feature, stop. You do not clone the agent per area. You point one reviewer at the whole repo, and if a feature ever needs an extra rule, you add that rule to this one reviewer.

---

## The conventions to enforce

Your reviewer judges every change against these, straight from `.claude/rules/`:

| Area | Rule | The mistake to catch |
|------|------|----------------------|
| Backend | DTOs are `record` types with explicit `ToEntity()` / `ToResponse()` mapping | a `class` DTO, or AutoMapper anywhere |
| Backend | Handlers are async all the way, taking and passing a `CancellationToken` | a sync handler, or one that drops the token |
| Backend | Warnings fail the build (`TreatWarningsAsErrors`) | code that would trip a warning |
| Frontend | Data access goes through TanStack Query via `lib/api.ts` | a raw `fetch` in a component |
| Tests | Integration tests are snake_case `Should_When` | a PascalCase or camelCase test name |

These are the same rules Claude already reads from `.claude/rules/`. The reviewer's job is to make them checkable on a diff, out loud, with a reason.

---

## What a subagent sees, and the authoring surface

Read this before you write the file. It is the part that decides whether the reviewer can do its job and nothing but its job.

A subagent is not the main thread with a hat on. It runs in its own fresh context: it does not see your conversation, only the prompt it is handed and whatever its tools let it read. So the reviewer cannot lean on anything you said to Claude a moment ago. Everything it needs, the conventions and the diff, it has to fetch through its own tools. That isolation is the feature. It is also why the `SKILL`-style instinct of "it already knows the repo" does not carry over.

You author it as a single Markdown file at `.claude/agents/convention-reviewer.md`, project-local, not in your global `~/.claude`. The frontmatter is the whole control surface, and five fields carry the design:

| Field | Example | What it decides |
|-------|---------|-----------------|
| `name` | `convention-reviewer` | how you address it: `@convention-reviewer`, or the Agent tool by this name |
| `description` | "Reviews any change… use proactively after edits" | **when Claude reaches for it unprompted.** Matched on intent, so write when to run it, and carry the word "proactively" |
| `tools` | `Read, Grep, Glob, Bash` | what it can touch. **Read-only here**, so it can never edit while it reviews. Bash is for `git diff`, nothing more |
| `model` | `haiku` | a cheaper model, because reading a diff against a checklist is cheaper work than writing code |
| `isolation` | `worktree` | it reviews a clean checkout in its own worktree and cannot touch your working tree |

The two fields to think hardest about are `tools` and `description`. `tools` is the first lock that keeps a reviewer from turning into a refactorer: leave `Edit` and `Write` off and it physically cannot rewrite your code. `description` is what makes it fire without being named, so it is matched on *intent* ("use after edits") not on output. A description that only says what it is fires less reliably than one that says when to reach for it.

### Where it lives

Put the file in the project's `.claude/agents/`, the same `.claude/` the CLI loads for this course, so it ships with the repo and is picked up as soon as it is saved, no restart. Do not author it in your global `~/.claude/agents/`; a repo's conventions belong to the repo, not to your machine. Confirm it registered before you lean on it: type `@` and look for `convention-reviewer` in the list.

---

## If you're comfortable, do this

Four steps. Skip the rest if it works on the first try.

0. Create `.claude/agents/convention-reviewer.md`. Frontmatter: read-only `tools` (Read, Grep, Glob, and Bash for `git diff`), a cheaper `model`, `isolation: worktree`, and a `description` that says it reviews changes against the project's conventions and should run proactively after edits. Then write the body as a checklist from the table above, each rule with a wrong form and a right form, plus an instruction to cite file and line and to order findings by severity.
1. Apply a change that breaks several rules on purpose (a `class` DTO with AutoMapper instead of a `record` with explicit `ToResponse()` mapping, a raw `fetch`, a PascalCase test), commit it to a scratch branch, then run the reviewer over that committed diff. `isolation: worktree` cannot see uncommitted work, so commit first and `git switch main` before you stop.
2. Revert to clean code and run it again.

**Done when**: pointed at the breaking change, the reviewer flags each violation with a file, a line, and a reason; pointed at clean code, it passes without inventing problems. Both halves matter. A reviewer that flags everything is as useless as one that flags nothing.

---

## Full path

Only if the fast path did not just work.

### Step 0: Author the subagent

This is the exercise. Everything after it just runs and checks what you build here, so build it in small moves and know the trap in each. Create `.claude/agents/convention-reviewer.md`.

**0.1 The frontmatter.** Write it from the shape below and adjust each field with the reasons from "the authoring surface" above.

```markdown
---
name: convention-reviewer
description: Reviews any change in this repo against the project's conventions. Use proactively after edits under app/.
tools: Read, Grep, Glob, Bash
model: haiku
isolation: worktree
---
```

Read-only tools, so it can never edit while it reviews. Bash is there for `git diff`, nothing more. A cheaper model, because reading a diff against a checklist is a cheaper job than writing code. `isolation: worktree`, so it reviews a clean checkout and cannot touch your working tree. The `description` carries "proactively", so Claude reaches for it after a change without being named. The trap here is `tools`: leave `Edit` and `Write` on by habit and you have built a refactorer, not a reviewer.

**0.2 The checklist body.** Write the conventions from `.claude/rules/` as explicit rules, each with a wrong form and a right form. State the reason in the rule, not just the rule. "This throws away compile-time safety" gives you a better review comment than "wrong type." End the body with an instruction to cite file and line for every finding and to order findings by severity, so the output is actionable rather than a wall of prose.

One rule needs a carve-out, or your reviewer will flag the project's own correct code. "Map through `ToResponse()` instead of building a response by hand" is right for a single loaded entity, but **not** for a `new XResponse(...)` inside an EF Core `.Select(...)` projection. EF cannot translate the `ToResponse()` extension method into SQL, so the reference slice's `GetExpenses` and `GetBudgets` project inline on purpose (`ExpenseEndpoints.cs`). Write that exception into the rule. A checklist that bans every inline `new XResponse(...)` will flag those two handlers and suggest a "fix" (`.ToResponse()`) that breaks the query. This is the clean-pass failure mode from Step 2, seeded at authoring time.

Keep it repo-wide. Do not scope the instructions to Expenses or Subscriptions. Say "any feature slice under `Features/`" and "any component under `app/web/`", so the one reviewer covers whatever you build next. The trap here is the mirror of the `SKILL` lesson from Section 4: a checklist littered with one feature's type names reads generic but is really welded to that feature.

**0.3 Confirm it registered.** Type `@` and look for `convention-reviewer` in the list. It should show up as soon as the file is saved, no restart. If it does not, suspect the frontmatter before the session (see Troubleshooting). Some environments snapshot the agent list at session start instead of picking up new files live. If invoking by name fails even with correct frontmatter, start a fresh session or window and try again before you suspect the file itself.

### Step 1: Break the rules on purpose

In a scratch edit, do a few of these at once: change a DTO from `record` to `class`, add an AutoMapper `Profile` instead of an explicit `ToEntity()` / `ToResponse()`, drop the `CancellationToken` from a handler and call sync EF, swap a TanStack Query call for a raw `fetch`, and name a test `ReturnsTotal` instead of `Should_Return_Total_When_...`.

**Commit the change before you review it.** `isolation: worktree` reviews a committed ref, so it never sees your *working tree*. Uncommitted edits are invisible to it no matter how you set `worktree.baseRef`, because even `head` branches from a commit, and your commit does not contain them yet. Put the breaking change on a scratch branch:

```
git switch -c scratch/convention-break
git commit -am "break conventions on purpose"
```

Then run the reviewer against that committed ref. It is visible from inside the reviewer's worktree because worktrees share the repo's object store:

```
Use the convention-reviewer to review the diff of scratch/convention-break against main (git diff main..scratch/convention-break).
```

It should flag each violation, with the file and line and a reason. If a review comes back vague, tell it to re-review strictly against its own checklist and cite file and line for every finding.

Before you end the turn, `git switch main` so the Stop hook builds a clean working tree, not your broken branch. This is why a scratch branch beats editing in place: the break lives on a ref the reviewer can read but the build never touches, so it never has to compile.

If instead the reviewer reports on code you never touched, or passes everything, it is not seeing your change. Almost always that is the isolation trap: the change is still uncommitted, or the worktree is branching from a ref that does not contain your commit. See "The worktree isolation reviews the wrong code" in Troubleshooting.

### Step 2: Prove the clean pass

Revert to the correct code and run it again. It should pass without manufacturing issues. If it flags clean code, the instructions are too vague or too aggressive. Tighten them.

```
convention-reviewer review the latest commit on this branch.
```

---

## Troubleshooting

### The reviewer edits files instead of just reviewing

Its `tools` are not read-only. Remove Edit and Write. A reviewer that can write is a refactorer, which is not the job. The worktree is a second lock, but the tool list is the first.

### It misses a violation

The rule is not specific enough, or the reviewer is not seeing the changed files. Name the thing to catch explicitly (the word "AutoMapper", the `class` keyword on a DTO) and confirm the diff is actually in front of it.

### It flags clean code

Over-tuned. If you told it to be suspicious of every `string`, it will flag fields that are correctly strings. Scope each rule to the exact thing it guards. The one that bites here: a rule that bans every inline `new XResponse(...)` will flag the reference slice's EF `.Select(...)` projections in `GetExpenses` / `GetBudgets`, which are correct (EF cannot translate `ToResponse()` into SQL). Carve that exception into the rule, as in Step 0.2.

### The worktree isolation reviews the wrong code (it misses your change entirely)

This is the one that will fool you. `isolation: worktree` reviews a **fresh checkout branched from `origin/main`** (the default base ref, `fresh`). It does **not** check out your current local branch. So neither your uncommitted edits **nor a commit you made on a local branch** are in front of it. Commit to a scratch branch and the reviewer still sees `origin/main`, reviews *that*, and reports on code you never touched. It looks like the reviewer is broken or hallucinating. It is not. It is reviewing a tree that does not contain your change.

First, be clear on what it can *never* see: **uncommitted edits.** A worktree checks out a committed ref, so your working-tree changes are invisible to it regardless of `worktree.baseRef` (even `head` branches from a commit, and an uncommitted edit is in no commit). So "review my uncommitted changes" cannot work under isolation. Commit first. Then, to get your committed change in front of it, pick one:

- **Diff a committed branch ref (recommended).** Commit the change to a scratch branch and ask the reviewer to review `git diff main..<your-branch>`. Both refs live in the shared object store, so the reviewer reads them from inside its worktree without any config change. This is the least fiddly path and the one Step 1 uses.
- **Point the worktree at your local HEAD.** Set `worktree.baseRef` to `head` in `.claude/settings.local.json` (`{ "worktree": { "baseRef": "head" } }`). Now the worktree branches from your local HEAD, so a committed change on your current branch is visible. Note this still requires a commit; it does nothing for uncommitted work.
- **Push the branch** so your change is reachable from the ref the worktree branches from.
- **Drop `isolation: worktree` for the local test**, review your working tree directly (the only way to review *uncommitted* edits), then put the field back once you have seen both halves pass.

Some sandboxed setups block editing `.claude/` config from inside Claude. If your `settings.local.json` edit is refused, make that edit in your editor outside the session.

### You are tempted to build a second reviewer for another feature

Don't. Add the rule to this one. One repo, one `convention-reviewer`.

### Invoking it by name says "agent not found"

The file and frontmatter are fine, but the session started before the file existed and is holding a stale agent list. This shows up in some environments more than others. Start a new session or window, then invoke it again. Do not conclude the reviewer is broken until you've tried that.

---

## You can now

Author a subagent from scratch, encode a real project's conventions as a checklist it applies, and scope it correctly to the whole repo. You have a reviewer that catches the convention slips a generic reviewer waves through, and stays quiet on code that follows the rules.

---

## Summary

You have added:

- **`.claude/agents/convention-reviewer.md`**: a read-only, cheaper-model, worktree-isolated reviewer scoped to the whole repo.
- **The project's conventions as a checklist**: record DTOs and explicit mapping with no AutoMapper, async handlers that pass a `CancellationToken`, TanStack Query over raw fetch, snake_case `Should_When` tests, and warnings-as-errors.
- **A two-way verification**: it flags a breaking change with file and line, and passes clean code.

---

## What's next

In Section 7 I wire and lock down an MCP server on the repo. That solo is repo-global too (MCP config does not belong to one feature), so it is another hint-model task: add and scope a second MCP server, writing its allow and deny rules before you trust it.
