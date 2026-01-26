// Torch Lighter Tool
// T044: Example tool that emits log, state_patch, asset, done events

import 'dart:convert';
import 'dart:io';

void main(List<String> args) async {
  // Read input from stdin
  final input = await stdin.transform(utf8.decoder).join();
  Map<String, dynamic> inputJson = {};
  String? requestId;
  
  try {
    if (input.trim().isNotEmpty) {
      inputJson = jsonDecode(input) as Map<String, dynamic>;
      requestId = inputJson['requestId'] as String?;
    }
  } catch (e) {
    _emitError(requestId, 'INVALID_INPUT', 'Failed to parse input JSON: $e');
    _emitDone(requestId, ok: false, summary: 'Input validation failed');
    exit(0);
  }

  final torchId = inputJson['torch_id'] as String? ?? 'default_torch';
  final action = inputJson['action'] as String? ?? 'light';

  // Log: Starting
  _emitLog(requestId, 'info', 'Processing torch action: $action for $torchId');

  // Simulate some work
  await Future.delayed(const Duration(milliseconds: 100));

  if (action == 'light') {
    // Log: Progress
    _emitLog(requestId, 'debug', 'Igniting torch...');
    
    // State patch: Update torch state
    _emitStatePatch(requestId, {
      'inventory': {
        torchId: {
          'lit': true,
          'fuel': 100,
        },
      },
    });
    
    _emitLog(requestId, 'info', 'Torch is now lit, providing warm light');
    
    // Asset: Reference the torch image
    final assetPath = _getAssetPath('torch_lit.png');
    if (File(assetPath).existsSync()) {
      _emitAsset(
        requestId: requestId,
        assetId: '${torchId}_image',
        kind: 'image',
        mediaType: 'image/png',
        path: assetPath,
        metadata: {'width': 512, 'height': 512},
      );
    } else {
      _emitLog(requestId, 'warn', 'Asset not found: $assetPath (this is expected in tests)');
    }
    
    // Done: Success
    _emitDone(requestId, ok: true, summary: 'Torch lit successfully');
    
  } else if (action == 'extinguish') {
    _emitLog(requestId, 'info', 'Extinguishing torch...');
    
    _emitStatePatch(requestId, {
      'inventory': {
        torchId: {
          'lit': false,
        },
      },
    });
    
    _emitDone(requestId, ok: true, summary: 'Torch extinguished');
    
  } else {
    _emitError(requestId, 'UNKNOWN_ACTION', 'Unknown action: $action');
    _emitDone(requestId, ok: false, summary: 'Unknown action requested');
  }
}

String _getAssetPath(String filename) {
  // Get the directory where this tool is located
  final scriptPath = Platform.script.toFilePath();
  final toolDir = File(scriptPath).parent.path;
  return '$toolDir/assets/$filename';
}

void _emitLog(String? requestId, String level, String message, [Map<String, dynamic>? fields]) {
  _emit({
    'version': '0',
    'type': 'log',
    if (requestId != null) 'requestId': requestId,
    'timestamp': DateTime.now().toUtc().toIso8601String(),
    'level': level,
    'message': message,
    if (fields != null) 'fields': fields,
  });
}

void _emitStatePatch(String? requestId, Map<String, dynamic> patch) {
  _emit({
    'version': '0',
    'type': 'state_patch',
    if (requestId != null) 'requestId': requestId,
    'timestamp': DateTime.now().toUtc().toIso8601String(),
    'patch': patch,
  });
}

void _emitAsset({
  String? requestId,
  required String assetId,
  required String kind,
  required String mediaType,
  required String path,
  Map<String, dynamic>? metadata,
}) {
  _emit({
    'version': '0',
    'type': 'asset',
    if (requestId != null) 'requestId': requestId,
    'timestamp': DateTime.now().toUtc().toIso8601String(),
    'assetId': assetId,
    'kind': kind,
    'mediaType': mediaType,
    'path': path,
    if (metadata != null) 'metadata': metadata,
  });
}

void _emitError(String? requestId, String code, String message, [Map<String, dynamic>? details]) {
  _emit({
    'version': '0',
    'type': 'error',
    if (requestId != null) 'requestId': requestId,
    'timestamp': DateTime.now().toUtc().toIso8601String(),
    'errorCode': code,
    'errorMessage': message,
    if (details != null) 'details': details,
  });
}

void _emitDone(String? requestId, {required bool ok, String? summary}) {
  _emit({
    'version': '0',
    'type': 'done',
    if (requestId != null) 'requestId': requestId,
    'timestamp': DateTime.now().toUtc().toIso8601String(),
    'ok': ok,
    if (summary != null) 'summary': summary,
  });
}

void _emit(Map<String, dynamic> event) {
  stdout.writeln(jsonEncode(event));
}
