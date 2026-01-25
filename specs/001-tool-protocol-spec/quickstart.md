# Quickstart: Implementing Spec 001 Tool Protocol

## 1) Emit NDJSON to stdout
- Write one JSON object per line (UTF-8, Unix newlines).
- Include `version: "0"` and a valid `type` in every event.
- Do not buffer the whole payload; flush after each line for streaming responsiveness.

## 2) Minimal tool skeleton (pseudocode)

```
print({"version": "0", "type": "log", "level": "info", "message": "Starting"})
# ...do work, emit state_patch/asset/ui_event as needed...
print({"version": "0", "type": "done", "ok": true, "summary": "Completed."})
exit(0)
```

## 3) Event types to use
- `log`: progress or diagnostics; never treated as failure.
- `state_patch`: JSON object merged into Narratoria session state.
- `asset`: register a generated file with `assetId`, `kind`, `mediaType`, `path`.
- `ui_event`: request UI actions by `event` name with optional `payload`.
- `error`: structured error, followed by a `done` event with `ok: false`.
- `done`: exactly one per invocation; set `ok` true/false, include optional `summary`.

## 4) Process exit rules
- Exit code 0 means protocol intact; `done.ok` reports logical success/failure.
- Non-zero exit code means protocol failure even if `done` was emitted.

## 5) Validation tips
- Use a JSON/NDJSON linter to ensure each line is valid and complete.
- Ensure `state_patch.patch` is an object, not an array or primitive.
- Keep `assetId` unique per invocation and provide valid MIME strings.

## 6) Forward compatibility
- Accept and ignore unknown fields from Narratoria input.
- Emit only defined `type` values; keep `version` at "0" until Spec 002.
