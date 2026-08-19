# Claude Code Deep Dive - Section 5: Hooks Prompts

These are the prompts run on screen during this section's lessons. Copy them as they are into Claude Code and follow along.

## L3 - Pre-Tool Hooks: Blocking and Validating

### Create the hook

Builds a PreToolUse hook that inspects Bash commands and denies destructive git operations before they run.

```
Create a PreToolUse hook to block any destructive git command
```

### Trigger the hook

Asks for exactly the kind of command the hook guards against, so you can watch it get denied.

```
Please push a git reset
```

## L4 - Post-Tool Hooks: Auto-Formatting and Logging

### Create Hook to format

Installs Prettier and adds a PostToolUse hook that formats backend and web code after edits.

```
Install Prettier and then create a PostToolUse hook to format the dotnet code using dotnet format command and oxlint to format the web app code.
```

### Add Note field to expenses

Adds an optional note field to expenses across the entity, migration, API, and tests.

```
Add an optional note field to expenses: update the entity, create a new migration, and update the API and tests.
```
