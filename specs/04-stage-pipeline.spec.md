## spec: stage-pipeline

mode:
  - compositional (defines composition/execution contracts; no owned state)

behavior:
  - what: Define a runnable pipeline container (GstPipeline analogue) that composes stage elements (sources/transforms/sinks), supports pipeline-as-element composition (bin analogue), and emits shared `StageEvent` telemetry.
  - input:
      - IStagePipeline<TOut> : configured pipeline ready to execute
      - StagePipelineDefinition : declarative composition definition
      - IStageSource<T0> / IStageTransform<TIn,TOut> / IStageSink<TIn> : stage element surfaces
      - IStageEventSink : event sink for `StageEvent`
      - StageExecutionContext : execution identifiers and policy
      - CancellationToken
  - output:
      - TOut : pipeline output for source+transform pipelines
      - StageEvent stream (via sink) : stage lifecycle and metrics
  - caller_obligations:
      - compose pipelines such that adjacent element types are compatible (TOut of prior equals TIn of next)
      - provide a non-null IStageEventSink for UI-visible pipelines
      - treat the returned pipeline as single-use unless explicitly declared reusable
  - side_effects_allowed:
      - only through contained elements; pipeline container itself performs no IO beyond event emission

state:
  - none

preconditions:
  - definition has exactly one source and at least one sink (direct sink or transform chain ending in sink)
  - every StageId is non-empty

postconditions:
  - executing the pipeline runs elements in composition order
  - for each element execution, events are emitted: Running then a single terminal event

invariants:
  - deterministic ordering: given the same definition and inputs, element execution order is stable
  - event bubbling: events emitted by nested pipelines are emitted to the parent sink with their original StageId and ExecutionId
  - cancellation is propagated to all elements

failure_modes:
  - invalid_graph :: missing source/sink or incompatible types :: throw ArgumentException; no elements executed
  - element_failure :: an element throws or returns failure :: emit Failed event for that stage; abort downstream; propagate exception
  - cancellation :: token is signaled :: emit Canceled for active stage; abort downstream; propagate cancellation

policies:
  - execution_model:
      - pipelines are single-run by default; a reusable pipeline MUST explicitly declare it
  - concurrency:
      - a pipeline instance MUST NOT be executed concurrently
      - multiple pipeline instances MAY execute concurrently

never:
  - reorder elements at runtime
  - swallow element exceptions
  - emit StageEvent payloads containing raw prompts, system prompts, or raw attachment bytes

non_goals:
  - dynamic graph negotiation beyond generic type compatibility
  - streaming payload propagation (token streaming remains owned by narration provider dispatch)

performance:
  - pipeline container overhead under 2ms for up to 10 elements

observability:
  - logs:
      - trace_id, request_id, execution_id, stage_id, status, elapsed_ms, error_class
  - metrics:
      - stage_pipeline_run_count (by status), stage_pipeline_run_latency_ms

output:
  - minimal implementation only (no commentary, no TODOs)

---

types:
  - StagePipelineDefinition: record {
      string Name;
      IReadOnlyList<StageElementDefinition> Elements;
    }

  - StageElementDefinition: record {
      string StageId;
      string ElementKind; // source|transform|sink|pipeline
      object Element; // concrete stage element or nested pipeline
    }

interfaces:
  - IStagePipeline<TOut>:
      - ValueTask<TOut> RunAsync(StageExecutionContext context, IStageEventSink sink, CancellationToken cancellationToken)

  - IStagePipelineBuilder:
      - IStagePipeline<TOut> Build<T0, TOut>(StagePipelineDefinition definition)

notes:
  - pipeline-as-element:
      - a nested IStagePipeline<TOut> MAY appear as an element in a parent definition
      - the nested pipeline is treated as an element whose execution emits its internal StageEvents directly (bubbled) to the parent sink
      - the parent pipeline MAY additionally emit a parent-level stage event for the nested pipeline as a whole only if a distinct StageId is assigned to the nested pipeline wrapper
