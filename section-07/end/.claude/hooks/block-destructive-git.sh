#!/usr/bin/env bash
# PreToolUse hook: hard-block destructive git commands.
# JSON in on stdin; JSON out on stdout. Emits a "deny" decision on a match.
set -euo pipefail

input=$(cat)
command=$(printf '%s' "$input" | jq -r '.tool_input.command // ""')

# A trailing space lets "-f" / "-d" flags at the end of the command match a
# following "[[:space:]]" without needing an end-of-line anchor (BSD grep chokes
# on "$" inside alternation groups).
padded="$command "

# Each entry: <extended-regex>~~<human-readable reason>. The "~~" delimiter avoids
# colliding with the "|" alternation inside the regexes. Matched case-insensitively
# against the whole command, so a git call chained after && ; | is caught too.
patterns=(
  'git.*push.*(--force|--force-with-lease|-f[[:space:]]|--delete)~~force-push or delete a remote branch'
  'git.*reset.*--hard~~hard reset (discards working tree)'
  'git.*clean.*-[a-z]*f~~git clean -f (deletes untracked files)'
  'git.*branch.*-D[[:space:]]~~force-delete a branch'
  'git.*(checkout|restore).*(--force|-f[[:space:]])~~discard working-tree changes'
  'git.*checkout.*--[[:space:]]~~checkout -- discards changes'
  'git.*rebase~~rebase (rewrites history)'
  'git.*commit.*--amend~~amend (rewrites the last commit)'
  'git.*stash.*(drop|clear)~~drop/clear stashed changes'
  'git.*(filter-branch|filter-repo)~~filter-branch/filter-repo (rewrites history)'
  'git.*reflog.*(expire|delete)~~expire/delete reflog entries'
  'git.*update-ref.*-d[[:space:]]~~delete a ref'
  'git.*tag.*-d[[:space:]]~~delete a tag'
  'git.*gc.*--prune~~gc --prune (removes unreachable objects)'
)

for entry in "${patterns[@]}"; do
  regex="${entry%%~~*}"
  reason="${entry#*~~}"
  if printf '%s' "$padded" | grep -iEq "$regex"; then
    jq -n --arg reason "Blocked destructive git command: $reason" \
      '{hookSpecificOutput: {hookEventName: "PreToolUse", permissionDecision: "deny", permissionDecisionReason: $reason}}'
    exit 0
  fi
done

echo '{}'
