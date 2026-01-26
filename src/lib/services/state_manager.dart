// State Manager Service
// Spec 001 §6: State orchestration and lifecycle management

import 'package:flutter/foundation.dart';
import '../models/session_state.dart';
import '../models/protocol_events.dart';

/// State manager that orchestrates session state changes.
/// 
/// Per constitution principle V: All modules support unit testing in isolation.
/// StateManager depends on SessionState for deep merge logic.
class StateManager extends ChangeNotifier {
  SessionState _sessionState = SessionState();
  
  /// Current session state (immutable snapshot)
  SessionState get sessionState => _sessionState;

  /// Apply a state patch from a StatePatchEvent.
  /// Per Spec 001 §4.2: Uses deep merge semantics.
  void applyPatch(StatePatchEvent event) {
    _sessionState = _sessionState.applyPatch(event.patch);
    notifyListeners();
  }

  /// Apply a raw patch map.
  void applyRawPatch(Map<String, dynamic> patch) {
    _sessionState = _sessionState.applyPatch(patch);
    notifyListeners();
  }

  /// Get a value at a path in the state tree.
  dynamic getPath(String path) {
    return _sessionState.getPath(path);
  }

  /// Reset state to empty.
  void reset() {
    _sessionState = SessionState();
    notifyListeners();
  }

  /// Reset state to a specific initial state.
  void resetTo(Map<String, dynamic> initialState) {
    _sessionState = SessionState(initialState);
    notifyListeners();
  }
}
