// Tool Execution Status Model
// T014: Track running tools, events, exit codes

import 'protocol_events.dart';

/// Status of a single tool during execution.
enum ToolStatus {
  /// Waiting for dependencies
  pending,
  /// Currently executing
  running,
  /// Completed successfully (exit 0, done.ok=true)
  success,
  /// Completed with logical failure (exit 0, done.ok=false)
  failed,
  /// Protocol or process error (non-zero exit, missing done, etc.)
  error,
}

/// Tracks the execution state of a single tool.
class ToolExecutionStatus {
  /// The tool ID from the Plan JSON
  final String toolId;
  
  /// Path to the tool executable
  final String toolPath;
  
  /// Current execution status
  ToolStatus status;
  
  /// All events received from this tool
  final List<ProtocolEvent> events;
  
  /// Log events only (for quick access)
  List<LogEvent> get logEvents => 
      events.whereType<LogEvent>().toList();
  
  /// The done event (if received)
  DoneEvent? get doneEvent => 
      events.whereType<DoneEvent>().firstOrNull;
  
  /// Error events
  List<ErrorEvent> get errorEvents => 
      events.whereType<ErrorEvent>().toList();
  
  /// Process exit code (null if still running)
  int? exitCode;
  
  /// Error message if status is error
  String? errorMessage;
  
  /// Start time of execution
  DateTime? startTime;
  
  /// End time of execution  
  DateTime? endTime;
  
  /// Duration of execution
  Duration? get duration {
    if (startTime == null) return null;
    final end = endTime ?? DateTime.now();
    return end.difference(startTime!);
  }

  ToolExecutionStatus({
    required this.toolId,
    required this.toolPath,
    this.status = ToolStatus.pending,
    List<ProtocolEvent>? events,
    this.exitCode,
    this.errorMessage,
    this.startTime,
    this.endTime,
  }) : events = events ?? [];

  /// Mark as running
  void markRunning() {
    status = ToolStatus.running;
    startTime = DateTime.now();
  }

  /// Add an event received from the tool
  void addEvent(ProtocolEvent event) {
    events.add(event);
  }

  /// Mark as completed with exit code
  void markCompleted(int code) {
    exitCode = code;
    endTime = DateTime.now();
    
    if (code != 0) {
      // Non-zero exit = protocol failure
      status = ToolStatus.error;
      errorMessage = 'Process exited with code $code';
    } else if (doneEvent == null) {
      // Exit 0 but no done event = protocol violation
      status = ToolStatus.error;
      errorMessage = 'Missing done event';
    } else if (doneEvent!.ok) {
      status = ToolStatus.success;
    } else {
      status = ToolStatus.failed;
      errorMessage = doneEvent!.summary ?? 'Tool reported failure';
    }
  }

  /// Mark as error with message
  void markError(String message) {
    status = ToolStatus.error;
    errorMessage = message;
    endTime = DateTime.now();
  }
}

/// Tracks execution of an entire Plan JSON.
class PlanExecutionStatus {
  /// The Plan request ID
  final String requestId;
  
  /// Status of each tool
  final Map<String, ToolExecutionStatus> tools;
  
  /// Plan-level start time
  DateTime? startTime;
  
  /// Plan-level end time
  DateTime? endTime;
  
  /// Overall plan duration
  Duration? get duration {
    if (startTime == null) return null;
    final end = endTime ?? DateTime.now();
    return end.difference(startTime!);
  }

  /// Is the plan currently executing?
  bool get isRunning => tools.values.any((t) => t.status == ToolStatus.running);

  /// Did all tools complete successfully?
  bool get isSuccess => tools.values.every((t) => t.status == ToolStatus.success);

  /// Did any tool fail or error?
  bool get hasFailures => tools.values.any(
    (t) => t.status == ToolStatus.failed || t.status == ToolStatus.error,
  );

  /// Are all tools complete (success, failed, or error)?
  bool get isComplete => tools.values.every(
    (t) => t.status != ToolStatus.pending && t.status != ToolStatus.running,
  );

  PlanExecutionStatus({
    required this.requestId,
    Map<String, ToolExecutionStatus>? tools,
    this.startTime,
    this.endTime,
  }) : tools = tools ?? {};

  /// Get status for a specific tool
  ToolExecutionStatus? getToolStatus(String toolId) => tools[toolId];

  /// Add a tool to track
  void addTool(String toolId, String toolPath) {
    tools[toolId] = ToolExecutionStatus(
      toolId: toolId,
      toolPath: toolPath,
    );
  }
}
