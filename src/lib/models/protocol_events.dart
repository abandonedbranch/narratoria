// Protocol Event Models
// Spec 001 §3-4: Event envelope and event types

import 'dart:convert';

/// Base envelope for all protocol events.
/// Per Spec 001 §3: Every JSON object emitted by a tool MUST include version and type.
sealed class ProtocolEvent {
  /// Protocol version. For Spec 001, MUST equal "0".
  final String version;
  
  /// Event type. One of: log, state_patch, asset, ui_event, error, done.
  final String type;
  
  /// Optional request ID provided by Narratoria.
  final String? requestId;
  
  /// Optional ISO-8601 timestamp.
  final String? timestamp;

  const ProtocolEvent({
    required this.version,
    required this.type,
    this.requestId,
    this.timestamp,
  });

  /// Parse a protocol event from JSON.
  /// Throws [ProtocolError] for unknown event types per Spec 001 §5.2.
  factory ProtocolEvent.fromJson(Map<String, dynamic> json) {
    final version = json['version'] as String? ?? '0';
    final type = json['type'] as String?;
    
    if (type == null) {
      throw ProtocolError('Missing required field: type');
    }
    
    // Validate protocol version (only "0" is valid for Spec 001)
    if (version != '0') {
      throw ProtocolError('Unsupported protocol version: $version');
    }

    return switch (type) {
      'log' => LogEvent.fromJson(json),
      'state_patch' => StatePatchEvent.fromJson(json),
      'asset' => AssetEvent.fromJson(json),
      'ui_event' => UiEvent.fromJson(json),
      'error' => ErrorEvent.fromJson(json),
      'done' => DoneEvent.fromJson(json),
      _ => throw ProtocolError('Unknown event type: $type'),
    };
  }

  /// Parse an NDJSON line into a ProtocolEvent.
  static ProtocolEvent parseLine(String line) {
    try {
      final json = jsonDecode(line) as Map<String, dynamic>;
      return ProtocolEvent.fromJson(json);
    } on FormatException catch (e) {
      throw ProtocolError('Malformed JSON: ${e.message}');
    }
  }

  Map<String, dynamic> toJson();
}

/// Protocol error for invalid events.
/// Per Spec 001 §5.2: Unknown event types are protocol errors.
class ProtocolError implements Exception {
  final String message;
  const ProtocolError(this.message);
  
  @override
  String toString() => 'ProtocolError: $message';
}

/// Log event for progress/diagnostic information.
/// Spec 001 §4.1
class LogEvent extends ProtocolEvent {
  /// Log level: debug, info, warn, error
  final String level;
  
  /// Log message
  final String message;
  
  /// Optional structured fields
  final Map<String, dynamic>? fields;

  const LogEvent({
    required super.version,
    super.requestId,
    super.timestamp,
    required this.level,
    required this.message,
    this.fields,
  }) : super(type: 'log');

  factory LogEvent.fromJson(Map<String, dynamic> json) {
    return LogEvent(
      version: json['version'] as String? ?? '0',
      requestId: json['requestId'] as String?,
      timestamp: json['timestamp'] as String?,
      level: json['level'] as String? ?? 'info',
      message: json['message'] as String? ?? '',
      fields: json['fields'] as Map<String, dynamic>?,
    );
  }

  @override
  Map<String, dynamic> toJson() => {
    'version': version,
    'type': type,
    if (requestId != null) 'requestId': requestId,
    if (timestamp != null) 'timestamp': timestamp,
    'level': level,
    'message': message,
    if (fields != null) 'fields': fields,
  };
}

/// State patch event for updating session state.
/// Spec 001 §4.2
class StatePatchEvent extends ProtocolEvent {
  /// Patch object to merge into session state using deep merge semantics.
  final Map<String, dynamic> patch;

  const StatePatchEvent({
    required super.version,
    super.requestId,
    super.timestamp,
    required this.patch,
  }) : super(type: 'state_patch');

  factory StatePatchEvent.fromJson(Map<String, dynamic> json) {
    return StatePatchEvent(
      version: json['version'] as String? ?? '0',
      requestId: json['requestId'] as String?,
      timestamp: json['timestamp'] as String?,
      patch: json['patch'] as Map<String, dynamic>? ?? {},
    );
  }

  @override
  Map<String, dynamic> toJson() => {
    'version': version,
    'type': type,
    if (requestId != null) 'requestId': requestId,
    if (timestamp != null) 'timestamp': timestamp,
    'patch': patch,
  };
}

/// Asset event for notifying about created assets.
/// Spec 001 §4.3
class AssetEvent extends ProtocolEvent {
  /// Unique asset ID within tool invocation
  final String assetId;
  
  /// Broad category (image, audio, video, model)
  final String kind;
  
  /// MIME type
  final String mediaType;
  
  /// Filesystem path to asset file
  final String path;
  
  /// Optional metadata (dimensions, duration, etc.)
  final Map<String, dynamic>? metadata;

  const AssetEvent({
    required super.version,
    super.requestId,
    super.timestamp,
    required this.assetId,
    required this.kind,
    required this.mediaType,
    required this.path,
    this.metadata,
  }) : super(type: 'asset');

  factory AssetEvent.fromJson(Map<String, dynamic> json) {
    return AssetEvent(
      version: json['version'] as String? ?? '0',
      requestId: json['requestId'] as String?,
      timestamp: json['timestamp'] as String?,
      assetId: json['assetId'] as String? ?? '',
      kind: json['kind'] as String? ?? 'unknown',
      mediaType: json['mediaType'] as String? ?? 'application/octet-stream',
      path: json['path'] as String? ?? '',
      metadata: json['metadata'] as Map<String, dynamic>?,
    );
  }

  @override
  Map<String, dynamic> toJson() => {
    'version': version,
    'type': type,
    if (requestId != null) 'requestId': requestId,
    if (timestamp != null) 'timestamp': timestamp,
    'assetId': assetId,
    'kind': kind,
    'mediaType': mediaType,
    'path': path,
    if (metadata != null) 'metadata': metadata,
  };
}

/// UI event for requesting UI actions.
/// Spec 001 §4.4
class UiEvent extends ProtocolEvent {
  /// Event name identifying the UI action
  final String event;
  
  /// Event-specific payload
  final Map<String, dynamic>? payload;

  const UiEvent({
    required super.version,
    super.requestId,
    super.timestamp,
    required this.event,
    this.payload,
  }) : super(type: 'ui_event');

  factory UiEvent.fromJson(Map<String, dynamic> json) {
    return UiEvent(
      version: json['version'] as String? ?? '0',
      requestId: json['requestId'] as String?,
      timestamp: json['timestamp'] as String?,
      event: json['event'] as String? ?? '',
      payload: json['payload'] as Map<String, dynamic>?,
    );
  }

  @override
  Map<String, dynamic> toJson() => {
    'version': version,
    'type': type,
    if (requestId != null) 'requestId': requestId,
    if (timestamp != null) 'timestamp': timestamp,
    'event': event,
    if (payload != null) 'payload': payload,
  };
}

/// Error event for structured errors.
/// Spec 001 §4.5
class ErrorEvent extends ProtocolEvent {
  /// Error code
  final String errorCode;
  
  /// Human-readable error message
  final String errorMessage;
  
  /// Optional structured error details
  final Map<String, dynamic>? details;

  const ErrorEvent({
    required super.version,
    super.requestId,
    super.timestamp,
    required this.errorCode,
    required this.errorMessage,
    this.details,
  }) : super(type: 'error');

  factory ErrorEvent.fromJson(Map<String, dynamic> json) {
    return ErrorEvent(
      version: json['version'] as String? ?? '0',
      requestId: json['requestId'] as String?,
      timestamp: json['timestamp'] as String?,
      errorCode: json['errorCode'] as String? ?? 'UNKNOWN',
      errorMessage: json['errorMessage'] as String? ?? 'Unknown error',
      details: json['details'] as Map<String, dynamic>?,
    );
  }

  @override
  Map<String, dynamic> toJson() => {
    'version': version,
    'type': type,
    if (requestId != null) 'requestId': requestId,
    if (timestamp != null) 'timestamp': timestamp,
    'errorCode': errorCode,
    'errorMessage': errorMessage,
    if (details != null) 'details': details,
  };
}

/// Done event signaling tool completion.
/// Spec 001 §4.6
class DoneEvent extends ProtocolEvent {
  /// Whether the tool completed successfully
  final bool ok;
  
  /// Optional summary message
  final String? summary;

  const DoneEvent({
    required super.version,
    super.requestId,
    super.timestamp,
    required this.ok,
    this.summary,
  }) : super(type: 'done');

  factory DoneEvent.fromJson(Map<String, dynamic> json) {
    return DoneEvent(
      version: json['version'] as String? ?? '0',
      requestId: json['requestId'] as String?,
      timestamp: json['timestamp'] as String?,
      ok: json['ok'] as bool? ?? false,
      summary: json['summary'] as String?,
    );
  }

  @override
  Map<String, dynamic> toJson() => {
    'version': version,
    'type': type,
    if (requestId != null) 'requestId': requestId,
    if (timestamp != null) 'timestamp': timestamp,
    'ok': ok,
    if (summary != null) 'summary': summary,
  };
}
