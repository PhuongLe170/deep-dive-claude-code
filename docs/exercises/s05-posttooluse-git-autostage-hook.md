# S5 Solo - A `PostToolUse` Git Auto-Stage Hook

> Section 5, Hooks. Solo. Same shared project. Hints and troubleshooting, not a full hand-hold.

## Mission

In the lecture I built the safety-net hook set: a `Notification` hook, a `PreToolUse` hook that blocks edits to migrations and secrets, and a `PostToolUse` hook that formats and tests. Now you add one more hook of your own, a second `PostToolUse` entry, so every file Claude writes lands in the git staging area on its own. Right after Claude edits a file, the `PostToolUse` event hands your hook the path it just wrote, and your hook runs `git add` on that exact file.

Hooks are repo-global. They do not belong to the Expenses or the Subscriptions area, so this is a parallel hook task, not a Subscriptions feature. That is the whole reason it follows the hint model rather than the two-area split. There is no second entity to build, just one config artifact that serves the whole repo.

By the end, editing a file through Claude leaves it already staged, the edit itself still runs untouched, and your `git status` sorts Claude's work into the index cleanly apart from anything you changed by hand. You will also have met the one interaction that makes this hook harder than it looks: it shares the `PostToolUse` event with the lecture's formatter, and the two do not run in a guaranteed order.

**Learning Objectives**

- Attach a `command` hook to `PostToolUse` and scope it with a matcher so it fires only on the file-writing tools.
- Read `tool_input.file_path` from the hook's stdin payload and act on the exact file Claude just changed.
- Know the `PostToolUse` exit-code contract, and why a git failure here must never surface as a block.
- See that matching hooks for one event are not ordered against each other, and design around the race with the lecture's formatter instead of assuming it away.
- Tell the difference between a hook that gates an action and a hook that reacts to one, and let that difference decide your exit code.

---

## Prerequisites

- S5L7 finished. The guided hook set exists (`Notification`, `PreToolUse`, `PostToolUse`) in your settings or plugin `hooks.json`, and you have seen a hook fire.
- The repo is a git working copy. Auto-staging only makes sense where `git add` has an index to add to. Confirm with `git rev-parse --is-inside-work-tree`.
- `jq` is on your `PATH`. The lecture hooks already lean on it, so if they ran, you have it (`which jq`).

---

## What we're solving

A long Claude session touches a lot of files. By the time you come up for air, your working tree is a wall of modified files, and every one of them sits unstaged next to whatever you edited yourself. Telling Claude's changes apart from your own means reading each diff, and the natural checkpoint before a commit turns into a chore.

Staging fixes that split. Once a file is in the index, `git status` sorts it into the green column, and `git diff --staged` shows you the whole of Claude's work as one reviewable set, cleanly separated from the unstaged changes you have not blessed yet. The trouble is remembering to stage as you go. You do not, so the index stays empty until commit time and the split never happens.

The `PostToolUse` event fires the moment Claude finishes writing a file, and it is the natural place to close that gap. The event payload on stdin carries `tool_input.file_path`, the path of the file the tool just wrote. So the hook does not have to guess what changed or scan the tree. It stages the one file it was handed, every time, at zero ongoing effort.

That is the shape of the whole exercise, and it is smaller than it sounds: a `command` hook on `PostToolUse` that runs `git add` on the file from the payload keeps the index in step with Claude's edits. The interesting part is not the `git add`. It is two constraints around it, and both are the point of the exercise: this hook must never turn a git hiccup into a block, and it shares its event with a hook that can undo its work.

One caveat worth naming up front, because it is the real trade. Auto-staging blurs a line you might want kept sharp. When every edit is staged the instant it happens, you lose the "Claude touched this, I have not looked yet" signal that an unstaged file gives you for free. You are trading that per-file signal for a clean per-turn split between staged and unstaged. That is a fair trade for a fast feature build where you review the staged set as a whole. It is the wrong trade if you lean on unstaged status as your review queue. Decide which you want before you wire it in.

---

## What `PostToolUse` actually hands you

Read this before you write the command. It is the part that decides whether your hook stages the right file, and whether it stages one at all.

A hook is a shell command reacting to an event. When `PostToolUse` fires, Claude Code writes a JSON object to the command's **stdin** and runs it, *after* the tool has already done its work. Capture it once and it looks like this:

```json
{
  "session_id": "a1b2c3d4-...",
  "transcript_path": "/Users/you/.claude/projects/<slug>/<id>.jsonl",
  "cwd": "/Users/you/repo",
  "hook_event_name": "PostToolUse",
  "tool_name": "Edit",
  "tool_input": {
    "file_path": "/Users/you/repo/app/api/Api/Features/Subscriptions/SubscriptionEndpoints.cs",
    "old_string": "...",
    "new_string": "..."
  },
  "tool_response": { "filePath": "...", "success": true }
}
```

The fields you build the hook around:

| Field | Example | What it's for |
|-------|---------|---------------|
| `tool_name` | `"Edit"` / `"Write"` | which tool just ran; your matcher already filters on this, but it is here to read too |
| `tool_input` | `{ "file_path": "...", ... }` | **the whole exercise** lives at `.tool_input.file_path` |
| `tool_response` | `{ "success": true, ... }` | the tool's result, so you *could* skip staging a write that failed |
| `cwd` | `"/Users/you/repo"` | the directory the session is rooted in |
| `transcript_path` | `"/Users/.../id.jsonl"` | the conversation file; not needed here |

Three things about `file_path` that decide whether your hook works:

- **It can be absolute.** Claude writes `file_path` as the path it used, which is often absolute (`/Users/you/repo/app/...`). `git add` accepts an absolute path or a repo-relative one, as long as the file sits inside the working copy, so pass it through verbatim. Do not try to rewrite it into a relative path first.
- **Not every write tool puts it at `tool_input.file_path`.** `Edit`, `Write`, and `MultiEdit` all use `file_path`. But **`NotebookEdit` uses `notebook_path`**, not `file_path`. If you ever widen the matcher to include notebooks, a plain `.tool_input.file_path` reads empty and the notebook never stages. For this exercise, keep notebooks out of the matcher and you sidestep it, but know the field name is not universal.
- **Tools with no file have none.** A `Bash` or a read-only tool carries no path at all. Your matcher should keep those out, but guard for an empty value anyway so a call that slips through stages nothing instead of erroring on a `null`.

`tool_response` is optional flavor. You could inspect `.tool_response.success` to skip staging when the write reported a failure. It is not required. If the write failed there is nothing new to stage, and `git add` on an unchanged file is a harmless no-op, so a plain unconditional stage is a complete answer. Reading it is the bonus in the fast path.

---

## Matcher and ordering: the two things that make or break this

This is the section that separates a hook that works from one that half-works. Read it before Step 0.

### The matcher is a regex against `tool_name`

The `matcher` on a `PostToolUse` entry is matched against the `tool_name` string, and it is a regex, not a literal. That gives you three behaviours to get right:

| Matcher | Fires on | Note |
|---------|----------|------|
| `"Edit\|Write\|MultiEdit"` | the three file-writing tools | what you want here |
| `""` or `"*"` | every tool, including `Bash`, `Read`, `Grep` | a pointless `git add` after every read |
| `"Edit"` | `Edit` only | misses `Write` (new files) and `MultiEdit` (multi-change edits) |
| `"edit"` | nothing | matching is case-sensitive; `tool_name` is `Edit` |

Scope it to `Edit|Write|MultiEdit`. Too wide and the hook runs on reads and shell commands; too narrow and a brand-new file written with `Write` never stages. The empty-path guard from the previous section is your backstop for anything that still slips through.

### Matching hooks for one event are not ordered against each other

Here is the interaction the naive version misses. You already have a `PostToolUse` hook from the lecture: format-and-test. Now you are adding a second `PostToolUse` hook: auto-stage. Both match the same edit. And matching hooks for a single event are **not guaranteed to run in a set order relative to one another**. You cannot assume the formatter runs before your `git add` just because you listed it first.

Why that matters, concretely:

1. Claude edits `SubscriptionEndpoints.cs`.
2. Your auto-stage hook runs and stages the file *as written by Claude*.
3. The formatter runs and rewrites the same file to satisfy the style rules.
4. Now the **staged** copy is the unformatted version, and the working tree is dirty again with the formatting diff. `git status` shows the file both staged (old bytes) and modified (new bytes). Exactly the confusion you built the hook to avoid.

This is not hypothetical in this repo. The lecture's `format-code.sh` really does rewrite files: `dotnet format whitespace` on any `.cs`, Prettier on anything under `app/web/`. So a separate auto-stage entry **will** leave `.cs` and web edits both staged and modified whenever the format order loses the race. Edits the formatter ignores (a `.md`, a `.txt`) are never touched and stage cleanly. If you keep the separate entry, expect to see this on your API and web files.

The order can also come out the other way and be fine. That is the problem: it is a race, so sometimes it looks correct and sometimes it does not, which is the worst kind of bug to chase.

The root cause is worth naming, because it points straight at every fix. The problem is not really "which hook runs first." It is that one hook rewrites the file's bytes *after* the edit, the formatter, so whichever hook stages before that rewrite stages stale bytes. Fix that timing and the race is gone. There are four ways to do it, and you should pick one on purpose rather than leave it to chance.

- **Fold the `git add` into the formatter's command.** Instead of a second, separate hook, append the stage step to the *end* of the existing format-and-test command, so within one script the order is guaranteed: format first, then `git add`. Race-free. It is what "Bonus two" in "Done when" points at. The cost is that staging now lives inside the formatter script, so the two jobs share one file.
- **Let the formatter re-stage its own output, and keep both hooks separate.** Leave `auto-stage.sh` as it is and add a `git add -- "$file"` to the *end* of `format-code.sh` too. The formatter is the only hook that changes bytes after the edit, so if it always stages what it just rewrote, both orderings land clean. Run the formatter first and auto-stage re-stages the same formatted bytes. Run it second and its own stage supersedes auto-stage's stale one. This keeps the two hooks as independent entries, at the price of `git add` appearing in two scripts.
- **Move auto-stage to the `Stop` event.** `Stop` fires after the whole turn, so every `PostToolUse` formatter has already run by then. Staging there always catches the formatted bytes, and the race cannot happen. The trade is real. `Stop` hands you no `file_path`, so to stay precise you must read the files Claude touched from the transcript (`transcript_path`) rather than the payload, or fall back to `git add -A` and lose the "leave my hand-edits alone" split. Staging also becomes one batch per turn instead of one file per edit.
- **Keep it a separate hook and accept the window.** Simplest to write and fine if your formatter is a no-op most of the time, but know that on a formatted file you may need a second edit (or a manual `git add`) to catch the formatting diff. This is what the shipped solution does, on purpose, so you feel the race before you decide how to close it.

Whichever you choose, do not pretend the ordering is deterministic when it is not. That assumption is the single most common reason this hook "sometimes leaves the file dirty."

---

## Why this hook must not interfere

The lecture's `PreToolUse` hook and this one sit on opposite sides of a line, and the line is the point of the exercise.

| Hook | Event | Its job | So its exit code... |
|------|-------|---------|---------------------|
| block-secrets | `PreToolUse` | **gate** an action | *should* stop the tool when the rule says no |
| format-and-test | `PostToolUse` | react after the fact | reports, does not gate |
| auto-stage | `PostToolUse` | **react** to an edit | must never turn a git hiccup into a block |

A `PreToolUse` hook is *supposed* to be able to say no. That is how it protects the migrations. Carry that instinct into your auto-stage hook and you punish Claude for a git problem that has nothing to do with the code. The `PostToolUse` exit-code contract is specific, and it is why:

| Exit code | What Claude Code does with it |
|-----------|-------------------------------|
| `0` | success; stdout is shown only in verbose/debug, the turn continues |
| `2` | **blocking**; stderr is fed back to Claude as something to address |
| other non-zero | non-blocking error; stderr goes to you, the turn continues |

The trap is exit `2`. A `git add` that fails and lets its non-zero status become the process exit, combined with a hook that returns `2`, injects a git error into the conversation on **every single edit**: the file was git-ignored, you were mid-rebase, the path sat outside the repo. None of that is Claude's to fix, but it will try, and it will drift off the task.

So the rule for this hook is blunt: **return success unconditionally.** End the command so its exit status is `0` whether the `git add` worked or not, and silence git's own chatter. If the stage fails, the outcome you want is an unstaged file you can add by hand, never a stream of errors nagging Claude. Keep the work cheap, too. This runs after every edit, so it is not the place for anything slow.

Concretely, the command ends in a way that swallows failure. `git add ... ; exit 0` does it. So does `git add ... || true`. Add `2>/dev/null` to keep git's warnings off the wire. What you must not do is let a failed `git add`, a missing file, or an unset variable become the process's exit status.

---

## Where it lives

Register the entry alongside your existing hooks, in the same `hooks.json` (or `settings.json`) where the lecture set lives. You already have a `PostToolUse` block for format-and-test, so this is a second object under the same event key, next to the one you have. Keeping all the repo's hooks in one place is what makes the plugin bundle clean in Section 12, and it means this hook ships with the rest instead of as a loose local tweak.

Here is the shape, shown next to the lecture's formatter so the two-entries-one-event layout is concrete. Note the lecture hooks do not inline a command string; each points at a script under `.claude/hooks/` with `command: "bash"` and the path in `args`, interpolated from `${CLAUDE_PROJECT_DIR}`. Follow that convention so your hook sits next to the others cleanly:

```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Edit|Write|MultiEdit",
        "hooks": [
          { "type": "command", "command": "bash",
            "args": ["${CLAUDE_PROJECT_DIR}/.claude/hooks/format-code.sh"] }
        ]
      },
      {
        "matcher": "Edit|Write|MultiEdit",
        "hooks": [
          { "type": "command", "command": "bash",
            "args": ["${CLAUDE_PROJECT_DIR}/.claude/hooks/auto-stage.sh"] }
        ]
      }
    ]
  }
}
```

Because the lecture's `PostToolUse` formatter lives in `format-code.sh`, your auto-stage logic belongs in its own `.claude/hooks/auto-stage.sh` rather than crammed into JSON as a one-liner.

If you take the robust path from the ordering section, there is no second entry at all: you append the `git add` to the end of the formatter's own script (`format-code.sh`) so order is guaranteed. Either layout is valid. The two-entry version is easier to read; the folded version is race-free.

Two placement notes. Put the hook where the lecture hooks already live, the `.claude/` that `${CLAUDE_PROJECT_DIR}` points at, which for this course is `section-05/end/.claude/`. Do not reach for the git-root `.claude/` two levels up: in this repo the git root is the whole course tree and its `.claude/` holds only `settings.local.json`, no hooks. The rule of thumb is "the same `.claude/` as the other hooks," not "the git root," because here the project root the CLI loads and the git root are not the same directory. And unlike the lecture's export-style hooks, this one writes nothing to disk of its own, so there is nothing new to add to `.gitignore`. It only moves Claude's edits into the git index.

---

## If you're comfortable, do this

Five steps. Skip the rest if it works on the first try.

0. Add the `PostToolUse` auto-stage command next to your lecture hooks, `type: command`, matcher `Edit|Write|MultiEdit`. Decide up front: separate entry, or folded into the formatter's command to beat the race.
1. Point the command at a script (or write it inline) that reads stdin, pulls `tool_input.file_path` with `jq`, guards for empty, and runs `git add` on it. Silence git and end on `exit 0`.
2. Nothing to `.gitignore` here. The hook stages files, it does not create any.
3. Ask Claude to edit a file. Confirm the file shows up staged in `git status` and the edit itself completed normally.
4. Edit a file the formatter will touch, and confirm the staged copy is the *formatted* one, not left dirty. Then edit a second file and confirm your own unstaged changes were left alone.

**Done when**: Claude edits a file and that file appears in the git index without you running anything, the edit still completes normally, and a second edit stages the second file without disturbing changes you made by hand. The hook exits `0` on every path, so a failed `git add` could never surface as an error to Claude. Bonus one, you also skip the stage when `tool_response.success` is false, so the index only tracks edits that landed. Bonus two, you handled the formatter race so a formatted file is never left both staged and dirty.

---

## Full path

Only if the fast path did not just work.

### Step 0: Register and write the hook

This is the exercise. Everything after it just runs and checks what you build here, so build it in small moves and know the trap in each.

**0.1 Add the entry.** Open the `hooks.json` (or `settings.json`) that holds your lecture hooks and add a `PostToolUse` object using the shape in "Where it lives" above. Set `matcher` to `Edit|Write|MultiEdit` and `type` to `command`. Keep the `command` short: a hook like this is a one-liner or a pointer to a small script, not a program. Then confirm the config still parses. A single trailing comma will silently disable *every* hook in the file, not just the new one, so re-open it and check the JSON is valid before you trust the test in 0.4.

**0.2 See the payload before you parse it.** Do not guess the field name. Point the command at a throwaway that captures its stdin:

```bash
cat > /tmp/posttooluse.json
```

Ask Claude to edit any file, then read `/tmp/posttooluse.json`. Confirm the path lives at exactly `.tool_input.file_path` and that its value points at the file just written. This is also where you would confirm `.tool_response.success` exists if you want the bonus guard. Now you know what you are staging and where the path comes from.

Shortcut: you can skip the capture entirely here. The lecture's `format-code.sh` already reads `.tool_input.file_path` from this same event, so the field name is confirmed by a hook that works. Copy its `input=$(cat)` / `jq -r '.tool_input.file_path // empty'` opening and you are parsing the payload the same proven way.

**0.3 Write the command.** Read stdin, extract the path with `jq -r`, guard for an empty value, and stage. The reference one-liner:

```bash
#!/usr/bin/env bash
set -euo pipefail

input=$(cat)
file=$(printf '%s' "$input" | jq -r '.tool_input.file_path // empty')
[ -z "$file" ] && exit 0

git add -- "$file" 2>/dev/null || true

exit 0
```

Every line earns its place. `input=$(cat)` reads the whole payload once so you can pull more than one field from it. `printf '%s' "$input"` feeds it to `jq` without `echo`'s escaping surprises, matching the lecture's `format-code.sh`. `jq -r '.tool_input.file_path // empty'` gives you the raw path, or an empty string when the field is absent, so a tool with no `file_path` does not hand you the literal `null`. The `[ -z "$file" ] && exit 0` guard means a call that slips past the matcher stages nothing instead of erroring. `git add -- "$file"` uses `--` so a path is always treated as a filename, never a flag, and `2>/dev/null` keeps a git complaint off the wire. The `|| true` matters once you add `set -euo pipefail` like the other hooks: without it, a failed `git add` under `set -e` would abort the script before the final line. And `exit 0` is the guarantee from "Why this hook must not interfere": whatever happened above, the hook reports success and the turn continues with no git error injected.

If you would rather not carry a multi-line body inside JSON, move these lines into `.claude/hooks/auto-stage.sh`, make it executable (`chmod +x`), and set the command to that path. The behavior is identical; the config stays readable.

**0.4 Handle the formatter race (the move that separates this from a toy).** Decide now, not after it bites. If you keep auto-stage as a separate entry, know that it may stage the pre-format bytes and leave the file dirty (see "Matcher and ordering"). To make it race-free, do not add a second entry at all: append the stage to the *end* of the existing format-and-test command, so within one script the formatter runs first and `git add` runs last. Whichever you pick, write down which one, because "sometimes the file is left dirty" is exactly this decision made by accident.

**0.5 Bonus: only stage what landed.** If you want the index to track only successful writes, add a guard on the response before staging:

```bash
ok=$(printf '%s' "$input" | jq -r '.tool_response.success // "true"')
[ "$ok" != "true" ] && exit 0
```

Default it to `true` so a tool whose response has no `success` field is still staged. This is optional. `git add` on an unchanged file is harmless, so a plain unconditional stage is already correct.

Confirm the hook is loaded before you lean on it. Trigger a small edit and check `git status`, or run Claude with hook debugging on if your setup supports it. A saved hooks file is picked up without a restart.

### Step 1: Trigger an edit and watch it stage

Ask Claude to make any file change. Two things must both be true afterward:

- The edited file shows up under "Changes to be committed" in `git status`, staged without you running `git add`.
- The edit itself actually completed. Claude wrote the file and kept going, it did not error or stall.

Editing through Claude rather than staging by hand is the point. It is the same code path every future edit will take, and you control when it happens. Run `git diff --staged` and confirm the staged content is the change Claude just made.

### Step 2: Prove it survives the formatter

Ask Claude to edit a file your lecture formatter will reformat (introduce a style the formatter fixes, e.g. odd spacing, if that is safe in your repo). Then check `git status`:

- If the file is **staged and clean**, your ordering is right: the formatter ran, then the stage caught the formatted bytes.
- If the file is **both staged and modified**, the race bit you: auto-stage caught the pre-format version and the formatter dirtied it afterward. Go back to 0.4 and fold the `git add` into the formatter's command so order is guaranteed.

This is the check the fast path's "Bonus two" is about, and it is the one most people skip until it confuses them at commit time.

### Step 3: Prove it scopes, and leaves your work alone

Make an edit of your own to a different file by hand, without going through Claude, and do not stage it. Then ask Claude to edit a second file. You should now see Claude's file staged and your hand-edited file still sitting unstaged. That separation is the whole reason the hook stages the specific file from the payload instead of running a blanket `git add -A`.

There is nothing to add to `.gitignore` for this exercise. The hook produces no files of its own, so `git status` stays clean except for the edits themselves.

---

## Troubleshooting

### The hook never fires

The event name or the matcher is wrong. Confirm the entry is registered under `PostToolUse` exactly (case matters) and that the matcher is `Edit|Write|MultiEdit`, not empty and not a tool name spelled differently. Check it is in the `.claude/settings.json` Claude actually loads, the same one holding the lecture hooks (here `section-05/end/.claude/`), not the git-root `.claude/` or `app/`. A stray trailing comma or brace elsewhere in the JSON disables the whole hooks file, so validate it (`jq . .claude/settings.json` or your plugin's `hooks.json`). And trigger it by asking Claude to edit a file: a hand edit is not a tool call and fires nothing.

### The file is not staged after an edit

Either the command failed silently, or it staged a path git does not track. Run the same command by hand, feeding it the captured payload:

```bash
cat /tmp/posttooluse.json | bash .claude/hooks/auto-stage.sh
git status
```

Watch its output and errors. Confirm the file is inside this repo's working copy, is not git-ignored, and that you are reading `git status` in the same repo the hook staged into. Remember the hook runs with the project as its working directory, so a relative path resolves from the repo root.

### `tool_input.file_path` is empty, or `jq` returns `null`

You are not parsing the stdin payload, or you have the key path wrong. Echo stdin to a scratch file inside the hook (`cat > /tmp/posttooluse.json`) and inspect it. Confirm the path is exactly `.tool_input.file_path`. If `jq` prints the literal `null`, either the key is off or the tool that fired carries no file (your matcher is too wide). If the tool is a notebook edit, the field is `notebook_path`, not `file_path`, which is one reason to keep `NotebookEdit` out of the matcher. Use `// empty` so an absent field becomes an empty string your guard can catch.

### `git add` fails on a git-ignored file

```
The following paths are ignored by one of your .gitignore files:
```

Expected, and already handled if you kept `2>/dev/null ; exit 0`. A git-ignored file should not be staged, so let the `git add` no-op and move on. Do not reach for `git add --force` to "fix" it: forcing an ignored file into the index is almost always wrong, and it is not this hook's job to override `.gitignore`.

### Claude starts reporting git errors on every edit

Your hook is exiting non-zero, most likely `2`. A `PostToolUse` hook's exit `2` feeds stderr back to Claude, so a `git add` that fails on every edit becomes a stream of noise that pulls Claude off the task. Make the command exit `0` unconditionally (`... ; exit 0`, or `git add ... || true`) and silence git with `2>/dev/null`. Re-read "Why this hook must not interfere": this hook reacts to an edit, it does not gate one, so a git problem is never Claude's problem to fix.

### The file ends up both staged and modified

The formatter race from "Matcher and ordering." Your separate auto-stage hook caught the pre-format bytes, then the format-and-test hook rewrote the file. Matching hooks for one event are not ordered against each other, so listing yours second does not make it run second. Fold the `git add` onto the end of the formatter's own command so the order is guaranteed within one script, then re-run the Step 2 check.

### The hook stages files I edited myself

You are running a blanket `git add -A` or `git add .` instead of staging the one path from the payload. Stage only `.tool_input.file_path` so the index tracks exactly what Claude wrote and leaves your unstaged work untouched. That precision is the reason to read the path from stdin rather than sweeping the tree.

### Every tool call triggers a stage, even reads and Bash

Your matcher is empty, `*`, or too broad, so the hook fires on tools that never wrote a file. Scope it to `Edit|Write|MultiEdit`. Keep the empty-path guard as a backstop so any call that still slips through stages nothing instead of erroring.

### `jq: command not found`

The hook runs in a plain shell without your interactive `PATH`. Confirm `jq` is installed (`which jq`) and, if the hook still cannot find it, call it by absolute path or add its directory to the command. This is the same reason a hook that works when you paste it into your terminal can fail when Claude runs it.

### `fatal: not a git repository` in the hook output

The hook ran somewhere without a git repo, or the path pointed outside the working copy. Confirm you launched Claude Code from inside the repo, and that `file_path` resolves inside it. The `2>/dev/null ; exit 0` tail keeps this from ever reaching Claude, so the only symptom should be a file that did not stage, not a broken turn.

### A secret or `.env` file got staged

Two layers should stop this, and it is worth checking both. The lecture's `PreToolUse` block-secrets hook should refuse the *edit* in the first place, so auto-stage never sees the file. And `.gitignore` should keep secrets untracked, so `git add` no-ops on them. If a secret still reached the index, the edit got past block-secrets. Fix it there, in the gate, not by teaching the observer to special-case paths.

---

## You can now

Hook the `PostToolUse` event with a purpose-built entry, scope it with a matcher so it fires only where it should, read `tool_input.file_path` from the payload, and act on the exact file Claude changed without ever standing in Claude's way. You met the exit-code contract that makes exit `2` a trap for a reactor, and the fact that matching hooks for one event race each other, which is why a naive stage-after-format leaves files dirty. Your git index now keeps step with the session on its own, which turns commit-time review from a scavenger hunt into reading one staged set. More than the convenience, you have internalized the split the rest of the hook events turn on: some hooks are there to gate an action and are allowed to say no, and some are there to react to one and must never do anything but succeed.

---

## Summary

You have added:

- **A `PostToolUse` `command` hook**: registered next to your lecture hooks with an `Edit|Write|MultiEdit` matcher, it reads `tool_input.file_path` from the event payload and runs `git add` on the file Claude just wrote.
- **A non-interfering reactor**: it exits `0` unconditionally and silences git, so a failed stage never surfaces as an error into the conversation.
- **A race-aware design**: because `PostToolUse` hooks are not ordered against each other, the stage is folded after the formatter (or the trade is stated on purpose), so a formatted file is never left both staged and dirty.
- **A precise stage**: only the file from the payload lands in the index, so Claude's work stays cleanly separated from the changes you made by hand.

---

## What's next

In Section 6 I build the `convention-reviewer` subagent: one read-only, worktree-isolated reviewer that checks any change in the repo against the project's conventions. Your solo is repo-global too, so it is a hint-model task: build that same `convention-reviewer` yourself. You do not build a reviewer per feature. One repo, one reviewer.

---

## Reference

Your on-screen safety-net hooks from S5L7 are the reference for the mechanics: how a `command` hook is registered, how it reads stdin, and how exit codes are interpreted. The difference here is the intent (react and stage, not gate), the matcher (scoped to the file-writing tools), and the ordering interaction with the formatter that shares the event.

Three things worth having in front of you as you build:

- **The payload.** `PostToolUse` delivers `session_id`, `transcript_path`, `cwd`, `hook_event_name`, `tool_name`, `tool_input`, and `tool_response` on stdin. `tool_input.file_path` is the only field you strictly need. `NotebookEdit` is the exception that uses `notebook_path`, which is why you keep it out of the matcher.
- **The matcher.** A regex against `tool_name`, case-sensitive. `Edit|Write|MultiEdit` is the scope; empty or `*` fires on everything.
- **The exit-code contract.** `0` succeeds and the turn continues, `2` blocks and feeds stderr to Claude, other non-zero reports to you without blocking. A `PreToolUse` hook's non-zero exit is a feature that gates the tool. A `PostToolUse` auto-stage hook's non-zero exit is a bug that nags Claude on every edit. Same mechanism, opposite intent. Let that difference, not habit, set your exit code.
