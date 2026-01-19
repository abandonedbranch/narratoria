# Phase 0 Research: UnifiedInference Client

## Decisions

- OpenAI Access: Use the official OpenAI .NET SDK (wrap SDK types directly; expose `NativeClient`).
- Ollama Access: Prefer official/de-facto .NET client; if absent, implement HTTP API via `HttpClient` with transport abstraction; expose native transport/client.
- Hugging Face Access: Use `HttpClient` for generic Inference APIs and configurable Inference Endpoints; support any `model id` and task-dependent responses.
- Capability Discovery: Per provider+model; unknown capabilities default disabled; callers use discovery to avoid exceptions.
- Error Handling: Throw `NotSupportedException` for unsupported modalities/settings; propagate `HttpRequestException`/SDK exceptions with stable contextual info.
- Output Assembly: Project file may be `UnifiedInference.csproj`; enforce `<AssemblyName>inference</AssemblyName>` so artifact is `inference.dll`.

## Rationale

- Official OpenAI SDK ensures forward compatibility and reduces maintenance risk; exposing `NativeClient` enables advanced usage without breaking unified surface.
- Ollama’s HTTP API is stable and well-documented; wrapping it avoids dependency on unofficial libraries if no de-facto standard exists.
- Hugging Face lacks an official .NET SDK; `HttpClient` offers full control for generic endpoints and Inference Endpoints.
- Capability-first integration avoids runtime surprises, aligning with conservative defaults and small/stable interfaces.
- Clear error semantics simplify caller logic: capabilities guide behavior; exceptions only for unsupported or transport failures.
- Assembly naming constraint (`inference.dll`) meets downstream packaging/runtime expectations.

## Alternatives Considered

- Re-implementing OpenAI endpoints via `HttpClient`: rejected due to requirement to use official SDK and high maintenance.
- Mandatory video/music support at GA: rejected; clarified as best-effort (video) and hooks-only (music).
- Provider-wide capability defaults: rejected; per-model discovery is more precise and aligns with conservative defaults.

## Best Practices

- DI-Friendly: constructors accept preconfigured `OpenAIClient`, `HttpClient`, and/or Ollama native/transport.
- Cancellation: honor `CancellationToken` in all public methods; propagate cancellation exceptions.
- Immutability: prefer `record` types for requests/responses/settings; treat `ProviderOverrides` as opaque immutable map.
- Mapping: ignore or error on unsupported settings per provider; document mapping rules.
- Streaming: where supported (text/audio), expose stream-friendly methods in provider-specific classes, while keeping unified surface minimal.
