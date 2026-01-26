// Session State Model with Deep Merge
// Spec 001 §4.2: Deep merge semantics for state_patch events

/// Session state model with deep merge support.
/// 
/// Per Spec 001 §4.2, deep merge semantics are:
/// - Nested objects are merged recursively (keys added/updated, not replaced entirely)
/// - Arrays are replaced entirely (not merged element-by-element)
/// - Null values remove keys from the state tree
class SessionState {
  /// The current state tree
  final Map<String, dynamic> _state;

  SessionState([Map<String, dynamic>? initialState]) 
      : _state = Map<String, dynamic>.from(initialState ?? {});

  /// Get the current state as an unmodifiable map.
  Map<String, dynamic> get state => Map.unmodifiable(_state);

  /// Get a value at a path (dot notation: "inventory.torch.lit")
  dynamic getPath(String path) {
    final parts = path.split('.');
    dynamic current = _state;
    
    for (final part in parts) {
      if (current is Map<String, dynamic>) {
        current = current[part];
      } else {
        return null;
      }
    }
    
    return current;
  }

  /// Deep merge a patch into this state.
  /// Returns a new SessionState with the merged result.
  /// 
  /// Spec 001 §4.2 semantics:
  /// - Nested objects merged recursively
  /// - Arrays replaced entirely  
  /// - Null values remove keys
  SessionState applyPatch(Map<String, dynamic> patch) {
    final merged = deepMerge(_state, patch);
    return SessionState(merged);
  }

  /// Deep merge utility function (pure function).
  /// 
  /// Per Spec 001 §4.2:
  /// - Nested objects are merged recursively
  /// - Arrays are replaced entirely
  /// - Null values remove keys from the state tree
  /// 
  /// Example: 
  /// base: {"a": {"b": 1, "c": 2}}
  /// patch: {"a": {"c": 3, "d": 4}}
  /// result: {"a": {"b": 1, "c": 3, "d": 4}}
  static Map<String, dynamic> deepMerge(
    Map<String, dynamic> base,
    Map<String, dynamic> patch,
  ) {
    final result = Map<String, dynamic>.from(base);
    
    for (final entry in patch.entries) {
      final key = entry.key;
      final patchValue = entry.value;
      
      // Null removes the key
      if (patchValue == null) {
        result.remove(key);
        continue;
      }
      
      final baseValue = result[key];
      
      // If both are maps, merge recursively
      if (baseValue is Map<String, dynamic> && 
          patchValue is Map<String, dynamic>) {
        result[key] = deepMerge(baseValue, patchValue);
      } else {
        // Otherwise replace (including arrays)
        result[key] = patchValue;
      }
    }
    
    return result;
  }

  /// Create a copy of this state
  SessionState copy() {
    return SessionState(_deepCopy(_state));
  }

  /// Deep copy a map
  static Map<String, dynamic> _deepCopy(Map<String, dynamic> source) {
    final result = <String, dynamic>{};
    for (final entry in source.entries) {
      if (entry.value is Map<String, dynamic>) {
        result[entry.key] = _deepCopy(entry.value as Map<String, dynamic>);
      } else if (entry.value is List) {
        result[entry.key] = List.from(entry.value as List);
      } else {
        result[entry.key] = entry.value;
      }
    }
    return result;
  }

  /// Check if state is empty
  bool get isEmpty => _state.isEmpty;

  /// Check if state is not empty
  bool get isNotEmpty => _state.isNotEmpty;

  @override
  String toString() => 'SessionState($_state)';
}
