#!/usr/bin/env bash
# PostToolUse (Edit|Write|MultiEdit): stage the file Claude just wrote.
# Reacts to an edit, it does not gate one, so it always exits 0. A failed
# `git add` must never surface as an error into the conversation.
set -euo pipefail

input=$(cat)
file=$(printf '%s' "$input" | jq -r '.tool_input.file_path // empty')
[ -z "$file" ] && exit 0

# Bonus: only stage writes that actually landed. Skip only on an explicit
# `false`; an absent field reads as "null" here and still stages. Do not use
# `.tool_response.success // "true"` here: jq's `//` falls through on both null
# AND false, so a failed write would slip past the guard and stage anyway.
ok=$(printf '%s' "$input" | jq -r '.tool_response.success')
[ "$ok" = "false" ] && exit 0

git add -- "$file" 2>/dev/null || true

exit 0
