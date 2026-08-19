#!/usr/bin/env bash
# Notification hook: ping the desktop when Claude needs attention.
# JSON in on stdin; JSON out on stdout. There is no decision to make here.
set -euo pipefail

input=$(cat)
message=$(printf '%s' "$input" | jq -r '.message // "Claude needs your attention"')

# OSC 777 desktop notification, emitted by Claude Code (hooks have no /dev/tty).
seq=$(printf '\033]777;notify;Claude Code;%s\033\\' "$message")

jq -n --arg s "$seq" '{terminalSequence: $s}'