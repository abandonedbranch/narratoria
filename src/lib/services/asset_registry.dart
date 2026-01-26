// Asset Registry Service
// Spec 001 §4.3 & §7: Asset registration and validation

import 'package:flutter/foundation.dart';
import '../models/asset.dart';
import '../models/protocol_events.dart';

/// Registry for assets created by tools during plan execution.
/// 
/// Per Spec 001 §7: Narratoria validates that asset files exist.
/// Per constitution principle IV: Graceful degradation for unsupported assets.
class AssetRegistry extends ChangeNotifier {
  final List<Asset> _assets = [];

  /// All registered assets (unmodifiable)
  List<Asset> get assets => List.unmodifiable(_assets);

  /// Register an asset from an AssetEvent.
  /// Returns the created Asset.
  Asset registerFromEvent(AssetEvent event, {String? sourceToolId}) {
    final asset = Asset.fromEvent(
      assetId: event.assetId,
      kind: event.kind,
      mediaType: event.mediaType,
      path: event.path,
      metadata: event.metadata,
      sourceToolId: sourceToolId,
      requestId: event.requestId,
    );
    
    _assets.add(asset);
    notifyListeners();
    return asset;
  }

  /// Get assets by kind (image, audio, video, etc.)
  List<Asset> getByKind(String kind) {
    return _assets.where((a) => a.kind == kind).toList();
  }

  /// Get assets by request ID
  List<Asset> getByRequestId(String requestId) {
    return _assets.where((a) => a.requestId == requestId).toList();
  }

  /// Get assets by tool ID
  List<Asset> getByToolId(String toolId) {
    return _assets.where((a) => a.sourceToolId == toolId).toList();
  }

  /// Get an asset by its ID
  Asset? getById(String assetId) {
    return _assets.where((a) => a.assetId == assetId).firstOrNull;
  }

  /// Validate all assets exist on disk.
  /// Returns list of invalid (missing) assets.
  List<Asset> validateAll() {
    return _assets.where((a) => !a.isValid).toList();
  }

  /// Clear all registered assets.
  void clear() {
    _assets.clear();
    notifyListeners();
  }

  /// Clear assets for a specific request.
  void clearForRequest(String requestId) {
    _assets.removeWhere((a) => a.requestId == requestId);
    notifyListeners();
  }
}
