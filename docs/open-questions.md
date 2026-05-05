# Open questions

Decisions noted but not yet made. Each entry is one paragraph under a numbered heading. Resolve by either making the decision (then either delete the entry or fold it into a TASK / commit message) or by sizing it into a TASK file.

Per `CLAUDE.md` §Workflow: open questions live here, not in chat history. Read only if one blocks your task.

## OQ-0001 — Should `.claude/` be gitignored?

The directory holds Claude Code session metadata. It currently appears in `git status` on every run. Options: (a) add to `.gitignore`; (b) commit it (only if its contents are version-relevant — they probably are not); (c) leave noisy. Default-leaning (a). Not blocking.

## OQ-0002 — JSON apostrophe escaping in CLI output

`System.Text.Json` defaults to `JavaScriptEncoder.Default`, which Unicode-escapes the apostrophe character (and a few others) in error message strings. Run `engine apply Wibble` to see the form: apostrophes appear as their `\u`-escaped Unicode codepoint rather than as `'`. Valid JSON; passes through `jq` correctly; visually noisy when read raw. Switching to `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` resolves it but broadens what the encoder permits unescaped — generally safe for a CLI not feeding HTML/JS contexts, but worth a moment of thought before flipping. Not blocking.
