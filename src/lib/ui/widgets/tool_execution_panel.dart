// Tool Execution Panel Widget
// T015: Display tool name, status, streaming logs

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../models/protocol_events.dart';
import '../../models/tool_execution_status.dart';
import '../../services/plan_executor.dart';
import '../theme.dart';

/// Panel displaying tool execution status and streaming logs.
/// 
/// Per Spec 001 §12.2: Tool Execution Panel shows:
/// - Active tool names and status indicators
/// - Streaming log events color-coded by level
/// - Asset previews as they arrive
class ToolExecutionPanel extends StatelessWidget {
  const ToolExecutionPanel({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<PlanExecutor>(
      builder: (context, executor, _) {
        final execution = executor.currentExecution;
        
        if (execution == null) {
          return const _EmptyState();
        }
        
        return _ExecutionView(execution: execution);
      },
    );
  }
}

class _EmptyState extends StatelessWidget {
  const _EmptyState();

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            Icons.build_outlined,
            size: 64,
            color: Theme.of(context).colorScheme.outline,
          ),
          const SizedBox(height: 16),
          Text(
            'No Active Execution',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 8),
          Text(
            'Tool logs will appear here when a plan is executed',
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
              color: Theme.of(context).colorScheme.outline,
            ),
          ),
        ],
      ),
    );
  }
}

class _ExecutionView extends StatelessWidget {
  final PlanExecutionStatus execution;
  
  const _ExecutionView({required this.execution});

  @override
  Widget build(BuildContext context) {
    final tools = execution.tools.values.toList();
    
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _ExecutionHeader(execution: execution),
        const Divider(),
        Expanded(
          child: ListView.builder(
            itemCount: tools.length,
            itemBuilder: (context, index) => _ToolCard(
              status: tools[index],
            ),
          ),
        ),
      ],
    );
  }
}

class _ExecutionHeader extends StatelessWidget {
  final PlanExecutionStatus execution;
  
  const _ExecutionHeader({required this.execution});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Row(
        children: [
          if (execution.isRunning)
            const SizedBox(
              width: 20,
              height: 20,
              child: CircularProgressIndicator(strokeWidth: 2),
            )
          else if (execution.isSuccess)
            const Icon(Icons.check_circle, color: NarratoriaTheme.successColor)
          else if (execution.hasFailures)
            const Icon(Icons.error, color: NarratoriaTheme.errorColor)
          else
            const Icon(Icons.pending_outlined),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Plan: ${execution.requestId}',
                  style: theme.textTheme.titleMedium,
                ),
                if (execution.duration != null)
                  Text(
                    'Duration: ${_formatDuration(execution.duration!)}',
                    style: theme.textTheme.bodySmall,
                  ),
              ],
            ),
          ),
          _StatusBadge(
            isRunning: execution.isRunning,
            isSuccess: execution.isSuccess,
            hasFailures: execution.hasFailures,
          ),
        ],
      ),
    );
  }

  String _formatDuration(Duration d) {
    if (d.inSeconds < 1) {
      return '${d.inMilliseconds}ms';
    } else if (d.inMinutes < 1) {
      return '${d.inSeconds}.${(d.inMilliseconds % 1000) ~/ 100}s';
    } else {
      return '${d.inMinutes}m ${d.inSeconds % 60}s';
    }
  }
}

class _StatusBadge extends StatelessWidget {
  final bool isRunning;
  final bool isSuccess;
  final bool hasFailures;
  
  const _StatusBadge({
    required this.isRunning,
    required this.isSuccess,
    required this.hasFailures,
  });

  @override
  Widget build(BuildContext context) {
    final (label, color) = _getStatusInfo();
    
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.2),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: color),
      ),
      child: Text(
        label,
        style: TextStyle(color: color, fontWeight: FontWeight.bold),
      ),
    );
  }

  (String, Color) _getStatusInfo() {
    if (isRunning) return ('RUNNING', Colors.blue);
    if (isSuccess) return ('SUCCESS', NarratoriaTheme.successColor);
    if (hasFailures) return ('FAILED', NarratoriaTheme.errorColor);
    return ('PENDING', Colors.grey);
  }
}

class _ToolCard extends StatelessWidget {
  final ToolExecutionStatus status;
  
  const _ToolCard({required this.status});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: ExpansionTile(
        leading: _buildStatusIcon(),
        title: Text(status.toolId),
        subtitle: Text(
          status.toolPath,
          style: theme.textTheme.bodySmall,
          overflow: TextOverflow.ellipsis,
        ),
        trailing: status.duration != null
            ? Text(
                '${status.duration!.inMilliseconds}ms',
                style: theme.textTheme.bodySmall,
              )
            : null,
        children: [
          if (status.events.isNotEmpty)
            _LogList(events: status.events)
          else
            const Padding(
              padding: EdgeInsets.all(16),
              child: Text('No events yet'),
            ),
          if (status.errorMessage != null)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              margin: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: NarratoriaTheme.errorColor.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Text(
                status.errorMessage!,
                style: TextStyle(color: NarratoriaTheme.errorColor),
              ),
            ),
        ],
      ),
    );
  }

  Widget _buildStatusIcon() {
    return switch (status.status) {
      ToolStatus.pending => const Icon(Icons.pending_outlined, color: Colors.grey),
      ToolStatus.running => const SizedBox(
          width: 24,
          height: 24,
          child: CircularProgressIndicator(strokeWidth: 2),
        ),
      ToolStatus.success => const Icon(Icons.check_circle, color: NarratoriaTheme.successColor),
      ToolStatus.failed => const Icon(Icons.cancel, color: NarratoriaTheme.warningColor),
      ToolStatus.error => const Icon(Icons.error, color: NarratoriaTheme.errorColor),
    };
  }
}

class _LogList extends StatelessWidget {
  final List<ProtocolEvent> events;
  
  const _LogList({required this.events});

  @override
  Widget build(BuildContext context) {
    return Container(
      constraints: const BoxConstraints(maxHeight: 300),
      child: ListView.builder(
        shrinkWrap: true,
        padding: const EdgeInsets.all(8),
        itemCount: events.length,
        itemBuilder: (context, index) => _LogEntry(event: events[index]),
      ),
    );
  }
}

class _LogEntry extends StatelessWidget {
  final ProtocolEvent event;
  
  const _LogEntry({required this.event});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 60,
            child: _buildTypeChip(),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: SelectableText(
              _getEventText(),
              style: theme.textTheme.bodySmall?.copyWith(
                fontFamily: 'monospace',
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildTypeChip() {
    final (label, color) = switch (event) {
      LogEvent e => (e.level.toUpperCase(), NarratoriaTheme.logLevelColor(e.level)),
      StatePatchEvent _ => ('PATCH', Colors.purple),
      AssetEvent _ => ('ASSET', Colors.teal),
      UiEvent _ => ('UI', Colors.indigo),
      ErrorEvent _ => ('ERROR', NarratoriaTheme.errorColor),
      DoneEvent e => (e.ok ? 'DONE' : 'FAIL', e.ok ? NarratoriaTheme.successColor : NarratoriaTheme.errorColor),
    };
    
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.2),
        borderRadius: BorderRadius.circular(4),
      ),
      child: Text(
        label,
        style: TextStyle(
          fontSize: 10,
          color: color,
          fontWeight: FontWeight.bold,
        ),
        textAlign: TextAlign.center,
      ),
    );
  }

  String _getEventText() {
    return switch (event) {
      LogEvent e => e.message,
      StatePatchEvent e => 'Applied patch: ${e.patch}',
      AssetEvent e => '${e.kind}: ${e.path}',
      UiEvent e => '${e.event}: ${e.payload}',
      ErrorEvent e => '${e.errorCode}: ${e.errorMessage}',
      DoneEvent e => e.summary ?? (e.ok ? 'Completed successfully' : 'Completed with failure'),
    };
  }
}
