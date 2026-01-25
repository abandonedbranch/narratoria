# Research Findings

## Language and Runtime Scope
- **Decision:** Treat the Tool Protocol as language-agnostic documentation; no implementation language mandated. Tools remain free to be authored in any language that can emit UTF-8 NDJSON.
- **Rationale:** Spec 001 explicitly targets external processes and must not constrain tool authors; limiting to a runtime would reduce compatibility and violate protocol goals.
- **Alternatives considered:** Mandating a reference runtime (e.g., .NET, Node) was rejected because it would create adoption friction and contradict the cross-language intent.

## Dependencies and Libraries
- **Decision:** No runtime dependencies beyond a JSON serializer that can emit NDJSON with UTF-8 and Unix newlines; repository deliverables stay Markdown-only.
- **Rationale:** The spec is purely contractual; minimizing dependencies keeps the protocol portable and easy to consume. Any reference tooling can choose its own stack later.
- **Alternatives considered:** Shipping a reference SDK was deferred to avoid locking in a stack before protocol stabilization.

## Testing and Validation
- **Decision:** Validate examples and schema fragments with JSON linters/formatters and (optionally) NDJSON validators; apply Markdown lint for docs quality.
- **Rationale:** Ensures the provided examples are mechanically valid and copy-pastable for tool authors; lightweight enough for a documentation-only deliverable.
- **Alternatives considered:** Formal JSON Schema publication for each event type was deferred until Spec 002 to keep the current draft lightweight.

## Target Platform and Invocation
- **Decision:** Target cross-platform execution (macOS, Windows, Linux) with Narratoria invoking tools via native process launch and stdin/stdout pipes.
- **Rationale:** Matches the invocation model described in the spec and supports the widest set of tool authors; avoids platform-specific IPC.
- **Alternatives considered:** HTTP or gRPC transport was rejected because the spec centers on stdin/stdout simplicity and minimal dependencies.

## Performance and Constraints
- **Decision:** Favor low-latency, streaming-friendly behavior: emit NDJSON lines as work progresses, ensure each line is a complete JSON object, and always terminate with a `done` event plus process exit code 0 when successful.
- **Rationale:** Matches the streaming and ordering expectations in the spec while keeping tools simple; explicit termination avoids dangling processes.
- **Alternatives considered:** Buffered batch emission was rejected because it would delay UI responsiveness and increase failure blast radius.

## Scope and Project Structure
- **Decision:** Keep repository deliverables documentation-centric under `specs/001-tool-protocol-spec/` with supporting research, data model, contracts, and quickstart artifacts; no source code changes required for this feature.
- **Rationale:** The repository currently has empty `src/` and `tests/` directories; focusing on specs aligns effort to the requested formalization.
- **Alternatives considered:** Introducing reference implementations or SDK stubs was deferred until a later iteration (post Spec 001 stabilization).
