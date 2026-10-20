// PreToolUse hook (matcher: Edit|Write|MultiEdit).
// Denies an edit to a .cs file that adds new XML doc comments (/// or /** */).
// Editing or removing existing doc comments is allowed: only a net increase is denied.

import {readFileSync, existsSync} from "node:fs";

// "///" but not "////", and "/**" but not the empty block comment "/**/".
const DOC_COMMENT = /^\s*(\/\/\/(?!\/)|\/\*\*(?!\/))/;

const countDocLines = (text = "") =>
    text.split(/\r?\n/).filter((line) => DOC_COMMENT.test(line)).length;

const input = JSON.parse(readFileSync(0, "utf8"));
const tool = input.tool_input ?? {};
const file = tool.file_path ?? "";

if (!file.toLowerCase().endsWith(".cs")) process.exit(0);

let before = 0;
let after = 0;

switch (input.tool_name) {
    case "Edit":
        before = countDocLines(tool.old_string);
        after = countDocLines(tool.new_string);
        break;
    case "MultiEdit":
        for (const edit of tool.edits ?? []) {
            before += countDocLines(edit.old_string);
            after += countDocLines(edit.new_string);
        }
        break;
    case "Write":
        before = existsSync(file) ? countDocLines(readFileSync(file, "utf8")) : 0;
        after = countDocLines(tool.content);
        break;
    default:
        process.exit(0);
}

if (after > before) {
    console.log(
        JSON.stringify({
            hookSpecificOutput: {
                hookEventName: "PreToolUse",
                permissionDecision: "deny",
                permissionDecisionReason:
                    "Do not add XML doc comments (/// or /** */) to C# code. Redo the edit without them.",
            },
        }),
    );
}

process.exit(0);
