// Tool Invoker Service
// T012, T012a: Process.start, stdin write, stdout NDJSON parsing, protocol error handling

import 'dart:async';
import 'dart:convert';
import 'dart:io';

import '../models/protocol_events.dart';
import '../models/tool_execution_status.dart';

/// Result of a tool invocation.
class ToolInvocationResult {
  /// All events received from the tool
  final List<ProtocolEvent> events;
  
  /// Process exit code
  final int exitCode;
  
  /// Whether the invocation was successful (exit 0 + done.ok)
  final bool success;
  
  /// Error message if failed
  final String? errorMessage;
  
  /// The done event (if received)
  DoneEvent? get doneEvent => events.whereType<DoneEvent>().firstOrNull;

  const ToolInvocationResult({
    required this.events,
    required this.exitCode,
    required this.success,
    this.errorMessage,
  });
}

/// Service for invoking external tools per Spec 001 protocol.
/// 
/// Per constitution principle II: Tools run as independent OS processes
/// communicating via NDJSON on stdout/stdin.
class ToolInvoker {
  /// Default timeout for tool execution
  final Duration defaultTimeout;

  ToolInvoker({
    this.defaultTimeout = const Duration(seconds: 30),
  });

  /// Invoke a tool with the given input.
  /// 
  /// [toolPath] - Path to the tool executable
  /// [input] - JSON object to send via stdin
  /// [requestId] - Optional request ID to include in events
  /// [onEvent] - Callback for streaming events as they arrive
  /// [timeout] - Override default timeout
  /// 
  /// Per Spec 001 §5: Tool MUST emit exactly one done event before exit.
  /// Exit code 0 = protocol intact, check done.ok for logical success.
  /// Non-zero exit = protocol failure.
  Future<ToolInvocationResult> invoke({
    required String toolPath,
    required Map<String, dynamic> input,
    String? requestId,
    void Function(ProtocolEvent)? onEvent,
    Duration? timeout,
  }) async {
    final events = <ProtocolEvent>[];
    String? errorMessage;
    
    try {
      // Start the process
      final process = await Process.start(toolPath, []);
      
      // Send input JSON via stdin and close
      if (requestId != null) {
        input = {...input, 'requestId': requestId};
      }
      process.stdin.writeln(jsonEncode(input));
      await process.stdin.close();
      
      // Parse NDJSON from stdout
      final stdoutCompleter = Completer<void>();
      
      process.stdout
          .transform(utf8.decoder)
          .transform(const LineSplitter())
          .listen(
            (line) {
              if (line.trim().isEmpty) return;
              
              try {
                final event = ProtocolEvent.parseLine(line);
                events.add(event);
                onEvent?.call(event);
              } on ProtocolError catch (e) {
                // Per Spec 001 §5.2: Unknown event types are protocol errors
                // We log but continue processing - the tool may still send done
                events.add(ErrorEvent(
                  version: '0',
                  requestId: requestId,
                  errorCode: 'PROTOCOL_ERROR',
                  errorMessage: e.message,
                ));
                onEvent?.call(events.last);
              }
            },
            onDone: () => stdoutCompleter.complete(),
            onError: (e) => stdoutCompleter.completeError(e),
          );
      
      // Collect stderr for debugging (not parsed as NDJSON)
      final stderrBuffer = StringBuffer();
      process.stderr
          .transform(utf8.decoder)
          .listen((data) => stderrBuffer.write(data));
      
      // Wait for completion with timeout
      final effectiveTimeout = timeout ?? defaultTimeout;
      
      try {
        final exitCode = await process.exitCode.timeout(effectiveTimeout);
        await stdoutCompleter.future.timeout(const Duration(seconds: 1));
        
        // Validate protocol compliance
        final doneEvents = events.whereType<DoneEvent>().toList();
        
        if (exitCode != 0) {
          // Non-zero exit = protocol failure
          errorMessage = 'Process exited with code $exitCode';
          if (stderrBuffer.isNotEmpty) {
            errorMessage = '$errorMessage\nstderr: $stderrBuffer';
          }
          return ToolInvocationResult(
            events: events,
            exitCode: exitCode,
            success: false,
            errorMessage: errorMessage,
          );
        }
        
        if (doneEvents.isEmpty) {
          // Per Spec 001 §4.6: Exactly one done event required
          errorMessage = 'Missing done event (protocol violation)';
          return ToolInvocationResult(
            events: events,
            exitCode: exitCode,
            success: false,
            errorMessage: errorMessage,
          );
        }
        
        if (doneEvents.length > 1) {
          // Multiple done events is also a protocol violation
          errorMessage = 'Multiple done events received (protocol violation)';
          return ToolInvocationResult(
            events: events,
            exitCode: exitCode,
            success: false,
            errorMessage: errorMessage,
          );
        }
        
        final done = doneEvents.first;
        return ToolInvocationResult(
          events: events,
          exitCode: exitCode,
          success: done.ok,
          errorMessage: done.ok ? null : (done.summary ?? 'Tool reported failure'),
        );
        
      } on TimeoutException {
        // Kill the process on timeout
        process.kill(ProcessSignal.sigterm);
        errorMessage = 'Tool execution timed out after ${effectiveTimeout.inSeconds}s';
        return ToolInvocationResult(
          events: events,
          exitCode: -1,
          success: false,
          errorMessage: errorMessage,
        );
      }
      
    } on ProcessException catch (e) {
      // Tool not found or not executable
      errorMessage = 'Failed to start tool: ${e.message}';
      return ToolInvocationResult(
        events: events,
        exitCode: -1,
        success: false,
        errorMessage: errorMessage,
      );
    }
  }

  /// Invoke a tool and track status in a ToolExecutionStatus object.
  /// 
  /// This method provides real-time status updates for UI integration.
  Future<ToolInvocationResult> invokeWithStatus({
    required String toolPath,
    required Map<String, dynamic> input,
    required ToolExecutionStatus status,
    String? requestId,
    void Function()? onStatusChanged,
    Duration? timeout,
  }) async {
    status.markRunning();
    onStatusChanged?.call();
    
    final result = await invoke(
      toolPath: toolPath,
      input: input,
      requestId: requestId,
      timeout: timeout,
      onEvent: (event) {
        status.addEvent(event);
        onStatusChanged?.call();
      },
    );
    
    if (result.errorMessage != null && result.exitCode < 0) {
      status.markError(result.errorMessage!);
    } else {
      status.markCompleted(result.exitCode);
    }
    onStatusChanged?.call();
    
    return result;
  }
}
