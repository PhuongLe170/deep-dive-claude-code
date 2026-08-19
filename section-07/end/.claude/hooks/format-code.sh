#!/usr/bin/env bash
# PostToolUse (Edit|Write|MultiEdit): format the file Claude just touched.
# C# (.cs) files under the API go through `dotnet format`; files under the web
# app go through Prettier. Anything else is left untouched.
set -euo pipefail

input=$(cat)
file=$(printf '%s' "$input" | jq -r '.tool_input.file_path // empty')
[ -z "$file" ] && exit 0
[ -f "$file" ] || exit 0

root="${CLAUDE_PROJECT_DIR:-$(pwd)}"

case "$file" in
  *.cs)
    # Format just this file within the API project. Quietly no-op if the
    # tooling is unavailable so an editor change never fails the hook.
    if command -v dotnet >/dev/null 2>&1; then
      dotnet format whitespace "$root/app/api/Api" --include "$file" >/dev/null 2>&1 || true
    fi
    ;;
  "$root"/app/web/*)
    case "$file" in
      *.ts|*.tsx|*.js|*.jsx|*.json|*.css|*.html|*.md)
        if command -v pnpm >/dev/null 2>&1; then
          pnpm --dir "$root/app/web" exec prettier --write --ignore-unknown "$file" >/dev/null 2>&1 || true
        fi
        ;;
    esac
    ;;
esac

# Re-stage the formatted bytes. PostToolUse hooks are not ordered against each
# other, so auto-stage.sh may have staged the pre-format version. The formatter
# is the only hook that rewrites bytes after the edit, so staging its own output
# here makes both orderings land clean. Never let a git hiccup fail the hook.
git add -- "$file" 2>/dev/null || true

exit 0
