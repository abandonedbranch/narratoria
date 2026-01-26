// Plan JSON Models
// Spec 001 §13.3: Plan JSON schema for tool execution

/// Plan JSON document produced by narrator AI.
/// Spec 001 §13.3
class PlanJson {
  /// Unique identifier for this plan execution
  final String requestId;
  
  /// Optional narrative text to display before/during tool execution
  final String? narrative;
  
  /// Array of tool invocation descriptors
  final List<ToolInvocation> tools;
  
  /// If true, tools may run concurrently when dependencies allow
  final bool parallel;

  const PlanJson({
    required this.requestId,
    this.narrative,
    required this.tools,
    this.parallel = false,
  });

  factory PlanJson.fromJson(Map<String, dynamic> json) {
    return PlanJson(
      requestId: json['requestId'] as String? ?? '',
      narrative: json['narrative'] as String?,
      tools: (json['tools'] as List<dynamic>?)
          ?.map((t) => ToolInvocation.fromJson(t as Map<String, dynamic>))
          .toList() ?? [],
      parallel: json['parallel'] as bool? ?? false,
    );
  }

  Map<String, dynamic> toJson() => {
    'requestId': requestId,
    if (narrative != null) 'narrative': narrative,
    'tools': tools.map((t) => t.toJson()).toList(),
    'parallel': parallel,
  };

  /// Create a copy with modified fields
  PlanJson copyWith({
    String? requestId,
    String? narrative,
    List<ToolInvocation>? tools,
    bool? parallel,
  }) {
    return PlanJson(
      requestId: requestId ?? this.requestId,
      narrative: narrative ?? this.narrative,
      tools: tools ?? this.tools,
      parallel: parallel ?? this.parallel,
    );
  }
}

/// Tool invocation descriptor within a Plan JSON.
/// Spec 001 §13.3
class ToolInvocation {
  /// Unique ID for this tool within the plan (for dependency tracking)
  final String toolId;
  
  /// Path to the tool executable
  final String toolPath;
  
  /// JSON object passed to tool via stdin
  final Map<String, dynamic> input;
  
  /// Tool IDs that must complete before this tool runs
  final List<String> dependencies;

  const ToolInvocation({
    required this.toolId,
    required this.toolPath,
    required this.input,
    this.dependencies = const [],
  });

  factory ToolInvocation.fromJson(Map<String, dynamic> json) {
    return ToolInvocation(
      toolId: json['toolId'] as String? ?? '',
      toolPath: json['toolPath'] as String? ?? '',
      input: json['input'] as Map<String, dynamic>? ?? {},
      dependencies: (json['dependencies'] as List<dynamic>?)
          ?.map((d) => d as String)
          .toList() ?? [],
    );
  }

  Map<String, dynamic> toJson() => {
    'toolId': toolId,
    'toolPath': toolPath,
    'input': input,
    'dependencies': dependencies,
  };

  /// Create a copy with modified fields
  ToolInvocation copyWith({
    String? toolId,
    String? toolPath,
    Map<String, dynamic>? input,
    List<String>? dependencies,
  }) {
    return ToolInvocation(
      toolId: toolId ?? this.toolId,
      toolPath: toolPath ?? this.toolPath,
      input: input ?? this.input,
      dependencies: dependencies ?? this.dependencies,
    );
  }
}
