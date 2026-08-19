#!/usr/bin/env bash
set -euo pipefail
git diff main | claude -p "Review this diff against our conventions." --bare \
  --append-system-prompt "Be extremelly concise."  \
  --max-budget-usd 1 \
  --permission-mode dontAsk \
  --allowedTools "Read" \
  --output-format json --json-schema "$(cat scripts/review-schema.json)" \
  > /tmp/review.json


jq -e . /tmp/review.json > /dev/null 2>&1 || {
  echo "review: no valid JSON returned" >&2
  cat /tmp/review.json >&2
  exit 1
}

jq -r '(.structured_output.findings // [])[]
       | "\(.severity)\t\(.file):\(.line)\t\(.issue)"' /tmp/review.json

if jq -e '[(.structured_output.findings // [])[]
          | select(.severity == "important")] | length > 0' /tmp/review.json > /dev/null; then
  echo "review gate: important findings above" >&2
  exit 1
fi

echo "review gate: passed"