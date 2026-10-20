#!/usr/bin/env bash
# PostToolUse hook (matcher: Edit|Write|MultiEdit).
# Stages the file Claude just wrote. It reacts to an edit, it does not gate
# one, so it always exits 0: a failed `git add` must never reach Claude.

input=$(cat)

# Pull tool_input.file_path and tool_response.success out of the payload.
# Prefer jq, fall back to sed so the hook still works without jq.
if command -v jq >/dev/null 2>&1; then
  file=$(printf '%s' "$input" | jq -r '.tool_input.file_path // empty')
  ok=$(printf '%s' "$input" | jq -r '.tool_response.success')
else
  file=$(printf '%s' "$input" \
    | sed -n 's/.*"tool_input"[[:space:]]*:[[:space:]]*{.*"file_path"[[:space:]]*:[[:space:]]*"\(\([^"\\]\|\\.\)*\)".*/\1/p' \
    | head -n 1 \
    | sed 's/\\"/"/g; s/\\\\/\\/g')
  ok=$(printf '%s' "$input" \
    | sed -n 's/.*"tool_response"[[:space:]]*:[[:space:]]*{.*"success"[[:space:]]*:[[:space:]]*\(true\|false\).*/\1/p' \
    | head -n 1)
fi

[ -z "$file" ] && exit 0

# Only stage writes that landed. Skip on an explicit false; absent still stages.
[ "$ok" = "false" ] && exit 0

git add -- "$file" >/dev/null 2>&1 || true

exit 0
