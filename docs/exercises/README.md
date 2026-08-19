# Solo Exercises

The parallel track for the Dometrain course "Deep Dive: Claude Code." Each section, the instructor builds a feature or config on the **Expenses** area (and, from Section 4, on the plugin). At the end of the section you build the equivalent yourself, solo, on the same shared repo.

Solo challenges come in two shapes:

- **Two-area (Subscriptions).** When the section's technique is an app feature, you build the **Subscriptions** equivalent of what the instructor built on Expenses. Subscriptions is a disjoint area (its own entity, endpoints, tests, and UI route), so your work never collides with the on-screen Expenses code.
- **Hint model (repo-global).** When the technique is a repo-wide config artifact (a rule, a hook, MCP config, a security policy, a plugin), the solo is a parallel task with a one-line hint and an acceptance criterion, not a second feature area.

Every guide follows the same shape: Mission, Learning Objectives, Prerequisites, What we're solving, a fast path with a **Done when**, a fallback path or pointers, Troubleshooting, and a short close. These ship with hints and troubleshooting, not full solutions.

> **Spoiler warning.** The instructor's `end/` folders contain the completed solos, not just the on-screen work. `section-04/end` has the Subscriptions slice from the Section 4 solo, and `section-09/end` has the status endpoint from the Section 9 solo. If you want to check the instructor's Expenses work mid-section, diff against `start/` rather than reading `end/` whole.

## Working across sections

This section only applies if you are doing the solos. If you are following along with the lectures and nothing else, open a snapshot, run it, and move on. Nothing needs to carry over.

The repo ships as per-section `start/` and `end/` snapshots, but your solo track is one continuous body of work that has to survive from Section 4 all the way to the end. So you need one working copy that you never throw away, and you want it in place **before Section 4**, which is where the first solo lands.

**Keep one long-lived working copy.** Clone the repo once, copy a section `start/` into a folder of your own, make it a git repository, and do all your solo work there on your own branch. This is where your Subscriptions area, your `.claude/` config, your hooks, and your reviewer accumulate. The snapshot folders are read-only reference from that point on.

Copy the *contents* of `start/`, not the folder itself. Config that GitHub or Claude Code reads by convention has to sit at the root of your working copy: `.claude/` for settings, rules, hooks, and agents, and `.github/workflows/` for CI. GitHub Actions in particular only discovers workflows at the git repository root.

Then, to pick up the next section's baseline, you have two ways to sync. Neither is wrong, they trade effort against safety.

### Merge, the default

Copy the next section's `start/` (or `end/`) into your working copy on a branch, commit it, and merge that branch into your work.

Because Subscriptions lives in its own files, its entity, endpoints, `tests/Subscriptions`, and UI route never overlap the Expenses files the instructor edits on screen, so the merge is usually conflict free. You gain the latest baseline and keep everything you built.

Use this while you are keeping up. It is the only option that preserves your solo work automatically.

### Copy over, the reset

Copy the next section's snapshot straight over your working copy, overwriting what is there.

This is the fast way back to a known-good state when your copy has drifted, a solo went sideways, or you are joining a section without having done the earlier ones. The cost is real: it overwrites your solo work in any file the snapshot also contains. Your Subscriptions files survive only if the snapshot does not carry its own version of them, and from Section 4 on it does.

If you reach for this, commit first, or copy your `Features/Subscriptions`, `tests/Subscriptions`, and `SubscriptionsPage.tsx` somewhere safe and put them back afterwards. Your `.claude/` config is the other thing to watch, since every section from 3 on ships its own.

Whichever you use, that single never-thrown-away working copy is what makes the two-track model hold together end to end.

## The guides

| Section | Solo exercise | Model | File |
|---------|---------------|-------|------|
| 3. Configuring It at Scale | None. | n/a | n/a |
| 4. Agent Skills | Scaffold the Subscriptions area with your skill | Two-area | [s04-subscriptions-endpoint-skill.md](s04-subscriptions-endpoint-skill.md) |
| 5. Hooks | A `PostToolUse` git auto-stage hook | Hint | [s05-posttooluse-git-autostage-hook.md](s05-posttooluse-git-autostage-hook.md) |
| 6. Subagents | Build the `convention-reviewer` | Hint | [s06-convention-reviewer-subagent.md](s06-convention-reviewer-subagent.md) |
| 7. MCP Servers | Add and scope a second MCP server | Hint | [s07-scope-new-mcp-server.md](s07-scope-new-mcp-server.md) |
| 8. Permissions, Sandboxing, and Security | None. The section's policy is built on screen, and the quiz carries the check. | n/a | n/a |
| 9. Autonomous and Remote Workflows | Hand off a Subscriptions feature via `/goal` | Two-area | [s09-ship-subscription-status-via-goal.md](s09-ship-subscription-status-via-goal.md) |
| 10. Headless Mode and CI/CD | None. The pipeline is built on screen, and reproducing it needs a GitHub repo, the Claude GitHub App, and your own API key. | n/a | n/a |
| 11. Plugins | Build your own plugin from scratch | Hint | [s11-build-your-own-plugin.md](s11-build-your-own-plugin.md) |
| 12. Conclusion | None. | n/a | n/a |
