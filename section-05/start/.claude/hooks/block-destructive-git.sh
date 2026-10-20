#!/usr/bin/env bash
# PreToolUse hook (matcher: Bash).
# Reads the tool call JSON from stdin and denies destructive git commands
# before they run. Anything else is allowed through untouched.

input=$(cat)

# Pull tool_input.command out of the payload. Prefer jq, fall back to sed so
# the hook still works on machines without jq (e.g. plain Git Bash on Windows).
if command -v jq >/dev/null 2>&1; then
  cmd=$(printf '%s' "$input" | jq -r '.tool_input.command // empty')
else
  cmd=$(printf '%s' "$input" \
    | sed -n 's/.*"command"[[:space:]]*:[[:space:]]*"\(\([^"\\]\|\\.\)*\)".*/\1/p' \
    | sed 's/\\"/"/g; s/\\\\/\\/g')
fi

[ -z "$cmd" ] && exit 0

# Only inspect commands that actually call git.
printf '%s' "$cmd" | grep -Eq '(^|[[:space:];&|(])git[[:space:]]' || exit 0

deny() {
  local reason="$1"
  # Escape for embedding in JSON.
  reason=${reason//\\/\\\\}
  reason=${reason//\"/\\\"}
  printf '{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"deny","permissionDecisionReason":"%s"}}\n' "$reason"
  exit 0
}

check() {
  local pattern="$1" reason="$2"
  if printf '%s' "$cmd" | grep -Eq "$pattern"; then
    deny "Blocked destructive git command: $reason. Ask the user to run it themselves if it is really intended."
  fi
}

# "git" plus any global options (e.g. -C <path>, -c key=val, --no-pager) before the subcommand.
G='git([[:space:]]+(-[Cc][[:space:]]+[^[:space:]]+|-[^[:space:]]+))*[[:space:]]+'

check "${G}push([[:space:]].*)?[[:space:]](--force|-f)([[:space:]]|$)"   "force push rewrites remote history"
check "${G}push([[:space:]].*)?[[:space:]]\+[^[:space:]]+"                "force push via +refspec rewrites remote history"
check "${G}push([[:space:]].*)?[[:space:]](--delete|-d)([[:space:]]|$)"  "deleting a remote branch"
check "${G}push([[:space:]].*)?[[:space:]]:[^[:space:]]+"                 "deleting a remote branch via :refspec"
check "${G}push([[:space:]].*)?[[:space:]]--mirror"                      "mirror push overwrites the remote"
check "${G}reset([[:space:]].*)?[[:space:]]--(hard|merge|keep)"           "git reset discards local changes"
check "${G}clean([[:space:]].*)?[[:space:]]-[a-zA-Z]*[fdx]"               "git clean deletes untracked files"
check "${G}checkout([[:space:]].*)?[[:space:]](--[[:space:]]+)?\.([[:space:]]|$)" "git checkout . discards working tree changes"
check "${G}checkout([[:space:]].*)?[[:space:]](-f|--force)([[:space:]]|$)" "forced checkout discards local changes"
check "${G}restore([[:space:]].*)?[[:space:]]\.([[:space:]]|$)"          "git restore . discards working tree changes"
check "${G}branch([[:space:]].*)?[[:space:]](-D|--delete[[:space:]]+--force|-df|-fd)([[:space:]]|$)" "force-deleting a branch"
check "${G}stash[[:space:]]+(drop|clear)"                                 "dropping stashed work"
check "${G}filter-(branch|repo)"                                          "rewriting repository history"
check "${G}reflog[[:space:]]+(expire|delete)"                             "expiring reflog removes recovery points"
check "${G}gc([[:space:]].*)?--prune"                                     "pruning unreachable objects"
check "${G}update-ref[[:space:]]+-d"                                      "deleting a ref"

exit 0
