# Claude Code Deep Dive - Section 6: Subagents Prompts

These are the prompts run on screen during this section's lessons. Copy them as they are into Claude Code and follow along.

## L2 - Built-in Subagents

Asks a broad codebase question with no mention of subagents, so Claude decides on its own to delegate the search to Explore.

```
How does currency conversion flow from the API to the React frontend?
```

## L3 - How Subagents Work

### Run one delegation on the expense tracker

Sends a single scoped search into a subagent, so the file listing never lands in the main context.

```
Explore the Expenses feature and tell me which files touch currency conversion.
```

### Chain subagents

Runs two subagents in sequence, feeding the reviewer's findings into the optimizer.

```
Use the code-reviewer subagent to find performance issues, then use the optimizer subagent to fix them
```

### Parallel subagents

Fans the same review out across two features at once, so both run concurrently instead of one after the other.

```
Find performance issues at Expenses and Subscriptions features in parallel using separate subagents
```

## L4 - Creating Custom Subagents

### Add feature

Kicks off a feature that is vague enough to produce a plan worth challenging.

```
Add a monthly-budget feature to the Expenses slice.
```

### Review plan using sub agent

Hands the plan to the plan-adversary agent so a second, independent pass attacks it before any code is written.

```
Use the plan-adversary agent to review this plan
```
