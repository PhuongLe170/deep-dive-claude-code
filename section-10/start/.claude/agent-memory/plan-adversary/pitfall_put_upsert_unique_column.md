---
name: pitfall-put-upsert-unique-column
description: PUT-upserts on a uniquely-indexed column need conflict handling and must set code-managed timestamps on both branches
metadata:
  type: feedback
---

PUT-upsert endpoints that key off a uniquely-indexed column (e.g. MonthlyBudget's month/period) must do two things or they break in production:

1. **Guard SaveChangesAsync against unique-index conflicts.** Two concurrent PUTs both see "no row exists," both insert, and the second throws DbUpdateException → 500. Catch DbUpdateException, re-read the row, and switch to update.

2. **Set code-managed timestamps on every write branch.** CreatedAt has a DB default; UpdatedAt does not. If the update branch (or a second write path) forgets to set UpdatedAt, it persists as 0001-01-01. Set it explicitly on both insert and update, and assert `UpdatedAt != default` in a create-path test.

**Why:** Both were Critical findings (C1 500 on concurrent PUT, C2 0001-01-01 timestamp) in the MonthlyBudget review. They come from the same root cause: treating an upsert as a simple insert-or-update without accounting for concurrency or for columns the DB does not auto-populate.

**How to apply:** When reviewing any new PUT/upsert plan in this repo, check for both the DbUpdateException retry path and explicit timestamp-setting on all write branches before giving a go.
