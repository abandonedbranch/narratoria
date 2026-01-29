# Quickstart: Plan Generation and Skill Discovery

Get Narratoria's plan generation and skill system running in under 10 minutes.

## Prerequisites

- Dart 3.x + Flutter SDK (latest stable)
- Ollama installed locally (for LLM backend)
- macOS, Windows, or Linux desktop

## Step 1: Install Ollama and Pull Model

```bash
# Install Ollama (macOS/Linux)
curl -fsSL https://ollama.ai/install.sh | sh

# Or download from https://ollama.ai/ for Windows

# Pull the recommended model for MVP
ollama pull llama3.2:3b

# Verify it works
ollama run llama3.2:3b "You are a narrator. Describe a dark forest."
```

Expected output: Rich prose about a forest (if working, Ctrl+D to exit).

## Step 2: Add Dependencies to pubspec.yaml

```yaml
dependencies:
  flutter:
    sdk: flutter
  flutter_ai_toolkit: ^0.x.x  # Check pub.dev for latest version
  sqlite3: ^2.x.x
  sqflite: ^2.x.x
  json_schema: ^5.x.x
  path_provider: ^2.x.x
  uuid: ^4.x.x

dev_dependencies:
  flutter_test:
    sdk: flutter
  integration_test:
    sdk: flutter
```

Run: `flutter pub get`

## Step 3: Create a Minimal Skill

```bash
mkdir -p skills/dice-roller/scripts
cd skills/dice-roller
```

Create `skill.json`:

```json
{
  "name": "dice-roller",
  "displayName": "Dice Roller",
  "version": "1.0.0",
  "description": "Roll dice for randomness and game mechanics",
  "author": "Narratoria Team",
  "license": "MIT",
  "capabilities": ["randomness"]
}
```

Create `scripts/roll-dice.dart`:

```dart
#!/usr/bin/env dart
import 'dart:io';
import 'dart:convert';
import 'dart:math';

void main(List<String> args) async {
  // Read input from stdin
  final input = jsonDecode(await stdin.first);
  final formula = input['formula'] as String; // e.g., "1d20+5"
  
  // Parse formula (simple regex)
  final match = RegExp(r'(\d+)d(\d+)([+-]\d+)?').firstMatch(formula);
  if (match == null) {
    emitError('Invalid dice formula: $formula');
    emitDone(false);
    return;
  }
  
  final count = int.parse(match.group(1)!);
  final sides = int.parse(match.group(2)!);
  final modifier = int.tryParse(match.group(3) ?? '+0') ?? 0;
  
  // Roll dice
  final random = Random.secure();
  final rolls = List.generate(count, (_) => random.nextInt(sides) + 1);
  final total = rolls.reduce((a, b) => a + b) + modifier;
  
  emitLog('info', 'Rolled $formula: ${rolls.join(', ')} + $modifier = $total');
  
  // Emit UI event for display
  emitUiEvent({
    'type': 'dice_roll',
    'formula': formula,
    'rolls': rolls,
    'modifier': modifier,
    'total': total,
  });
  
  emitDone(true, {'total': total, 'rolls': rolls});
}

void emitLog(String level, String message) {
  print(jsonEncode({'version': '0', 'type': 'log', 'level': level, 'message': message}));
}

void emitUiEvent(Map<String, dynamic> payload) {
  print(jsonEncode({'version': '0', 'type': 'ui_event', 'payload': payload}));
}

void emitError(String message) {
  print(jsonEncode({'version': '0', 'type': 'error', 'message': message}));
}

void emitDone(bool ok, [Map<String, dynamic>? output]) {
  print(jsonEncode({'version': '0', 'type': 'done', 'ok': ok, if (output != null) 'output': output}));
}
```

Make it executable:

```bash
chmod +x scripts/roll-dice.dart
```

## Step 4: Test the Skill Script

```bash
echo '{"formula":"1d20+5"}' | dart scripts/roll-dice.dart
```

Expected output (NDJSON):

```json
{"version":"0","type":"log","level":"info","message":"Rolled 1d20+5: 14 + 5 = 19"}
{"version":"0","type":"ui_event","payload":{"type":"dice_roll","formula":"1d20+5","rolls":[14],"modifier":5,"total":19}}
{"version":"0","type":"done","ok":true,"output":{"total":19,"rolls":[14]}}
```

## Step 5: Run Narratoria with Skill Discovery

```bash
flutter run -d macos  # or -d windows / -d linux
```

The application will:

1. Scan `skills/` directory at startup
2. Discover `dice-roller` skill
3. Display it in Skills Settings UI
4. Make it available to the narrator AI for plan generation

## Step 6: Test Plan Generation

In the Narratoria UI:

1. Type: "I roll to pick the lock"
2. Narrator AI generates Plan JSON:
   ```json
   {
     "requestId": "uuid",
     "narrative": "You attempt to pick the lock...",
     "tools": [
       {
         "id": "roll",
         "toolPath": "skills/dice-roller/scripts/roll-dice.dart",
         "input": {"formula": "1d20+2"}
       }
     ]
   }
   ```
3. Plan executor runs `roll-dice.dart`
4. UI displays dice roll result
5. Narrator incorporates result into narration

## Troubleshooting

### "Ollama not found"

- Ensure Ollama is installed: `ollama --version`
- Check it's running: `ollama list`
- If models not pulled: `ollama pull llama3.2:3b`

### "Skill not discovered"

- Verify `skills/dice-roller/skill.json` exists
- Check JSON is valid: `cat skills/dice-roller/skill.json | jq`
- Restart Narratoria (hot-reload not supported for skill discovery in MVP)

### "Script permission denied"

- Make script executable: `chmod +x skills/dice-roller/scripts/roll-dice.dart`
- Verify shebang: `#!/usr/bin/env dart` (first line)

### "Invalid NDJSON protocol"

- Each event MUST be single-line JSON
- Each event MUST include `version: "0"` and `type` fields
- Exactly ONE `done` event per script execution
- Test manually: `echo '{"formula":"1d20"}' | dart scripts/roll-dice.dart`

## Next Steps

- **Add More Skills**: Create `storyteller`, `memory`, `reputation` skills
- **Configure Skills**: Open Settings → Skills, enter API keys, adjust preferences
- **Explore Plans**: Enable debug mode to see generated Plan JSON
- **Test Replan Loop**: Disable network, force failures, verify graceful degradation

## Reference Documentation

- Full spec: [spec.md](spec.md)
- Data model: [data-model.md](data-model.md)
- Implementation plan: [plan.md](plan.md)
- Tool protocol: [../001-tool-protocol-spec/spec.md](../001-tool-protocol-spec/spec.md)
- Agent Skills Standard: https://agentskills.io/specification
