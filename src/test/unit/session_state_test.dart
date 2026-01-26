// Unit Tests for SessionState Deep Merge
// T037: Validates deep merge semantics per Spec 001 §4.2

import 'package:flutter_test/flutter_test.dart';
import 'package:narratoria/models/session_state.dart';

void main() {
  group('SessionState Deep Merge Semantics (§4.2)', () {
    group('Nested objects merged recursively', () {
      test('adds new keys to nested objects', () {
        final base = SessionState({
          'inventory': {'torch': {'lit': false}},
        });
        
        final patched = base.applyPatch({
          'inventory': {'torch': {'fuel': 100}},
        });
        
        // Both keys should exist
        expect(patched.getPath('inventory.torch.lit'), equals(false));
        expect(patched.getPath('inventory.torch.fuel'), equals(100));
      });

      test('updates existing keys in nested objects', () {
        final base = SessionState({
          'inventory': {'torch': {'lit': false, 'fuel': 50}},
        });
        
        final patched = base.applyPatch({
          'inventory': {'torch': {'lit': true}},
        });
        
        // lit updated, fuel unchanged
        expect(patched.getPath('inventory.torch.lit'), equals(true));
        expect(patched.getPath('inventory.torch.fuel'), equals(50));
      });

      test('merges deeply nested objects', () {
        final base = SessionState({
          'world': {
            'dungeon': {
              'room1': {'explored': false},
            },
          },
        });
        
        final patched = base.applyPatch({
          'world': {
            'dungeon': {
              'room1': {'items': ['key']},
              'room2': {'explored': false},
            },
          },
        });
        
        // room1 merged, room2 added
        expect(patched.getPath('world.dungeon.room1.explored'), equals(false));
        expect(patched.getPath('world.dungeon.room1.items'), equals(['key']));
        expect(patched.getPath('world.dungeon.room2.explored'), equals(false));
      });

      test('adds new top-level keys', () {
        final base = SessionState({
          'player': {'name': 'Hero'},
        });
        
        final patched = base.applyPatch({
          'quest': {'active': true},
        });
        
        expect(patched.getPath('player.name'), equals('Hero'));
        expect(patched.getPath('quest.active'), equals(true));
      });
    });

    group('Arrays replaced entirely', () {
      test('replaces array with new array', () {
        final base = SessionState({
          'inventory': {'items': ['sword', 'shield']},
        });
        
        final patched = base.applyPatch({
          'inventory': {'items': ['sword', 'shield', 'potion']},
        });
        
        // Array is replaced, not appended
        expect(
          patched.getPath('inventory.items'),
          equals(['sword', 'shield', 'potion']),
        );
      });

      test('replaces array with empty array', () {
        final base = SessionState({
          'inventory': {'items': ['sword', 'shield']},
        });
        
        final patched = base.applyPatch({
          'inventory': {'items': []},
        });
        
        expect(patched.getPath('inventory.items'), isEmpty);
      });

      test('replaces nested arrays', () {
        final base = SessionState({
          'spell': {'effects': [1, 2, 3]},
        });
        
        final patched = base.applyPatch({
          'spell': {'effects': [4, 5]},
        });
        
        expect(patched.getPath('spell.effects'), equals([4, 5]));
      });
    });

    group('Null values remove keys', () {
      test('null removes top-level key', () {
        final base = SessionState({
          'player': {'name': 'Hero'},
          'temporary': {'buff': 'strength'},
        });
        
        final patched = base.applyPatch({
          'temporary': null,
        });
        
        expect(patched.getPath('player.name'), equals('Hero'));
        expect(patched.getPath('temporary'), isNull);
        expect(patched.state.containsKey('temporary'), isFalse);
      });

      test('null removes nested key', () {
        final base = SessionState({
          'inventory': {'torch': {'lit': true}, 'sword': {'equipped': true}},
        });
        
        final patched = base.applyPatch({
          'inventory': {'torch': null},
        });
        
        expect(patched.getPath('inventory.sword.equipped'), equals(true));
        expect(patched.getPath('inventory.torch'), isNull);
        expect(
          (patched.getPath('inventory') as Map).containsKey('torch'),
          isFalse,
        );
      });

      test('null in nested path removes only that key', () {
        final base = SessionState({
          'player': {
            'stats': {'health': 100, 'mana': 50},
            'name': 'Hero',
          },
        });
        
        final patched = base.applyPatch({
          'player': {'stats': {'mana': null}},
        });
        
        expect(patched.getPath('player.name'), equals('Hero'));
        expect(patched.getPath('player.stats.health'), equals(100));
        expect(patched.getPath('player.stats.mana'), isNull);
      });
    });

    group('getPath navigation', () {
      test('returns value at valid path', () {
        final state = SessionState({
          'a': {'b': {'c': 42}},
        });
        
        expect(state.getPath('a.b.c'), equals(42));
      });

      test('returns null for non-existent path', () {
        final state = SessionState({
          'a': {'b': 1},
        });
        
        expect(state.getPath('a.c'), isNull);
        expect(state.getPath('x.y.z'), isNull);
      });

      test('returns null when path traverses non-map', () {
        final state = SessionState({
          'a': {'b': 'string_value'},
        });
        
        expect(state.getPath('a.b.c'), isNull);
      });
    });

    group('Immutability', () {
      test('applyPatch returns new state without modifying original', () {
        final original = SessionState({
          'counter': 1,
        });
        
        final patched = original.applyPatch({'counter': 2});
        
        expect(original.getPath('counter'), equals(1));
        expect(patched.getPath('counter'), equals(2));
      });

      test('copy creates independent copy', () {
        final original = SessionState({
          'nested': {'value': 1},
        });
        
        final copy = original.copy();
        final patched = copy.applyPatch({
          'nested': {'value': 2},
        });
        
        expect(original.getPath('nested.value'), equals(1));
        expect(patched.getPath('nested.value'), equals(2));
      });
    });

    group('Edge cases', () {
      test('empty patch returns equivalent state', () {
        final base = SessionState({'a': 1});
        final patched = base.applyPatch({});
        
        expect(patched.state, equals({'a': 1}));
      });

      test('empty base with patch creates new state', () {
        final base = SessionState();
        final patched = base.applyPatch({'new_key': 'value'});
        
        expect(patched.getPath('new_key'), equals('value'));
      });

      test('handles various primitive types', () {
        final base = SessionState();
        final patched = base.applyPatch({
          'string': 'hello',
          'int': 42,
          'double': 3.14,
          'bool': true,
          'array': [1, 'two', 3.0],
        });
        
        expect(patched.getPath('string'), equals('hello'));
        expect(patched.getPath('int'), equals(42));
        expect(patched.getPath('double'), equals(3.14));
        expect(patched.getPath('bool'), equals(true));
        expect(patched.getPath('array'), equals([1, 'two', 3.0]));
      });

      test('deepMerge static method works independently', () {
        final result = SessionState.deepMerge(
          {'a': {'b': 1}},
          {'a': {'c': 2}},
        );
        
        expect(result, equals({'a': {'b': 1, 'c': 2}}));
      });
    });
  });

  group('SessionState utility methods', () {
    test('isEmpty returns true for empty state', () {
      expect(SessionState().isEmpty, isTrue);
      expect(SessionState({}).isEmpty, isTrue);
    });

    test('isNotEmpty returns true for non-empty state', () {
      expect(SessionState({'a': 1}).isNotEmpty, isTrue);
    });

    test('state returns unmodifiable map', () {
      final sessionState = SessionState({'a': 1});
      expect(
        () => sessionState.state['a'] = 2,
        throwsUnsupportedError,
      );
    });

    test('toString provides readable output', () {
      final state = SessionState({'key': 'value'});
      expect(state.toString(), contains('key'));
      expect(state.toString(), contains('value'));
    });
  });
}
