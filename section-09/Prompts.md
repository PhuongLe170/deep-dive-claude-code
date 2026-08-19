# Claude Code Deep Dive - Section 9: Autonomous and Remote Workflows Prompts

These are the prompts run on screen during this section's lessons. Copy them as they are into Claude Code and follow along.

## L2 - Verifying Claude's Work

Runs the verify skill, which boots Postgres, the API, and the web app, then drives the browser through Playwright to confirm the flow actually works.

```
/verify can I add an expense through the browser?
```

## L3 - Dynamic Workflows

### Deep research a migration decision

Asks for a researched recommendation on trading EF Core for Dapper, with the codebase specifics supplied up front and a demand for evidence over vendor claims.

```
/deep-research We use EF Core 10 with Npgsql: a DbContext, five migrations, LINQ including a GroupBy for monthly totals, and Testcontainers integration tests. Is migrating to Dapper worth it here? Compare query translation, migrations, change tracking, and what the tests would become, with real performance evidence rather than vendor claims. Recommend for this codebase, and name what would have to change your answer.
```

### Run a multi-agent security audit

Fans an OWASP Top 10 audit out across agents, one per category, with a second agent confirming every finding before it reaches the report.

```
ultracode audit this codebase against the OWASP Top 10. One agent per category, scoped to app/api, so I can check the shape of the run before widening it. Every finding needs a file and line, the category it maps to, and a second agent confirming it is real before it reaches the summary. Name the categories that found nothing. By the end, write a report with all the findings.
```

## L4 - Goal-Driven Autonomy

### The vague goal

Gives the agent nothing to aim at, so you can see what an underspecified goal does to an autonomous run.

```
/goal make totals validation better
```

### The precise goal

Names the exact behaviour, the proof that it works, the regression guard, and a turn limit, so the run has a definition of done.

```
/goal GET /expenses/totals/monthly rejects a future month with a 400 and a ProblemDetails body, proven by running dotnet test with a new MonthlyTotalsTests case green in this session, with no existing test weakened, or stop after 15 turns.
```

## L5 - Driving Claude Remotely

### Take remote control of a session

Attaches to a running session from another device, so you can steer the work without sitting at the machine.

```
 /remote-control dometrain-cc-deep-dive-demo
```

### Hand the work to the cloud

Launches the task on Claude's infrastructure instead of your laptop, so it keeps running after you close the terminal.

```
 claude --cloud "Update the API README examples to include the monthly totals endpoint" 
```
