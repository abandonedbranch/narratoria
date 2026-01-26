// Plan Executor Service
// T013: Dependency resolution, sequential/parallel execution, failure handling

import 'dart:async';

import 'package:flutter/foundation.dart';

import '../models/plan_json.dart';
import '../models/protocol_events.dart';
import '../models/tool_execution_status.dart';
import 'tool_invoker.dart';

/// Result of executing a Plan JSON.
class PlanExecutionResult {
  /// The request ID from the plan
  final String requestId;
  
  /// Results for each tool
  final Map<String, ToolInvocationResult> toolResults;
  
  /// Overall success (all tools succeeded)
  final bool success;
  
  /// Status tracking object
  final PlanExecutionStatus status;
  
  /// Error message if plan failed
  final String? errorMessage;

  const PlanExecutionResult({
    required this.requestId,
    required this.toolResults,
    required this.success,
    required this.status,
    this.errorMessage,
  });
}

/// Service for executing Plan JSON documents.
/// 
/// Per Spec 001 §13.3: Resolves tool dependencies and executes
/// tools in the correct order (sequential or parallel as specified).
class PlanExecutor extends ChangeNotifier {
  final ToolInvoker _toolInvoker;
  
  /// Currently executing plan status (null if idle)
  PlanExecutionStatus? _currentExecution;
  
  /// Get current execution status for UI binding
  PlanExecutionStatus? get currentExecution => _currentExecution;
  
  /// Base path for tool resolution (prepended to relative tool paths)
  final String? toolBasePath;

  PlanExecutor({
    ToolInvoker? toolInvoker,
    this.toolBasePath,
  }) : _toolInvoker = toolInvoker ?? ToolInvoker();

  /// Execute a Plan JSON document.
  /// 
  /// [plan] - The plan to execute
  /// [onEvent] - Callback for events from all tools
  /// [stopOnFailure] - If true, stop execution on first tool failure
  /// 
  /// Returns a PlanExecutionResult with all tool results.
  Future<PlanExecutionResult> execute({
    required PlanJson plan,
    void Function(String toolId, ProtocolEvent event)? onEvent,
    bool stopOnFailure = true,
  }) async {
    // Initialize execution status
    _currentExecution = PlanExecutionStatus(requestId: plan.requestId);
    _currentExecution!.startTime = DateTime.now();
    
    // Add all tools to status tracking
    for (final tool in plan.tools) {
      _currentExecution!.addTool(tool.toolId, tool.toolPath);
    }
    notifyListeners();
    
    final toolResults = <String, ToolInvocationResult>{};
    String? errorMessage;
    
    try {
      if (plan.parallel && _canRunAllInParallel(plan)) {
        // Execute all tools in parallel
        await _executeParallel(
          plan: plan,
          toolResults: toolResults,
          onEvent: onEvent,
        );
      } else {
        // Execute tools respecting dependencies
        await _executeWithDependencies(
          plan: plan,
          toolResults: toolResults,
          onEvent: onEvent,
          stopOnFailure: stopOnFailure,
        );
      }
    } catch (e) {
      errorMessage = 'Plan execution error: $e';
    }
    
    _currentExecution!.endTime = DateTime.now();
    notifyListeners();
    
    final success = toolResults.values.every((r) => r.success);
    
    return PlanExecutionResult(
      requestId: plan.requestId,
      toolResults: toolResults,
      success: success,
      status: _currentExecution!,
      errorMessage: success ? null : (errorMessage ?? 'One or more tools failed'),
    );
  }

  /// Check if all tools can run in parallel (no dependencies).
  bool _canRunAllInParallel(PlanJson plan) {
    return plan.tools.every((t) => t.dependencies.isEmpty);
  }

  /// Execute all tools in parallel.
  Future<void> _executeParallel({
    required PlanJson plan,
    required Map<String, ToolInvocationResult> toolResults,
    void Function(String toolId, ProtocolEvent event)? onEvent,
  }) async {
    final futures = <Future<void>>[];
    
    for (final tool in plan.tools) {
      futures.add(_executeTool(
        tool: tool,
        plan: plan,
        toolResults: toolResults,
        onEvent: onEvent,
      ));
    }
    
    await Future.wait(futures);
  }

  /// Execute tools respecting dependency order.
  Future<void> _executeWithDependencies({
    required PlanJson plan,
    required Map<String, ToolInvocationResult> toolResults,
    void Function(String toolId, ProtocolEvent event)? onEvent,
    required bool stopOnFailure,
  }) async {
    // Build dependency graph
    final completed = <String>{};
    final remaining = plan.tools.toList();
    
    while (remaining.isNotEmpty) {
      // Find tools whose dependencies are all satisfied
      final ready = remaining.where((t) {
        return t.dependencies.every((dep) => completed.contains(dep));
      }).toList();
      
      if (ready.isEmpty && remaining.isNotEmpty) {
        // Circular dependency or missing dependency
        throw StateError(
          'Circular or unsatisfied dependencies: '
          '${remaining.map((t) => t.toolId).join(", ")}',
        );
      }
      
      // Execute ready tools (in parallel if plan.parallel, else sequential)
      if (plan.parallel) {
        final futures = ready.map((tool) => _executeTool(
          tool: tool,
          plan: plan,
          toolResults: toolResults,
          onEvent: onEvent,
        ));
        await Future.wait(futures);
      } else {
        for (final tool in ready) {
          await _executeTool(
            tool: tool,
            plan: plan,
            toolResults: toolResults,
            onEvent: onEvent,
          );
          
          // Check if we should stop on failure
          if (stopOnFailure && !toolResults[tool.toolId]!.success) {
            // Mark remaining tools as pending and return
            return;
          }
        }
      }
      
      // Move executed tools to completed
      for (final tool in ready) {
        completed.add(tool.toolId);
        remaining.remove(tool);
      }
    }
  }

  /// Execute a single tool.
  Future<void> _executeTool({
    required ToolInvocation tool,
    required PlanJson plan,
    required Map<String, ToolInvocationResult> toolResults,
    void Function(String toolId, ProtocolEvent event)? onEvent,
  }) async {
    final status = _currentExecution!.getToolStatus(tool.toolId)!;
    
    // Resolve tool path
    String toolPath = tool.toolPath;
    if (toolBasePath != null && !toolPath.startsWith('/')) {
      toolPath = '$toolBasePath/$toolPath';
    }
    
    final result = await _toolInvoker.invokeWithStatus(
      toolPath: toolPath,
      input: tool.input,
      status: status,
      requestId: plan.requestId,
      onStatusChanged: notifyListeners,
    );
    
    toolResults[tool.toolId] = result;
    
    // Forward events to callback
    if (onEvent != null) {
      for (final event in result.events) {
        onEvent(tool.toolId, event);
      }
    }
  }

  /// Clear current execution status.
  void clearExecution() {
    _currentExecution = null;
    notifyListeners();
  }
}
