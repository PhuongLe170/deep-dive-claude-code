# S11 Solo - Build Your Own Plugin From Scratch

> Section 11, Plugins. Solo. Same shared project. Hints and troubleshooting, not a full hand-hold.

## Mission

In the lecture I packaged what the course had already built into `cc-deep-dive-devkit`: the `http-samples` skill, and a hook set that logs every Bash command Claude runs. Two components, both gathered rather than written, and both welded to this project.

Your plugin holds one thing, and it isn't welded to anything.

You'll build it from an empty folder: pick the name, write the manifest, put a single skill in it, load it, then add it to the repo's marketplace and install it for real. Same arc as the lecture with half the parts.

The skill is `shape-idea`, and it's below, ready to paste. It turns a half-baked idea into a written spec by interviewing you about it. It has nothing to do with expenses or subscriptions, and that's the point: **it's the first thing you've built in this course that works in any repo you open.**

By the end you'll have installed a plugin you wrote, the repo's marketplace will list two, and you'll own a tool you'll actually use next week.

**Learning Objectives**

- Choose a plugin name knowing it becomes the namespace on every invocation.
- Write a manifest from scratch and understand which field is doing the work.
- Take a plugin from an empty folder to a real project-scope install.
- See that one marketplace catalogues many plugins.

---

## Prerequisites

- S11L4 finished. `plugins/cc-deep-dive-devkit/` exists and is installed, and the repo's catalogue sits at `plugins/.claude-plugin/marketplace.json`.
- Nothing else. You're building from an empty folder, so there's no earlier artefact to carry forward.

---

## What we're solving

The lecture built a plugin by gathering things that already existed. That's the migration path, and it's the common one. It's also the one where every interesting decision was already made: the name was chosen, the manifest was written, the components had been working for weeks.

Building from empty is the other path, and it's the one you'll actually take on Monday. It surfaces three things the migration hides.

**The name is the only field that matters, and you can't take it back.** `name` in the manifest becomes the namespace on every invocation forever. Pick something long and you'll type it a thousand times. Pick something vague and you won't know what it does in six months. The lecture handed you `cc-deep-dive-devkit` already decided. This time it's yours, and this time the plugin isn't tied to a project, so the name has to say what's inside rather than where it lives.

**A plugin is mostly an empty folder with a manifest.** Everything else is optional. That's worth feeling directly, because it's the fact that makes plugins worth reaching for. The ceremony is about ten lines of JSON.

**One marketplace catalogues many plugins.** The repo already has a catalogue with one entry in it. Real team repos have several. Adding yours as a second entry is three lines, and it's the difference between thinking of a marketplace as "the thing that wraps my plugin" and understanding it as a list.

There's a fourth thing, and it only shows up because of what's inside this particular plugin. Everything you've built so far in this course was welded to the expense tracker: a skill that scaffolds *this* project's endpoints, hooks that guard *this* project's migrations, an agent that knows *this* project's conventions. All useful, none portable. `shape-idea` is the opposite, and packaging it is how you find out that the plugin system was built for exactly this.

---

## The skill

Create this as `SKILL.md` inside your plugin. **The frontmatter is not optional**: a skill with no `description` never registers, and you'll spend twenty minutes debugging a manifest that was fine all along.

```markdown
---
name: shape-idea
description: Turn a rough idea into a written spec by interviewing the user about it. Use when someone has a half-formed idea, is unsure how to scope something, or asks for help writing a spec or design doc.
---

# Shape Idea

Your job is to turn a rough idea into a spec the user could hand to someone else.

You are not collecting requirements. You are finding the two or three things that
are genuinely undecided, and leaving everything else alone. Most half-formed ideas
are 80% settled and 20% unexamined. Find the 20%.

## 1. Read the repo before you ask anything

Use Glob and Grep to work out what this project is, what it already does, and which
files the idea touches. Do it silently and do not report on it.

**Never ask a question the codebase already answers.** If they say "add caching" and
there is already a cache layer, the question is not "should we cache?" but "why is
the one you have not enough?"

Skip this if there is no real codebase open.

## 2. Name the frame, do not offer a menu

Decide from what they said whether this is mostly a **product** question (what it
should do, who for, how much) or a **technical** one (how it should be built, what
it costs, what it breaks). Say which you think it is in one line and let them
correct you.

Do not present it as a choice of modes. Guessing and being corrected is faster than
asking, and being wrong is useful information.

## 3. Interview

Use AskUserQuestion. One or two questions at a time, never more.

**The test for whether a question is worth asking: would a different answer change
what you write in the spec?** If not, do not ask it. Questions that only confirm
what you already assume are padding, and they make the interview feel like a form.

Aim to finish in six questions or fewer. If you are past ten you have lost the
thread, so write what you have and mark the rest unresolved.

**Push back at least once.** Somewhere in this conversation there is an assumption
that has not been examined: a user who may not exist, a constraint that is really a
preference, a solved problem being solved again. Say so plainly, once, and move on
when they answer. A thinking partner who agrees with everything is just a
transcriptionist.

## 4. Stop

Stop when you could write the spec yourself and be mostly right, or when new answers
stop changing anything. Running out of questions is not the same as being finished,
and being finished early is fine.

## 5. Write it

Keep it short enough to read in one go.

**For a product idea**

- **The problem**, in two sentences, from the user's side rather than the solution's
- **Who it is for**, specifically enough to rule someone out
- **What it does**, as a short list
- **What it deliberately does not do**, and why
- **What we did not decide**

**For a technical idea**

- **The goal**, in two sentences
- **Constraints** that are real, separated from preferences that are not
- **The approach**, and what makes it fit this codebase rather than any codebase
- **What we rejected**, and the reason
- **What we did not decide**

**The last section is mandatory in both, and it is the point.** A spec written from
a rough idea always has holes. Naming them is what makes it honest and what makes it
useful three weeks later. If there are genuinely none, say so explicitly rather than
dropping the heading.
```

Three things worth noticing about it, because they're all new to you.

**It restricts nothing.** No `allowed-tools`, no `model`, no `argument-hint`. Your `new-endpoint` skill needed all three because it writes files. This one reads and asks questions, so the frontmatter is two lines.

**It's a different kind of skill entirely.** `new-endpoint` is a scaffolder and `convention-reviewer` is a checker. This one runs a conversation and produces prose. Same file format, completely different job. That range is most of why skills are worth writing.

**Most of it is instructions about restraint, not about questions.** "Never ask what the codebase answers." "Would a different answer change the spec?" "Six questions or fewer." "Push back at least once." Writing a skill that *does* something is easy. The work is telling it what not to do, because the default behaviour of a model asked to interview you is to interview you forever and agree with everything. Read those lines again when you write your next skill.

Change it. Cut the sections you don't want, add a mode for the kind of work you actually do, adjust the tone paragraph until it sounds like a colleague you'd trust. It's yours now, and a skill you haven't edited is a skill you don't understand yet.

---

## The task

Build a new plugin in the repo with `shape-idea` in it, and install it for real.

**Hint**: four moves, and the first one is the one to slow down on.

1. **Name it.** Not after this project, because the plugin isn't about this project. Say the invocation out loud before you commit: `/<your-plugin>:shape-idea`. Something like `thinking-tools` works. So does your initials. It's yours, and you'll be typing it for a long time.
2. **The manifest.** `plugins/<your-plugin>/.claude-plugin/plugin.json`. Copy the shape from `cc-deep-dive-devkit`. `name` is the only required field.
3. **The skill.** `plugins/<your-plugin>/skills/shape-idea/SKILL.md`, pasted from above. Note where it goes: `skills/` at the **plugin root**, beside `.claude-plugin/`, never inside it.
4. **Catalogue, then install.** Add a second entry to `plugins/.claude-plugin/marketplace.json`. Load with `claude --plugin-dir ./plugins/<your-plugin>` while you're getting it working, then refresh the listing, install at **project** scope, and run it. The refresh is `/plugin marketplace update cc-deep-dive` inside a session, or `claude plugin marketplace update cc-deep-dive` from the terminal if you'd rather stay there.

**Project scope is not the default.** Both installers hand you user scope unless you say otherwise, so say otherwise:

```bash
claude plugin install <your-plugin>@cc-deep-dive --scope project
```

From `/plugin` inside a session, pick **project** when it asks you where. Get this wrong and nothing appears in the repo, which reads exactly like a failed install.

**Done when**: `/<your-plugin>:shape-idea` runs from a real project-scope install and actually interviews you, `/plugin` lists two plugins from the `cc-deep-dive` marketplace, and `.claude/settings.json` has a `<your-plugin>@cc-deep-dive` line under `enabledPlugins`, next to `cc-deep-dive-devkit@cc-deep-dive`. There will be more than two lines in there. The LSP plugins are already installed at project scope, so count the two that came from `cc-deep-dive` and ignore the rest.

**One last thing to notice, and it's the real close.** Look at what project scope bought you. The install went into the repo's `.claude/settings.json`, which is a tracked file, so the plugin you just wrote is now something you can commit and a teammate can pick up by cloning. That is what makes the marketplace entry worth having.

Now notice what it cost you. `shape-idea` has nothing to do with expenses, and it's sitting in the one repo where it's least useful. One flag moves it: `--scope user` puts it in `~/.claude/settings.json` and it follows you into every project you open. Leave it at project scope for now, because this is the course repo and that's where the exercise lives. Run the user-scope install on your own machine when you want it everywhere, and know that the only difference between the two is which settings file gets the line.

---

## Pointers if you get stuck

- **Name it before you make the folder.** The directory name and the manifest `name` don't have to match, but life is easier when they do.
- **The manifest is smaller than you expect.** `name` is required. `description`, `version` and `author` are useful and optional. Everything else in the reference is for later. Don't pad it.
- **Only the manifest goes in `.claude-plugin/`.** Your `skills/` folder sits at the plugin root, beside it. This is the number-one plugin bug and its symptom is a plugin that loads cleanly with nothing in it.
- **`--plugin-dir` first, install second.** Get it loading from your folder before you touch the catalogue. Debugging a manifest and a marketplace entry at the same time is twice the work for no reason.
- **`source` is relative to the catalogue, not the repo root.** `marketplace.json` lives in `plugins/.claude-plugin/`, so paths resolve from `plugins/`. The existing entry reads `"./cc-deep-dive-devkit"`, which means yours reads `"./<your-plugin>"`. Not `"./plugins/<your-plugin>"`.
- **Refresh the catalogue after you edit it**, with `/plugin marketplace update cc-deep-dive` in a session or `claude plugin marketplace update cc-deep-dive` from the terminal. Claude Code caches the marketplace listing. A new entry in the file doesn't reach the installer until the listing refreshes, and the error you get otherwise ("not found in the marketplace") looks like you got the name wrong.
- **Test the skill on something real.** Run it on a decision you're actually stuck on, not a made-up one. That's the test that matters, and it's the one this exercise was built to reach.

---

## Troubleshooting

### The skill doesn't appear at all

Nine times out of ten it's the frontmatter. A `SKILL.md` with no `description` never registers, and nothing warns you. Check the file starts with `---`, has `description:` on its own line, and closes with `---` before the body.

Then check the layout, then validate:

```bash
ls -a plugins/<your-plugin>        # expect .claude-plugin/ and skills/ side by side
claude plugin validate ./plugins/<your-plugin>
```

Use `-a`. `.claude-plugin` is a dotfile, so a plain `ls` hides it and you'll think your manifest folder is missing when it's sitting right there.

### `claude plugin validate` passes but nothing loads

Validation checks the manifest, not whether you put the components somewhere Claude Code will look. `skills/` belongs at the plugin root, not inside `.claude-plugin/`.

### The install says the plugin isn't in the marketplace

Your local copy of the catalogue is stale. Refresh it, then install:

```
/plugin marketplace update cc-deep-dive
```

From the terminal, same thing:

```bash
claude plugin marketplace update cc-deep-dive
```

If it still isn't found, check the `source` path in your new entry. It resolves from the folder holding `.claude-plugin/`, which is `plugins/`, not the repo root. So it's `"./<your-plugin>"`. While you're in there, check you added a comma after the first plugin's closing brace.

### Both plugins vanished from `/plugin`

Almost always a JSON error in `marketplace.json`, and the usual cause is a missing comma between the two entries. A malformed catalogue fails as a whole, which takes the lecture's plugin down with yours. Check the Errors tab, or:

```bash
python3 -m json.tool plugins/.claude-plugin/marketplace.json
```

### `/reload-plugins` says `0 skills`

Ignore that number and use `/reload-skills` instead. It's the one that reloads a skill folder and reports a count you can trust. The `/reload-plugins` summary only covers a plugin's `commands/` directory, so a plugin whose skills live in `skills/` reports zero even when everything loaded fine.

If you want proof the skill registered, ask Claude to list the skill names it can invoke. `claude plugin details` won't help you here: it only inspects installed plugins, so it can't see one you loaded with `--plugin-dir`, whatever its error message tells you to try.

### The skill loads but the interview goes sideways

**That's a skill problem, not a packaging problem, and it's worth separating them.** If `/<your-plugin>:shape-idea` is found and runs, your plugin is correct and finished. What happens next is the skill body's business: edit the instructions, tighten the tone section, cut a step. You'd do the same if it were sitting loose in `.claude/skills/`.

The most common fix is length. If it asks too many questions before writing anything, make Step 4 more decisive.

### The install said it worked but `.claude/settings.json` didn't change

You installed at user scope, which is the default. Check with `claude plugin list`, look at the `Scope:` line, and reinstall with `--scope project`.

### The skill runs under `--plugin-dir` but not after installing

Installing copies the plugin into a cache, so the installed copy is a snapshot of the folder at install time. If you edited after installing, the cache is stale. Either go back to `--plugin-dir` while you're still changing things, or reinstall.

### You want to undo the whole thing

Plugin state lives outside the repo, so `git checkout` won't remove an install:

```
/plugin uninstall <your-plugin>@cc-deep-dive
```

---

## You can now

Take a plugin from an empty folder to something installed, which is the move you'll repeat every time you write something worth keeping. The lecture's version looked like a big job because it had seven sections of material behind it. Stripped to one component, the actual work is a folder, ten lines of JSON, and a name you thought about for thirty seconds.

You also have `shape-idea`, installed in this repo and one `--scope user` away from being in every repo you open. Use it on the next thing you're not sure how to build. That's the first Claude Code extension you've made that isn't about expenses, and it won't be the last.

---

## Summary

You have built:

- **A plugin of your own**, from an empty folder, with a manifest whose `name` you chose knowing it becomes the namespace.
- **A second catalogue entry**: the repo's `marketplace.json` now lists two plugins, which is what a real team repo looks like.
- **A real project-scope install**: the line in the repo's tracked `.claude/settings.json` is what makes the marketplace entry mean something, and it's what a teammate gets by cloning.
- **A tool you'll keep**: `shape-idea` is portable by construction. One `--scope user` install and it works in any repo you open.

---

## Reference

`plugins/cc-deep-dive-devkit/` from S11L4 is your reference for the manifest shape and the folder layout. `plugins/.claude-plugin/marketplace.json` is your reference for the catalogue entry, and you're adding a sibling to what's already there.

The difference between the two plugins is worth noticing when you're done. Mine has two components, both pulled out of what the course had already built, and both welded to this project. Yours has one component that works anywhere. Both are plugins, and both install the same way. Only one of them is coming with you.
