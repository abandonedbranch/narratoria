## spec: <name>

mode:
  - isolated | compositional | stateful
  # isolated: pure function, no shared state
  # compositional: cooperates with collaborators, no owned state
  # stateful: reads/writes owned state (describe scope/persistence)

behavior:
  - what: <one-line behavior summary>
  - input:
      - <type>: <semantic role>
      - <type>: <semantic role>
  - output:
      - <type>: <semantic role>
  - caller_obligations:
      - <auth/session/state loading requirements>
  - side_effects_allowed:
      - <permitted IO or state changes>

state:
  - <state_key>: <type> | <persistence_scope>

preconditions:
  - <logical predicate>
  - <logical predicate>

postconditions:
  - <logical predicate>
  - <logical predicate>

invariants:
  - <always-true predicate>
  - <determinism/thread-safety constraints>

failure_modes:
  - <error_class> :: <trigger_condition> :: <mandatory_side_effect>
  - <error_class> :: <trigger_condition> :: <mandatory_side_effect>

policies:
  - <retry | timeout | idempotency | rate_limit>
  - <concurrency model | cancellation>

never:
  - <forbidden behavior>
  - <forbidden side effect>

non_goals:
  - <explicit exclusion>
  - <explicit exclusion>

performance:
  - <upper bound>
  - <latency SLO>

non_functional_requirements:
  - accessibility (WCAG 2.2 AA):
    - focus-visible: all interactive controls show a visible focus indicator (≥3:1 contrast)
    - keyboard: tab order follows visual order; actions operable via keyboard; ESC closes dialogs; Enter triggers primary actions; Space toggles buttons; Arrow keys for lists/grids
    - roles/states: appropriate ARIA roles/states (`button`, `dialog`, `navigation`, `main`, `list`, `listitem`, `aria-disabled`, `aria-expanded`, `aria-selected`, `aria-busy`)
    - live regions: streaming output uses `aria-live="polite"`; error banners use `aria-live="assertive"`
    - labels/descriptions: programmatic labels via `label`/`aria-label`/`aria-labelledby`; helper descriptions for constraints/errors
    - contrast: body text ≥4.5:1; large text and focus indicators ≥3:1
    - reduced motion: honor `prefers-reduced-motion`; disable non-essential animations and smooth scrolling
    - drag-and-drop: provide keyboard/clickable file-picker fallback; announce accepted/rejected files and reasons
  - responsive_ux:
    - breakpoints: 360x640 (mobile), 768x1024 (tablet), 1280x800 (desktop); no horizontal scroll in main content
    - containers: components flex/stack at breakpoints; sidebar collapses on mobile; compose bar stays reachable
    - target_sizes: interactive targets ≥44x44 px; spacing prevents accidental activation
    - typography: relative units; readable at 200% zoom without layout breakage
    - overflow/truncation: long strings truncate with ellipsis and accessible tooltip; essential info preserved
    - scroll/virtualization: virtualize long lists/logs beyond thresholds; maintain keyboard focus and item semantics
    - touch_fallbacks: provide explicit controls; no gesture-only actions
  - performance_budgets:
    - initial load: LCP ≤2.5s; TTI ≤2s on target hardware for primary routes
    - interactions: per interaction updates ≤50ms; per-keystroke ≤16ms; stream append ≤50ms
    - streaming cadence: render token updates within 100ms; avoid >200ms blocking bursts
    - virtualization thresholds: logs beyond 100 turns; session lists beyond 100 rows
    - storage patterns: IndexedDB access is batched; avoid blocking UI thread; backoff on quota signals
  - testing_hooks:
    - axe-core: integrate with Playwright; fail CI on any violations for `/test-orchestrator` and `/test-pipeline-log`
    - keyboard-only: traverse interactive controls; assert focus order and operability
    - reduced_motion: force `prefers-reduced-motion`; assert animations disabled
    - viewport_matrix: test at 360x640, 768x1024, 1280x800; assert no horizontal scroll and reachable controls
    - perf_smoke: collect render/stream timings; assert budgets per component

observability:
  - logs:
      - <required fields e.g., trace_id, request_id, stage, elapsed_ms, status, error_class>
  - metrics:
      - <required metrics>

output:
  - minimal implementation only (no commentary, no TODOs)
