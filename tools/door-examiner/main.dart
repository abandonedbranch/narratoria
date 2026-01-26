// Door Examiner Tool
// T045: Example tool that emits log, state_patch, ui_event, done events

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

  final doorId = inputJson['door_id'] as String? ?? 'default_door';
  final detailLevel = inputJson['detail_level'] as String? ?? 'basic';

  // Log: Starting
  _emitLog(requestId, 'info', 'Examining door: $doorId');

  // Simulate some work
  await Future.delayed(const Duration(milliseconds: 50));

  // Describe the door based on ID
  final doorDescription = _getDoorDescription(doorId, detailLevel);
  
  _emitLog(requestId, 'debug', 'Generated description for door');

  // State patch: Update world state with door info
  _emitStatePatch(requestId, {
    'world': {
      'doors': {
        doorId: {
          'examined': true,
          'description': doorDescription['short'],
          'material': doorDescription['material'],
          'locked': doorDescription['locked'],
        },
      },
    },
  });

  // UI Event: Present choices to the player
  _emitUiEvent(
    requestId: requestId,
    event: 'narrative_choice',
    payload: {
      'prompt': doorDescription['full'],
      'choices': _getDoorChoices(doorId, doorDescription),
    },
  );

  _emitLog(requestId, 'info', 'Door examination complete');
  
  // Done: Success
  _emitDone(requestId, ok: true, summary: 'Door examined successfully');
}

Map<String, dynamic> _getDoorDescription(String doorId, String detailLevel) {
  // Simulate different door types based on ID
  if (doorId.contains('ancient') || doorId.contains('gate')) {
    return {
      'short': 'An ancient stone gate covered in mysterious runes.',
      'full': 'Before you stands an ancient gate, its surface covered with '
          'glowing runes that pulse with an otherworldly light. The symbols '
          'seem to shift when you look at them directly.',
      'material': 'stone',
      'locked': true,
    };
  } else if (doorId.contains('wooden')) {
    return {
      'short': 'A weathered wooden door with iron bands.',
      'full': 'A simple wooden door, reinforced with iron bands. It looks '
          'sturdy but well-used, with scratches around the handle.',
      'material': 'wood',
      'locked': false,
    };
  } else if (doorId.contains('iron') || doorId.contains('metal')) {
    return {
      'short': 'A heavy iron door with a small barred window.',
      'full': 'A massive iron door blocks your path. Through a small barred '
          'window, you can see flickering torchlight from the other side.',
      'material': 'iron',
      'locked': true,
    };
  } else {
    return {
      'short': 'A plain door.',
      'full': 'You examine the door carefully. It appears to be a standard '
          'door with nothing particularly remarkable about it.',
      'material': 'unknown',
      'locked': false,
    };
  }
}

List<Map<String, String>> _getDoorChoices(String doorId, Map<String, dynamic> description) {
  final choices = <Map<String, String>>[];
  
  if (description['locked'] == true) {
    choices.add({
      'id': 'try_open',
      'label': 'Try to open the door',
      'hint': 'The door appears to be locked',
    });
    choices.add({
      'id': 'search_key',
      'label': 'Search for a key or mechanism',
    });
    if (description['material'] == 'wood') {
      choices.add({
        'id': 'force_open',
        'label': 'Force the door open',
        'hint': 'This may make noise',
      });
    }
  } else {
    choices.add({
      'id': 'open',
      'label': 'Open the door',
    });
  }
  
  choices.add({
    'id': 'leave',
    'label': 'Leave the door alone',
  });
  
  return choices;
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

void _emitUiEvent({
  String? requestId,
  required String event,
  Map<String, dynamic>? payload,
}) {
  _emit({
    'version': '0',
    'type': 'ui_event',
    if (requestId != null) 'requestId': requestId,
    'timestamp': DateTime.now().toUtc().toIso8601String(),
    'event': event,
    if (payload != null) 'payload': payload,
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
