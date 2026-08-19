# Claude Code Deep Dive - Section 10: Headless Mode and CI/CD Prompts

These are the prompts run on screen during this section's lessons. Copy them as they are into Claude Code and follow along.

## L2 - Print Mode and Its Outputs

### One-shot print mode

Runs a single prompt with no interactive session and prints the answer to stdout.

```
claude -p "What does the expenses endpoint group do?"
```

### JSON output

Wraps the same answer in a JSON envelope with cost, duration, and session id, so a script can read more than the text.

```
claude -p "What does the expenses endpoint group do?" --output-format json
```

### Streaming JSON output

Emits one JSON event per message as the run happens, so a pipeline can react before the run finishes.

```
claude -p "What does the expenses endpoint group do?" --output-format stream-json --verbose
```

### A write with no tools allowed

Asks for a file to be created without granting any tools, so you can watch the run stall on permissions.

```
claude -p "Create a README with the list of existing migrations"
```

### The same write, with tools allowed

Grants exactly the tools the task needs, so the headless run can complete unattended.

```
claude -p "Create a README with the list of existing migrations" --allowedTools "Read,Edit,Write"
```

### Bare mode

Skips hooks, LSP, plugins, and memory, so the run starts fast and behaves the same on every machine.

```
claude --bare -p "Summarise the Expenses feature in two sentences" 
```

### Structured output against a schema

Forces the answer into a JSON Schema you define, so the result can be piped straight into jq without parsing prose.

```
mkdir -p /tmp/s10 && cat > /tmp/s10/endpoints.json <<'EOF'
{
  "type": "object",
  "properties": {
    "endpoints": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "method": { "type": "string" },
          "route":  { "type": "string" },
          "handler":{ "type": "string" }
        },
        "required": ["method", "route"]
      }
    }
  },
  "required": ["endpoints"]
}
EOF

claude -p "List every API endpoint with its HTTP method and route" \
  --output-format json \
  --json-schema "$(cat /tmp/s10/endpoints.json)" \
  | jq '.structured_output.endpoints | length'

```

## L3 - Headless Mode and CI/CD

### Explain a CI failure

Pipes a build log into Claude and asks only for the root cause, which is the shape a CI step would use.

```
cat ci-failure.log | claude -p "Concisely explain the root cause of this failure."
```

### Typo-lint a diff

Pipes the branch diff in and constrains the output to `filename:line` plus the issue, so it is easy to parse or post as a comment.

```
git diff main | claude -p "Typo-lint this diff. Report filename:line and the issue, nothing else."
```
