// Contract Tests for Protocol Events
// T010: Validates event envelope schema per Spec 001 §3-4

import 'package:flutter_test/flutter_test.dart';
import 'package:narratoria/models/protocol_events.dart';

void main() {
  group('Event Envelope Contract', () {
    test('MUST have version field equal to "0"', () {
      const json = '{"version": "0", "type": "log", "level": "info", "message": "test"}';
      final event = ProtocolEvent.parseLine(json) as LogEvent;
      expect(event.version, equals('0'));
    });

    test('MUST reject unsupported version', () {
      const json = '{"version": "1", "type": "log", "level": "info", "message": "test"}';
      expect(
        () => ProtocolEvent.parseLine(json),
        throwsA(isA<ProtocolError>().having(
          (e) => e.message,
          'message',
          contains('Unsupported protocol version'),
        )),
      );
    });

    test('MUST have type field', () {
      const json = '{"version": "0"}';
      expect(
        () => ProtocolEvent.parseLine(json),
        throwsA(isA<ProtocolError>().having(
          (e) => e.message,
          'message',
          contains('Missing required field: type'),
        )),
      );
    });

    test('MUST reject unknown event types', () {
      const json = '{"version": "0", "type": "unknown_event"}';
      expect(
        () => ProtocolEvent.parseLine(json),
        throwsA(isA<ProtocolError>().having(
          (e) => e.message,
          'message',
          contains('Unknown event type'),
        )),
      );
    });

    test('MAY include optional requestId', () {
      const json = '{"version": "0", "type": "log", "level": "info", "message": "test", "requestId": "req-123"}';
      final event = ProtocolEvent.parseLine(json);
      expect(event.requestId, equals('req-123'));
    });

    test('MAY include optional timestamp', () {
      const json = '{"version": "0", "type": "log", "level": "info", "message": "test", "timestamp": "2024-01-01T00:00:00Z"}';
      final event = ProtocolEvent.parseLine(json);
      expect(event.timestamp, equals('2024-01-01T00:00:00Z'));
    });
  });

  group('LogEvent Contract (§4.1)', () {
    test('MUST have level and message fields', () {
      const json = '{"version": "0", "type": "log", "level": "info", "message": "Starting process"}';
      final event = ProtocolEvent.parseLine(json) as LogEvent;
      
      expect(event.type, equals('log'));
      expect(event.level, equals('info'));
      expect(event.message, equals('Starting process'));
    });

    test('level defaults to "info" if missing', () {
      const json = '{"version": "0", "type": "log", "message": "test"}';
      final event = ProtocolEvent.parseLine(json) as LogEvent;
      expect(event.level, equals('info'));
    });

    test('MAY include optional fields map', () {
      const json = '{"version": "0", "type": "log", "level": "debug", "message": "test", "fields": {"key": "value"}}';
      final event = ProtocolEvent.parseLine(json) as LogEvent;
      expect(event.fields, isNotNull);
      expect(event.fields!['key'], equals('value'));
    });

    test('serializes to correct JSON format', () {
      const event = LogEvent(
        version: '0',
        level: 'warn',
        message: 'Warning message',
        fields: {'count': 42},
      );
      
      final json = event.toJson();
      expect(json['type'], equals('log'));
      expect(json['level'], equals('warn'));
      expect(json['message'], equals('Warning message'));
      expect(json['fields']['count'], equals(42));
    });
  });

  group('StatePatchEvent Contract (§4.2)', () {
    test('MUST have patch field with object', () {
      const json = '{"version": "0", "type": "state_patch", "patch": {"torch": {"lit": true}}}';
      final event = ProtocolEvent.parseLine(json) as StatePatchEvent;
      
      expect(event.type, equals('state_patch'));
      expect(event.patch['torch'], isNotNull);
      expect((event.patch['torch'] as Map)['lit'], equals(true));
    });

    test('patch defaults to empty map if missing', () {
      const json = '{"version": "0", "type": "state_patch"}';
      final event = ProtocolEvent.parseLine(json) as StatePatchEvent;
      expect(event.patch, isEmpty);
    });

    test('serializes to correct JSON format', () {
      const event = StatePatchEvent(
        version: '0',
        patch: {'inventory': {'items': ['sword', 'shield']}},
      );
      
      final json = event.toJson();
      expect(json['type'], equals('state_patch'));
      expect(json['patch']['inventory']['items'], equals(['sword', 'shield']));
    });
  });

  group('AssetEvent Contract (§4.3)', () {
    test('MUST have assetId, kind, mediaType, path fields', () {
      const json = '''
        {"version": "0", "type": "asset", "assetId": "torch-1", "kind": "image", 
         "mediaType": "image/png", "path": "/tmp/torch.png"}
      ''';
      final event = ProtocolEvent.parseLine(json) as AssetEvent;
      
      expect(event.type, equals('asset'));
      expect(event.assetId, equals('torch-1'));
      expect(event.kind, equals('image'));
      expect(event.mediaType, equals('image/png'));
      expect(event.path, equals('/tmp/torch.png'));
    });

    test('MAY include optional metadata', () {
      const json = '''
        {"version": "0", "type": "asset", "assetId": "img-1", "kind": "image", 
         "mediaType": "image/png", "path": "/tmp/img.png", 
         "metadata": {"width": 512, "height": 512}}
      ''';
      final event = ProtocolEvent.parseLine(json) as AssetEvent;
      
      expect(event.metadata, isNotNull);
      expect(event.metadata!['width'], equals(512));
      expect(event.metadata!['height'], equals(512));
    });

    test('serializes to correct JSON format', () {
      const event = AssetEvent(
        version: '0',
        assetId: 'audio-1',
        kind: 'audio',
        mediaType: 'audio/wav',
        path: '/tmp/sound.wav',
        metadata: {'duration': 5.5},
      );
      
      final json = event.toJson();
      expect(json['type'], equals('asset'));
      expect(json['assetId'], equals('audio-1'));
      expect(json['metadata']['duration'], equals(5.5));
    });
  });

  group('UiEvent Contract (§4.4)', () {
    test('MUST have event field', () {
      const json = '{"version": "0", "type": "ui_event", "event": "narrative_choice"}';
      final event = ProtocolEvent.parseLine(json) as UiEvent;
      
      expect(event.type, equals('ui_event'));
      expect(event.event, equals('narrative_choice'));
    });

    test('MAY include optional payload', () {
      const json = '''
        {"version": "0", "type": "ui_event", "event": "narrative_choice", 
         "payload": {"choices": [{"id": "open", "label": "Open the door"}]}}
      ''';
      final event = ProtocolEvent.parseLine(json) as UiEvent;
      
      expect(event.payload, isNotNull);
      expect((event.payload!['choices'] as List).first['id'], equals('open'));
    });

    test('serializes to correct JSON format', () {
      const event = UiEvent(
        version: '0',
        event: 'narrative_choice',
        payload: {
          'choices': [
            {'id': 'leave', 'label': 'Leave quietly'},
          ],
        },
      );
      
      final json = event.toJson();
      expect(json['type'], equals('ui_event'));
      expect(json['event'], equals('narrative_choice'));
      expect(json['payload']['choices'], isList);
    });
  });

  group('ErrorEvent Contract (§4.5)', () {
    test('MUST have errorCode and errorMessage fields', () {
      const json = '{"version": "0", "type": "error", "errorCode": "FILE_NOT_FOUND", "errorMessage": "Asset file missing"}';
      final event = ProtocolEvent.parseLine(json) as ErrorEvent;
      
      expect(event.type, equals('error'));
      expect(event.errorCode, equals('FILE_NOT_FOUND'));
      expect(event.errorMessage, equals('Asset file missing'));
    });

    test('MAY include optional details', () {
      const json = '''
        {"version": "0", "type": "error", "errorCode": "VALIDATION_ERROR", 
         "errorMessage": "Invalid input", "details": {"field": "name", "reason": "required"}}
      ''';
      final event = ProtocolEvent.parseLine(json) as ErrorEvent;
      
      expect(event.details, isNotNull);
      expect(event.details!['field'], equals('name'));
    });

    test('serializes to correct JSON format', () {
      const event = ErrorEvent(
        version: '0',
        errorCode: 'TIMEOUT',
        errorMessage: 'Operation timed out',
        details: {'timeout_ms': 30000},
      );
      
      final json = event.toJson();
      expect(json['type'], equals('error'));
      expect(json['errorCode'], equals('TIMEOUT'));
      expect(json['details']['timeout_ms'], equals(30000));
    });
  });

  group('DoneEvent Contract (§4.6)', () {
    test('MUST have ok field', () {
      const json = '{"version": "0", "type": "done", "ok": true}';
      final event = ProtocolEvent.parseLine(json) as DoneEvent;
      
      expect(event.type, equals('done'));
      expect(event.ok, isTrue);
    });

    test('MUST be exactly one per invocation', () {
      // This is validated at the protocol level, not in parsing
      // Multiple done events would be a protocol violation
      const json1 = '{"version": "0", "type": "done", "ok": true}';
      const json2 = '{"version": "0", "type": "done", "ok": false}';
      
      // Both parse successfully (validation is at ToolInvoker level)
      expect(ProtocolEvent.parseLine(json1), isA<DoneEvent>());
      expect(ProtocolEvent.parseLine(json2), isA<DoneEvent>());
    });

    test('ok=false indicates logical failure with protocol intact', () {
      const json = '{"version": "0", "type": "done", "ok": false, "summary": "Asset generation failed"}';
      final event = ProtocolEvent.parseLine(json) as DoneEvent;
      
      expect(event.ok, isFalse);
      expect(event.summary, equals('Asset generation failed'));
    });

    test('MAY include optional summary', () {
      const json = '{"version": "0", "type": "done", "ok": true, "summary": "Completed successfully"}';
      final event = ProtocolEvent.parseLine(json) as DoneEvent;
      
      expect(event.summary, equals('Completed successfully'));
    });

    test('serializes to correct JSON format', () {
      const event = DoneEvent(
        version: '0',
        ok: true,
        summary: 'All tasks completed',
      );
      
      final json = event.toJson();
      expect(json['type'], equals('done'));
      expect(json['ok'], isTrue);
      expect(json['summary'], equals('All tasks completed'));
    });
  });

  group('NDJSON Parsing', () {
    test('parseLine handles valid JSON', () {
      const line = '{"version": "0", "type": "log", "level": "info", "message": "test"}';
      expect(ProtocolEvent.parseLine(line), isA<LogEvent>());
    });

    test('parseLine throws ProtocolError for malformed JSON', () {
      const line = '{not valid json}';
      expect(
        () => ProtocolEvent.parseLine(line),
        throwsA(isA<ProtocolError>().having(
          (e) => e.message,
          'message',
          contains('Malformed JSON'),
        )),
      );
    });

    test('parseLine handles JSON with various whitespace', () {
      const line = '  {"version":"0","type":"log","level":"info","message":"test"}  ';
      // Note: jsonDecode handles leading/trailing whitespace in the string itself
      // but NDJSON lines should be trimmed at the stream level
      final trimmed = line.trim();
      expect(ProtocolEvent.parseLine(trimmed), isA<LogEvent>());
    });
  });
}
