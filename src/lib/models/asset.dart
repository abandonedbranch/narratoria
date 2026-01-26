// Asset Model
// Spec 001 §4.3: Asset metadata from tool-generated assets

import 'dart:io';

/// Represents an asset created by a tool.
/// Spec 001 §4.3: Assets are registered and rendered based on kind/mediaType.
class Asset {
  /// Unique asset ID within the tool invocation
  final String assetId;
  
  /// Broad category: image, audio, video, model, etc.
  final String kind;
  
  /// MIME type string
  final String mediaType;
  
  /// Filesystem path to the asset file
  final String path;
  
  /// Optional metadata (dimensions, duration, etc.)
  final Map<String, dynamic>? metadata;
  
  /// Tool ID that created this asset
  final String? sourceToolId;
  
  /// Request ID from the plan execution
  final String? requestId;
  
  /// Timestamp when the asset was registered
  final DateTime registeredAt;

  Asset({
    required this.assetId,
    required this.kind,
    required this.mediaType,
    required this.path,
    this.metadata,
    this.sourceToolId,
    this.requestId,
    DateTime? registeredAt,
  }) : registeredAt = registeredAt ?? DateTime.now();

  /// Check if the asset file exists and is readable.
  /// Per Spec 001 §7: Narratoria MUST validate that asset files exist.
  bool get isValid {
    final file = File(path);
    return file.existsSync();
  }

  /// Check if this asset is a supported image type.
  bool get isImage {
    return kind == 'image' || 
           mediaType.startsWith('image/');
  }

  /// Check if this asset is a supported audio type.
  bool get isAudio {
    return kind == 'audio' || 
           mediaType.startsWith('audio/');
  }

  /// Check if this asset is a supported video type.
  bool get isVideo {
    return kind == 'video' || 
           mediaType.startsWith('video/');
  }

  /// Check if this asset type is supported for rendering.
  /// Per Spec 001 §4.3: Display placeholder for unsupported types.
  bool get isSupported {
    return isImage || isAudio || isVideo;
  }

  /// Create from an AssetEvent
  factory Asset.fromEvent({
    required String assetId,
    required String kind,
    required String mediaType,
    required String path,
    Map<String, dynamic>? metadata,
    String? sourceToolId,
    String? requestId,
  }) {
    return Asset(
      assetId: assetId,
      kind: kind,
      mediaType: mediaType,
      path: path,
      metadata: metadata,
      sourceToolId: sourceToolId,
      requestId: requestId,
    );
  }

  Map<String, dynamic> toJson() => {
    'assetId': assetId,
    'kind': kind,
    'mediaType': mediaType,
    'path': path,
    if (metadata != null) 'metadata': metadata,
    if (sourceToolId != null) 'sourceToolId': sourceToolId,
    if (requestId != null) 'requestId': requestId,
    'registeredAt': registeredAt.toIso8601String(),
  };
}
