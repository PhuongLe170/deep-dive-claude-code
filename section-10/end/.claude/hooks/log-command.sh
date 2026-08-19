#!/usr/bin/env bash
# PostToolUse (Bash): append every command Claude runs to an audit log.
set -euo pipefail

input=$(cat)
cmd=$(printf '%s' "$input" | jq -r '.tool_input.command // empty')
[ -z "$cmd" ] && exit 0

mkdir -p .claude/logs
printf '%s\t%s\n' "$(date -u +%FT%TZ)" "$cmd" >> .claude/logs/commands.log
exit 0