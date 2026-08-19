# Deep Dive: Claude Code

Welcome to the ["Deep Dive: Claude Code"](https://dometrain.com/course/deep-dive-claude-code/?ref=dometrain-github) course on Dometrain!

This course takes you past everyday use of Claude Code and into the machinery: how it actually works under the hood, how to configure it for a whole team, and how to extend it with agent skills, hooks, subagents, and MCP servers. It then covers locking it down with permissions and sandboxing, handing work off with autonomous and remote workflows, running it headless in CI, and finally packaging everything you built as a plugin.

It assumes you already use Claude Code day to day. If you are starting from zero, take [Getting Started with Claude Code](https://dometrain.com/course/getting-started-claude-code/?ref=dometrain-github) first.

This repository holds the source code for the course, so you can follow along. The **main branch** is the most up-to-date version, with the latest fixes and improvements.

## The example project

The course is built around a small but realistic **Expense Tracker**: a .NET 10 Minimal API backend, a React 19 and Vite frontend, and PostgreSQL for storage. Integration tests run against a throwaway Postgres container via Testcontainers.

It grows section by section. Expenses is what the instructor builds on screen. Subscriptions is the parallel area you build yourself, in its own files, so the two never collide.

## Getting started

### 1. Install what you need

The course teaches Claude Code against a .NET and React codebase, so you need both toolchains plus Claude Code itself.

| Tool | Version | Where to get it |
| --- | --- | --- |
| Git | any current version | [git-scm.com/downloads](https://git-scm.com/downloads) |
| Claude Code | any current version | [Install and authenticate](https://code.claude.com/docs/en/setup) |
| .NET SDK | any 10.x, 10.0.100 or newer | [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) |
| Node.js | 24 (LTS) | [nodejs.org/en/download](https://nodejs.org/en/download) |
| pnpm | 11.0.8 | [pnpm.io/installation](https://pnpm.io/installation) |
| Docker | any current version | [docker.com/get-started](https://www.docker.com/get-started/) |

Claude Code needs a paid plan. Taking this course on your own, that means **Claude Pro or Max** ([pricing](https://claude.com/pricing)). If your employer provides it, **Claude for Teams or Enterprise** logs in the same way. The free plan does not include Claude Code.

Organizations that bill through the API can use a **Claude Console** account or a cloud provider instead, but both need an admin to invite you first. See [Authentication](https://code.claude.com/docs/en/authentication) if that is your setup.

After installing, run `claude` once and follow the browser prompt to log in.

### 2. Verify your environment

Paste this before you start. Every line should print a version, not an error.

```bash
git --version
claude --version
dotnet --info | head -4
node --version
pnpm --version
docker compose version
```

`claude doctor` gives a fuller read on the Claude Code side, including settings-file errors, without starting a session. If any of the others fail, fix that before the first lesson. Nothing here is worth debugging mid-lecture.

The toolchain is pinned in every snapshot: `global.json` for the .NET SDK, `app/web/.nvmrc` for Node, and a `packageManager` field in `app/web/package.json` for pnpm. `dotnet` and `nvm use` pick the right versions for you, and pnpm 10 and newer fetch the pinned version themselves. The course was recorded against SDK 10.0.203, Node 24, and pnpm 11.0.8. The SDK floor in `global.json` is deliberately lower, so any .NET 10 install works rather than only the exact band used on screen.

### 3. Pick your mode

Two ways to take this course, and the choice matters before Section 4.

> **Following along only?** Use the snapshots. Open any `section-NN/start` or `end`, run it, throw it away, move on. Nothing carries over and nothing is at risk.
>
> **Doing the solo exercises?** Create one long-lived working copy first, before Section 4. Copy a snapshot's contents into a folder of your own, make it a git repo, and never reset it. Your Subscriptions area, `.claude/` config, hooks, and reviewer accumulate there across sections. Doing solos inside a disposable snapshot loses the work the moment you move on.

[Working across sections](docs/exercises/README.md#working-across-sections) covers how to keep that copy in sync.

### 4. Run it

The day-one baseline is [`section-02/start`](section-02/start).

```bash
git clone <this-repo>
cd dometrain-claude-code-deep-dive/section-02/start
```

Then follow that folder's [README](section-02/start/README.md), which walks the three startup steps with a success signal after each. Every `start/` and `end/` runs exactly the same way.

## How this repo is organized

One folder per course section, each with a README describing what that section covers, a `start/` and an `end/` copy of the code, and the prompts used on screen:

```
README.md            this file
docs/
  exercises/         the solo exercise guides, one per section
section-NN/
  README.md          what the section covers and what end/ added
  Prompts.md         the prompts run on screen during that section's lessons
  start/             the code as it is when the section begins
  end/               the code as it is when the section ends
```

Each `start/` and `end/` carries its own `README.md` covering how to run that copy, and nothing else. It has no links pointing out of the folder, so you can copy the contents over your working copy and everything still resolves. Everything about the *course* lives one level up, in `section-NN/README.md`.

Inside a snapshot folder you will find:

```
README.md            how to run this copy
CLAUDE.md            project memory, with @ imports to app/api and app/web
global.json          pins the .NET SDK; app/web/.nvmrc pins Node
.claude/             rules, skills, hooks, agents, settings, from Section 3 on
.mcp.json            MCP server registration, from Section 7 on
expenses.http        runnable requests for every endpoint, from Section 4 on
plugins/             the packaged plugins, section-11/end only
```

> **Spoiler warning.** The `end/` folders include the completed solo exercises, not just the on-screen work. [`section-04/end`](section-04/end) has the Subscriptions slice you are meant to scaffold yourself, and [`section-09/end`](section-09/end) has the status endpoint. Open them after you have tried the solo, not before.

## Sections

| Section | Overview | Start | End | Prompts |
| --- | --- | --- | --- | --- |
| 2. How Claude Code Works | [README](section-02/README.md) | [start](section-02/start) | [end](section-02/end) | [Prompts.md](section-02/Prompts.md) |
| 3. Configuring It at Scale | [README](section-03/README.md) | [start](section-03/start) | [end](section-03/end) | [Prompts.md](section-03/Prompts.md) |
| 4. Agent Skills | [README](section-04/README.md) | [start](section-04/start) | [end](section-04/end) | [Prompts.md](section-04/Prompts.md) |
| 5. Hooks | [README](section-05/README.md) | [start](section-05/start) | [end](section-05/end) | [Prompts.md](section-05/Prompts.md) |
| 6. Subagents | [README](section-06/README.md) | [start](section-06/start) | [end](section-06/end) | [Prompts.md](section-06/Prompts.md) |
| 7. MCP Servers | [README](section-07/README.md) | [start](section-07/start) | [end](section-07/end) | n/a |
| 8. Permissions, Sandboxing, and Security | [README](section-08/README.md) | [start](section-08/start) | [end](section-08/end) | n/a |
| 9. Autonomous and Remote Workflows | [README](section-09/README.md) | [start](section-09/start) | [end](section-09/end) | [Prompts.md](section-09/Prompts.md) |
| 10. Headless Mode and CI/CD | [README](section-10/README.md) | [start](section-10/start) | [end](section-10/end) | [Prompts.md](section-10/Prompts.md) |
| 11. Plugins | [README](section-11/README.md) | [start](section-11/start) | [end](section-11/end) | [Prompts.md](section-11/Prompts.md) |

Sections 1 and 12 are the Welcome and the Conclusion. Neither ships code.

## The API

The HTTP contract is documented on screen during Section 9, so it lives in that section's snapshot and is carried forward: [`section-09/end/README.md`](section-09/end/README.md#api). Routes, wire format, ordering, and the exact shape of every `400`. From Section 4 on, each snapshot also ships an `expenses.http` with a runnable request per endpoint.

## The plugin

`cc-deep-dive-devkit` is built across the course: a skill in Section 4, hooks, a subagent, and MCP config in later sections, and packaged in Section 11. The finished version, plus a second `thinking-tools` plugin and the marketplace manifest, lives in [`section-11/end/plugins`](section-11/end/plugins). Install it from that folder with `/plugin marketplace add ./plugins/`, then `/plugin install cc-deep-dive-devkit@cc-deep-dive`.

## Prompt banks

Every prompt run on screen lives in that section's `Prompts.md`, grouped by lesson and numbered to match the lecture. Each one carries a line saying what it does, so you can tell them apart before running anything. Copy them as they are into Claude Code and follow along.

Sections 7 and 8 have no prompt bank. Their work is configuration rather than conversation, so the artifacts to read are `.mcp.json` for MCP Servers and `.claude/settings.json` for Permissions, both in the section's `end/` folder.

## Solo exercises

Alongside the on-screen build you build a parallel **Subscriptions** track yourself, section by section. Subscriptions lives in its own files, so your work never collides with the Expenses code built on screen. The guides live in [`docs/exercises`](docs/exercises/README.md), one per section.

Your solo track is one continuous body of work that has to survive from Section 4 to the end, so keep a single working copy rather than starting fresh from each `start/` folder. [Working across sections](docs/exercises/README.md#working-across-sections) explains the two ways to stay in sync and when to reach for each.

## Resources

The official documentation for each section's topic. Worth reading alongside the lectures, and worth re-reading after, because these pages change faster than any course can.

| Section | Documentation |
| --- | --- |
| 2. How Claude Code Works | [Overview](https://code.claude.com/docs/en/overview) |
| 3. Configuring It at Scale | [Memory and CLAUDE.md](https://code.claude.com/docs/en/memory), [Settings](https://code.claude.com/docs/en/settings) |
| 4. Agent Skills | [Agent Skills](https://code.claude.com/docs/en/skills) |
| 5. Hooks | [Hooks guide](https://code.claude.com/docs/en/hooks-guide) |
| 6. Subagents | [Subagents](https://code.claude.com/docs/en/sub-agents) |
| 7. MCP Servers | [MCP](https://code.claude.com/docs/en/mcp) |
| 8. Permissions, Sandboxing, and Security | [Permissions](https://code.claude.com/docs/en/permissions), [Sandboxing](https://code.claude.com/docs/en/sandboxing) |
| 9. Autonomous and Remote Workflows | [Remote Control](https://code.claude.com/docs/en/remote-control), [Workflows](https://code.claude.com/docs/en/workflows) |
| 10. Headless Mode and CI/CD | [Headless mode](https://code.claude.com/docs/en/headless), [GitHub Actions](https://code.claude.com/docs/en/github-actions) |
| 11. Plugins | [Plugins](https://code.claude.com/docs/en/plugins) |

Also useful: the [full documentation index](https://code.claude.com/docs/en/) and the [Claude Code repository](https://github.com/anthropics/claude-code) for issues and release notes.
