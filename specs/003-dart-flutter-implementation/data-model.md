# Data Model: Dart Class Implementations

> **Spec**: 003-dart-flutter-implementation
> **Status**: Draft
> **Purpose**: Dart class definitions for Narratoria runtime

This document provides Dart class implementations for the algorithms and schemas defined in [Spec 002 data-model.md](../002-plan-generation-skills/data-model.md).

---

## §1 Skill Classes

### §1.1 Skill

```dart
/// A capability bundle following Agent Skills Standard
class Skill {
  final String name;
  final String? displayName;
  final String description;
  final String version;
  final String? author;
  final String? license;
  final String directoryPath;
  final String? promptPath;
  final String? configSchemaPath;
  final List<SkillScript> scripts;
  final List<String> capabilities;
  final int priority;
  final RetryPolicy? defaultRetryPolicy;

  bool enabled;
  SkillErrorState errorState;

  Skill({
    required this.name,
    this.displayName,
    required this.description,
    required this.version,
    this.author,
    this.license,
    required this.directoryPath,
    this.promptPath,
    this.configSchemaPath,
    this.scripts = const [],
    this.capabilities = const [],
    this.priority = 50,
    this.defaultRetryPolicy,
    this.enabled = true,
    this.errorState = SkillErrorState.healthy,
  });

  /// Load skill from skill.json manifest
  factory Skill.fromManifest(String directoryPath, Map<String, dynamic> json) {
    return Skill(
      name: json['name'] as String,
      displayName: json['displayName'] as String?,
      description: json['description'] as String,
      version: json['version'] as String,
      author: json['author'] as String?,
      license: json['license'] as String?,
      directoryPath: directoryPath,
      promptPath: json['prompt'] as String?,
      configSchemaPath: json['configSchema'] as String?,
      scripts: (json['scripts'] as List<dynamic>?)
              ?.map((s) => SkillScript.fromJson(s as Map<String, dynamic>))
              .toList() ??
          [],
      capabilities: (json['capabilities'] as List<dynamic>?)
              ?.cast<String>() ??
          [],
      priority: json['priority'] as int? ?? 50,
      defaultRetryPolicy: json['retryPolicy'] != null
          ? RetryPolicy.fromJson(json['retryPolicy'] as Map<String, dynamic>)
          : null,
    );
  }

  /// Get absolute path to a script
  String scriptPath(String scriptName) {
    final script = scripts.firstWhere(
      (s) => s.name == scriptName,
      orElse: () => throw SkillScriptNotFoundError(name, scriptName),
    );
    return '$directoryPath/${script.path}';
  }

  /// Check if skill is available for planning
  bool get isAvailable =>
      enabled && errorState != SkillErrorState.permanentFailure;
}
```

### §1.2 SkillScript

```dart
/// Executable script within a skill
class SkillScript {
  final String name;
  final String path;
  final String? description;
  final String? inputSchemaPath;
  final Duration timeout;
  final bool required;

  const SkillScript({
    required this.name,
    required this.path,
    this.description,
    this.inputSchemaPath,
    this.timeout = const Duration(seconds: 30),
    this.required = false,
  });

  factory SkillScript.fromJson(Map<String, dynamic> json) {
    return SkillScript(
      name: json['name'] as String,
      path: json['path'] as String,
      description: json['description'] as String?,
      inputSchemaPath: json['inputSchema'] as String?,
      timeout: Duration(milliseconds: json['timeout'] as int? ?? 30000),
      required: json['required'] as bool? ?? false,
    );
  }
}
```

### §1.3 SkillErrorState

```dart
/// Health status of a skill at runtime
enum SkillErrorState {
  /// Available for planning
  healthy,

  /// Available but may be slow/unreliable
  degraded,

  /// Transient network issue; retry with backoff
  temporaryFailure,

  /// Unrecoverable in this session; disable
  permanentFailure,
}

extension SkillErrorStateExtension on SkillErrorState {
  /// Whether the skill can be selected for planning
  bool get canSelect => this != SkillErrorState.permanentFailure;

  /// Human-readable description
  String get description => switch (this) {
        SkillErrorState.healthy => 'Available',
        SkillErrorState.degraded => 'Degraded performance',
        SkillErrorState.temporaryFailure => 'Temporary failure',
        SkillErrorState.permanentFailure => 'Unavailable',
      };
}
```

### §1.4 ConfigField

```dart
/// A field in a skill's configuration schema
class ConfigField {
  final String key;
  final ConfigFieldType type;
  final String? title;
  final String? description;
  final dynamic defaultValue;
  final List<dynamic>? enumValues;
  final String? format;
  final num? minimum;
  final num? maximum;
  final bool sensitive;
  final String? envVar;
  final String? category;

  const ConfigField({
    required this.key,
    required this.type,
    this.title,
    this.description,
    this.defaultValue,
    this.enumValues,
    this.format,
    this.minimum,
    this.maximum,
    this.sensitive = false,
    this.envVar,
    this.category,
  });

  factory ConfigField.fromJson(String key, Map<String, dynamic> json) {
    return ConfigField(
      key: key,
      type: ConfigFieldType.fromString(json['type'] as String),
      title: json['title'] as String?,
      description: json['description'] as String?,
      defaultValue: json['default'],
      enumValues: json['enum'] as List<dynamic>?,
      format: json['format'] as String?,
      minimum: json['minimum'] as num?,
      maximum: json['maximum'] as num?,
      sensitive: json['x-sensitive'] as bool? ?? false,
      envVar: json['x-env-var'] as String?,
      category: json['x-category'] as String?,
    );
  }
}

enum ConfigFieldType {
  string,
  number,
  integer,
  boolean,
  array;

  static ConfigFieldType fromString(String value) {
    return ConfigFieldType.values.firstWhere(
      (t) => t.name == value,
      orElse: () => ConfigFieldType.string,
    );
  }
}
```

---

## §2 Plan Classes

### §2.1 PlanJson

```dart
/// Structured plan generated by narrator AI
class PlanJson {
  final String requestId;
  final String? narrative;
  final List<ToolInvocation> tools;
  final bool parallel;
  final Set<String> disabledSkills;
  final PlanMetadata? metadata;

  const PlanJson({
    required this.requestId,
    this.narrative,
    required this.tools,
    this.parallel = false,
    this.disabledSkills = const {},
    this.metadata,
  });

  factory PlanJson.fromJson(Map<String, dynamic> json) {
    return PlanJson(
      requestId: json['requestId'] as String,
      narrative: json['narrative'] as String?,
      tools: (json['tools'] as List<dynamic>)
          .map((t) => ToolInvocation.fromJson(t as Map<String, dynamic>))
          .toList(),
      parallel: json['parallel'] as bool? ?? false,
      disabledSkills: (json['disabledSkills'] as List<dynamic>?)
              ?.cast<String>()
              .toSet() ??
          {},
      metadata: json['metadata'] != null
          ? PlanMetadata.fromJson(json['metadata'] as Map<String, dynamic>)
          : null,
    );
  }

  Map<String, dynamic> toJson() => {
        'requestId': requestId,
        if (narrative != null) 'narrative': narrative,
        'tools': tools.map((t) => t.toJson()).toList(),
        'parallel': parallel,
        'disabledSkills': disabledSkills.toList(),
        if (metadata != null) 'metadata': metadata!.toJson(),
      };
}
```

### §2.2 ToolInvocation

```dart
/// A tool invocation descriptor within a Plan JSON
class ToolInvocation {
  final String toolId;
  final String toolPath;
  final Map<String, dynamic> input;
  final List<String> dependencies;
  final bool required;
  final bool async;
  final RetryPolicy retryPolicy;

  const ToolInvocation({
    required this.toolId,
    required this.toolPath,
    this.input = const {},
    this.dependencies = const [],
    this.required = true,
    this.async = false,
    this.retryPolicy = const RetryPolicy(),
  });

  factory ToolInvocation.fromJson(Map<String, dynamic> json) {
    return ToolInvocation(
      toolId: json['toolId'] as String,
      toolPath: json['toolPath'] as String,
      input: json['input'] as Map<String, dynamic>? ?? {},
      dependencies:
          (json['dependencies'] as List<dynamic>?)?.cast<String>() ?? [],
      required: json['required'] as bool? ?? true,
      async: json['async'] as bool? ?? false,
      retryPolicy: json['retryPolicy'] != null
          ? RetryPolicy.fromJson(json['retryPolicy'] as Map<String, dynamic>)
          : const RetryPolicy(),
    );
  }

  Map<String, dynamic> toJson() => {
        'toolId': toolId,
        'toolPath': toolPath,
        'input': input,
        'dependencies': dependencies,
        'required': required,
        'async': async,
        'retryPolicy': retryPolicy.toJson(),
      };
}
```

### §2.3 RetryPolicy

```dart
/// Configures retry behavior for tool execution
class RetryPolicy {
  final int maxRetries;
  final int backoffMs;

  const RetryPolicy({
    this.maxRetries = 3,
    this.backoffMs = 100,
  });

  factory RetryPolicy.fromJson(Map<String, dynamic> json) {
    return RetryPolicy(
      maxRetries: json['maxRetries'] as int? ?? 3,
      backoffMs: json['backoffMs'] as int? ?? 100,
    );
  }

  Map<String, dynamic> toJson() => {
        'maxRetries': maxRetries,
        'backoffMs': backoffMs,
      };

  /// Calculate delay for a given attempt (1-indexed)
  /// Formula: backoffMs × 2^(attempt - 1)
  Duration delayForAttempt(int attempt) {
    assert(attempt >= 1);
    final delayMs = backoffMs * (1 << (attempt - 1));
    return Duration(milliseconds: delayMs);
  }
}
```

### §2.4 PlanMetadata

```dart
/// Plan metadata for debugging and replan tracking
class PlanMetadata {
  final int generationAttempt;
  final String? parentPlanId;

  const PlanMetadata({
    required this.generationAttempt,
    this.parentPlanId,
  });

  factory PlanMetadata.fromJson(Map<String, dynamic> json) {
    return PlanMetadata(
      generationAttempt: json['generationAttempt'] as int,
      parentPlanId: json['parentPlanId'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
        'generationAttempt': generationAttempt,
        'parentPlanId': parentPlanId,
      };
}
```

---

## §3 Execution Classes

### §3.1 PlanExecutionContext

```dart
/// Context for executing a plan with dependency resolution
class PlanExecutionContext {
  final PlanJson plan;
  final Map<String, ToolInvocation> _toolsById;
  final Map<String, Set<String>> _dependencyGraph;
  final Map<String, int> _inDegree;

  List<String>? _executionOrder;

  PlanExecutionContext(this.plan)
      : _toolsById = {for (final t in plan.tools) t.toolId: t},
        _dependencyGraph = {},
        _inDegree = {} {
    _buildGraph();
  }

  void _buildGraph() {
    for (final tool in plan.tools) {
      _dependencyGraph[tool.toolId] = {};
      _inDegree[tool.toolId] = tool.dependencies.length;
    }
    for (final tool in plan.tools) {
      for (final dep in tool.dependencies) {
        _dependencyGraph[dep]?.add(tool.toolId);
      }
    }
  }

  /// Check for circular dependencies using DFS
  bool hasCycle() {
    final visited = <String>{};
    final recStack = <String>{};

    bool dfs(String toolId) {
      visited.add(toolId);
      recStack.add(toolId);

      for (final dep in _toolsById[toolId]?.dependencies ?? []) {
        if (!visited.contains(dep)) {
          if (dfs(dep)) return true;
        } else if (recStack.contains(dep)) {
          return true;
        }
      }

      recStack.remove(toolId);
      return false;
    }

    for (final tool in plan.tools) {
      if (!visited.contains(tool.toolId)) {
        if (dfs(tool.toolId)) return true;
      }
    }
    return false;
  }

  /// Get topologically sorted execution order (Kahn's algorithm)
  List<String> get executionOrder {
    if (_executionOrder != null) return _executionOrder!;

    if (hasCycle()) {
      throw CyclicDependencyException(plan.requestId);
    }

    final inDegree = Map<String, int>.from(_inDegree);
    final queue = <String>[
      for (final entry in inDegree.entries)
        if (entry.value == 0) entry.key
    ];
    final result = <String>[];

    while (queue.isNotEmpty) {
      final current = queue.removeAt(0);
      result.add(current);

      for (final neighbor in _dependencyGraph[current] ?? {}) {
        inDegree[neighbor] = inDegree[neighbor]! - 1;
        if (inDegree[neighbor] == 0) {
          queue.add(neighbor);
        }
      }
    }

    _executionOrder = result;
    return result;
  }

  /// Get tools ready to execute (all dependencies satisfied)
  List<ToolInvocation> getReadyTools(Set<String> completed) {
    return plan.tools
        .where((t) =>
            !completed.contains(t.toolId) &&
            t.dependencies.every((d) => completed.contains(d)))
        .toList();
  }

  /// Get all tools that depend on the given tool (transitively)
  Set<String> getDependents(String toolId) {
    final result = <String>{};
    final queue = [toolId];

    while (queue.isNotEmpty) {
      final current = queue.removeAt(0);
      for (final dep in _dependencyGraph[current] ?? {}) {
        if (result.add(dep)) {
          queue.add(dep);
        }
      }
    }

    return result;
  }
}
```

### §3.2 ToolResult

```dart
/// Result of executing a single tool
class ToolResult {
  final String toolId;
  final ToolExecutionState state;
  final Map<String, dynamic>? output;
  final List<ProtocolEvent> events;
  final Duration executionTime;
  final int retryCount;
  final ToolError? error;

  const ToolResult({
    required this.toolId,
    required this.state,
    this.output,
    this.events = const [],
    required this.executionTime,
    this.retryCount = 0,
    this.error,
  });

  Map<String, dynamic> toJson() => {
        'toolId': toolId,
        'state': state.name,
        if (output != null) 'output': output,
        'events': events.map((e) => e.toJson()).toList(),
        'executionTimeMs': executionTime.inMilliseconds,
        'retryCount': retryCount,
        if (error != null) 'error': error!.toJson(),
      };
}

/// Tool execution states
enum ToolExecutionState {
  pending,
  running,
  success,
  failed,
  skipped,
  timeout,
}
```

### §3.3 ExecutionResult

```dart
/// Result of executing a complete Plan JSON
class ExecutionResult {
  final String planId;
  final bool success;
  final bool canReplan;
  final List<String> failedTools;
  final Set<String> disabledSkills;
  final List<ToolResult> toolResults;
  final Map<String, dynamic> aggregatedState;
  final List<AssetReference> aggregatedAssets;
  final Duration executionTime;
  final int attemptNumber;

  const ExecutionResult({
    required this.planId,
    required this.success,
    required this.canReplan,
    this.failedTools = const [],
    this.disabledSkills = const {},
    required this.toolResults,
    this.aggregatedState = const {},
    this.aggregatedAssets = const [],
    required this.executionTime,
    this.attemptNumber = 1,
  });

  Map<String, dynamic> toJson() => {
        'planId': planId,
        'success': success,
        'canReplan': canReplan,
        'failedTools': failedTools,
        'disabledSkills': disabledSkills.toList(),
        'toolResults': toolResults.map((r) => r.toJson()).toList(),
        'aggregatedState': aggregatedState,
        'aggregatedAssets': aggregatedAssets.map((a) => a.toJson()).toList(),
        'executionTimeMs': executionTime.inMilliseconds,
        'attemptNumber': attemptNumber,
      };
}
```

### §3.4 ToolError

```dart
/// Error details when a tool fails
class ToolError {
  final String code;
  final String message;
  final ToolErrorCategory category;
  final Map<String, dynamic>? details;

  const ToolError({
    required this.code,
    required this.message,
    required this.category,
    this.details,
  });

  Map<String, dynamic> toJson() => {
        'code': code,
        'message': message,
        'category': category.name,
        if (details != null) 'details': details,
      };
}

enum ToolErrorCategory {
  toolFailure,
  circularDependency,
  timeout,
  invalidJson,
  processError,
}
```

---

## §4 Protocol Events (Sealed Class Hierarchy)

```dart
/// Base class for all protocol events
sealed class ProtocolEvent {
  final String version;
  final String? requestId;
  final DateTime? timestamp;

  const ProtocolEvent({
    this.version = '0',
    this.requestId,
    this.timestamp,
  });

  factory ProtocolEvent.fromJson(Map<String, dynamic> json) {
    final type = json['type'] as String;
    return switch (type) {
      'log' => LogEvent.fromJson(json),
      'state_patch' => StatePatchEvent.fromJson(json),
      'asset' => AssetEvent.fromJson(json),
      'ui_event' => UiEventEvent.fromJson(json),
      'error' => ErrorEvent.fromJson(json),
      'done' => DoneEvent.fromJson(json),
      _ => throw UnknownEventTypeException(type),
    };
  }

  Map<String, dynamic> toJson();
}

/// Log event for progress/diagnostic information
class LogEvent extends ProtocolEvent {
  final LogLevel level;
  final String message;
  final Map<String, dynamic>? fields;

  const LogEvent({
    required this.level,
    required this.message,
    this.fields,
    super.version,
    super.requestId,
    super.timestamp,
  });

  factory LogEvent.fromJson(Map<String, dynamic> json) {
    return LogEvent(
      level: LogLevel.values.byName(json['level'] as String),
      message: json['message'] as String,
      fields: json['fields'] as Map<String, dynamic>?,
      version: json['version'] as String? ?? '0',
      requestId: json['requestId'] as String?,
      timestamp: json['timestamp'] != null
          ? DateTime.parse(json['timestamp'] as String)
          : null,
    );
  }

  @override
  Map<String, dynamic> toJson() => {
        'version': version,
        'type': 'log',
        'level': level.name,
        'message': message,
        if (fields != null) 'fields': fields,
        if (requestId != null) 'requestId': requestId,
        if (timestamp != null) 'timestamp': timestamp!.toIso8601String(),
      };
}

enum LogLevel { debug, info, warn, error }

/// State patch event for session state updates
class StatePatchEvent extends ProtocolEvent {
  final Map<String, dynamic> patch;

  const StatePatchEvent({
    required this.patch,
    super.version,
    super.requestId,
    super.timestamp,
  });

  factory StatePatchEvent.fromJson(Map<String, dynamic> json) {
    return StatePatchEvent(
      patch: json['patch'] as Map<String, dynamic>,
      version: json['version'] as String? ?? '0',
      requestId: json['requestId'] as String?,
      timestamp: json['timestamp'] != null
          ? DateTime.parse(json['timestamp'] as String)
          : null,
    );
  }

  @override
  Map<String, dynamic> toJson() => {
        'version': version,
        'type': 'state_patch',
        'patch': patch,
        if (requestId != null) 'requestId': requestId,
        if (timestamp != null) 'timestamp': timestamp!.toIso8601String(),
      };
}

/// Asset event for generated content
class AssetEvent extends ProtocolEvent {
  final String assetId;
  final String kind;
  final String mediaType;
  final String path;
  final Map<String, dynamic>? metadata;

  const AssetEvent({
    required this.assetId,
    required this.kind,
    required this.mediaType,
    required this.path,
    this.metadata,
    super.version,
    super.requestId,
    super.timestamp,
  });

  factory AssetEvent.fromJson(Map<String, dynamic> json) {
    return AssetEvent(
      assetId: json['assetId'] as String,
      kind: json['kind'] as String,
      mediaType: json['mediaType'] as String,
      path: json['path'] as String,
      metadata: json['metadata'] as Map<String, dynamic>?,
      version: json['version'] as String? ?? '0',
      requestId: json['requestId'] as String?,
      timestamp: json['timestamp'] != null
          ? DateTime.parse(json['timestamp'] as String)
          : null,
    );
  }

  @override
  Map<String, dynamic> toJson() => {
        'version': version,
        'type': 'asset',
        'assetId': assetId,
        'kind': kind,
        'mediaType': mediaType,
        'path': path,
        if (metadata != null) 'metadata': metadata,
        if (requestId != null) 'requestId': requestId,
        if (timestamp != null) 'timestamp': timestamp!.toIso8601String(),
      };
}

/// UI event for requesting UI actions
class UiEventEvent extends ProtocolEvent {
  final String event;
  final Map<String, dynamic>? payload;

  const UiEventEvent({
    required this.event,
    this.payload,
    super.version,
    super.requestId,
    super.timestamp,
  });

  factory UiEventEvent.fromJson(Map<String, dynamic> json) {
    return UiEventEvent(
      event: json['event'] as String,
      payload: json['payload'] as Map<String, dynamic>?,
      version: json['version'] as String? ?? '0',
      requestId: json['requestId'] as String?,
      timestamp: json['timestamp'] != null
          ? DateTime.parse(json['timestamp'] as String)
          : null,
    );
  }

  @override
  Map<String, dynamic> toJson() => {
        'version': version,
        'type': 'ui_event',
        'event': event,
        if (payload != null) 'payload': payload,
        if (requestId != null) 'requestId': requestId,
        if (timestamp != null) 'timestamp': timestamp!.toIso8601String(),
      };
}

/// Error event for structured errors
class ErrorEvent extends ProtocolEvent {
  final String errorCode;
  final String errorMessage;
  final Map<String, dynamic>? details;

  const ErrorEvent({
    required this.errorCode,
    required this.errorMessage,
    this.details,
    super.version,
    super.requestId,
    super.timestamp,
  });

  factory ErrorEvent.fromJson(Map<String, dynamic> json) {
    return ErrorEvent(
      errorCode: json['errorCode'] as String,
      errorMessage: json['errorMessage'] as String,
      details: json['details'] as Map<String, dynamic>?,
      version: json['version'] as String? ?? '0',
      requestId: json['requestId'] as String?,
      timestamp: json['timestamp'] != null
          ? DateTime.parse(json['timestamp'] as String)
          : null,
    );
  }

  @override
  Map<String, dynamic> toJson() => {
        'version': version,
        'type': 'error',
        'errorCode': errorCode,
        'errorMessage': errorMessage,
        if (details != null) 'details': details,
        if (requestId != null) 'requestId': requestId,
        if (timestamp != null) 'timestamp': timestamp!.toIso8601String(),
      };
}

/// Done event signaling completion
class DoneEvent extends ProtocolEvent {
  final bool ok;
  final String? summary;

  const DoneEvent({
    required this.ok,
    this.summary,
    super.version,
    super.requestId,
    super.timestamp,
  });

  factory DoneEvent.fromJson(Map<String, dynamic> json) {
    return DoneEvent(
      ok: json['ok'] as bool,
      summary: json['summary'] as String?,
      version: json['version'] as String? ?? '0',
      requestId: json['requestId'] as String?,
      timestamp: json['timestamp'] != null
          ? DateTime.parse(json['timestamp'] as String)
          : null,
    );
  }

  @override
  Map<String, dynamic> toJson() => {
        'version': version,
        'type': 'done',
        'ok': ok,
        if (summary != null) 'summary': summary,
        if (requestId != null) 'requestId': requestId,
        if (timestamp != null) 'timestamp': timestamp!.toIso8601String(),
      };
}
```

---

## §5 Session State with Deep Merge

```dart
/// Session state container with deep merge support
class SessionState {
  final Map<String, dynamic> _data;

  SessionState([Map<String, dynamic>? initial])
      : _data = Map<String, dynamic>.from(initial ?? {});

  /// Current state (immutable view)
  Map<String, dynamic> get data => Map.unmodifiable(_data);

  /// Apply a state patch using deep merge semantics
  void applyPatch(Map<String, dynamic> patch) {
    _deepMerge(_data, patch);
  }

  /// Get a value by dot-notation path (e.g., "inventory.torch.lit")
  T? get<T>(String path) {
    final keys = path.split('.');
    dynamic current = _data;
    for (final key in keys) {
      if (current is Map<String, dynamic> && current.containsKey(key)) {
        current = current[key];
      } else {
        return null;
      }
    }
    return current as T?;
  }

  /// Deep merge implementation per Spec 002 §6
  static void _deepMerge(
    Map<String, dynamic> target,
    Map<String, dynamic> patch,
  ) {
    for (final entry in patch.entries) {
      final key = entry.key;
      final value = entry.value;

      if (value == null) {
        // Null removes key
        target.remove(key);
      } else if (value is Map<String, dynamic> &&
          target[key] is Map<String, dynamic>) {
        // Recursively merge objects
        _deepMerge(target[key] as Map<String, dynamic>, value);
      } else if (value is List) {
        // Arrays are replaced entirely
        target[key] = List.from(value);
      } else {
        // Primitives replace
        target[key] = value;
      }
    }
  }

  /// Create a copy of the current state
  SessionState copy() => SessionState(Map<String, dynamic>.from(_data));
}
```

---

## §6 Asset Reference

```dart
/// Reference to a generated asset
class AssetReference {
  final String assetId;
  final String kind;
  final String mediaType;
  final String path;
  final String? toolId;
  final Map<String, dynamic>? metadata;

  const AssetReference({
    required this.assetId,
    required this.kind,
    required this.mediaType,
    required this.path,
    this.toolId,
    this.metadata,
  });

  factory AssetReference.fromEvent(AssetEvent event, {String? toolId}) {
    return AssetReference(
      assetId: event.assetId,
      kind: event.kind,
      mediaType: event.mediaType,
      path: event.path,
      toolId: toolId,
      metadata: event.metadata,
    );
  }

  Map<String, dynamic> toJson() => {
        'assetId': assetId,
        'kind': kind,
        'mediaType': mediaType,
        'path': path,
        if (toolId != null) 'toolId': toolId,
        if (metadata != null) 'metadata': metadata,
      };
}
```

---

## §7 Exception Hierarchy

```dart
/// Base exception for Narratoria errors
abstract class NarratoriaException implements Exception {
  final String message;
  const NarratoriaException(this.message);
  @override
  String toString() => '$runtimeType: $message';
}

/// Circular dependency detected in plan
class CyclicDependencyException extends NarratoriaException {
  final String planId;
  CyclicDependencyException(this.planId)
      : super('Circular dependency detected in plan $planId');
}

/// Plan execution failed
class PlanExecutionException extends NarratoriaException {
  final String planId;
  final List<String> failedTools;
  PlanExecutionException(this.planId, this.failedTools)
      : super('Plan $planId execution failed. Failed tools: $failedTools');
}

/// Tool execution timed out
class ToolTimeoutException extends NarratoriaException {
  final String toolId;
  final Duration timeout;
  ToolTimeoutException(this.toolId, this.timeout)
      : super('Tool $toolId timed out after ${timeout.inSeconds}s');
}

/// Protocol violation (invalid JSON, unknown event type, etc.)
class ProtocolViolationException extends NarratoriaException {
  final String? toolId;
  final String violation;
  ProtocolViolationException(this.violation, {this.toolId})
      : super('Protocol violation${toolId != null ? ' from $toolId' : ''}: $violation');
}

/// Unknown event type received
class UnknownEventTypeException extends NarratoriaException {
  final String eventType;
  UnknownEventTypeException(this.eventType)
      : super('Unknown event type: $eventType');
}

/// Skill script not found
class SkillScriptNotFoundError extends NarratoriaException {
  final String skillName;
  final String scriptName;
  SkillScriptNotFoundError(this.skillName, this.scriptName)
      : super('Script $scriptName not found in skill $skillName');
}

/// Max replan attempts exceeded
class MaxReplanAttemptsException extends NarratoriaException {
  final int attempts;
  MaxReplanAttemptsException(this.attempts)
      : super('Max replan attempts ($attempts) exceeded');
}
```

---

## Related Documents

- [Spec 002 Algorithms](../002-plan-generation-skills/data-model.md)
- [Plan JSON Schema](../002-plan-generation-skills/contracts/plan-json.schema.json)
- [Execution Result Schema](../002-plan-generation-skills/contracts/execution-result.schema.json)
- [Skill Manifest Schema](../002-plan-generation-skills/contracts/skill-manifest.schema.json)
