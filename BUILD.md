# Building Narratoria

This document describes how to build and run the Narratoria macOS app with bundled tools.

## Prerequisites

- Dart SDK (for compiling tools)
- Flutter SDK (for macOS app)
- macOS development environment

## Project Structure

```
narratoria/
├── bin/              # Compiled tool binaries (git-ignored)
├── tools/            # Tool source code (Dart)
│   ├── torch-lighter/
│   │   └── main.dart
│   └── door-examiner/
│       └── main.dart
├── src/              # Flutter application
│   └── macos/        # macOS-specific configuration
└── build-macos.sh    # Build script
```

## Development Mode

### Quick Start

```bash
# From project root
./run-dev.sh
```

This script:
1. Compiles tools to `bin/`
2. Launches the app with `flutter run -d macos`

The app will find tools automatically via the fallback search in [main_screen.dart](src/lib/ui/screens/main_screen.dart#L49).

### Manual Steps

If you prefer manual control:

```bash
# 1. Compile tools
mkdir -p bin
dart compile exe tools/torch-lighter/main.dart -o bin/torch-lighter
dart compile exe tools/door-examiner/main.dart -o bin/door-examiner

# 2. Run app
cd src
flutter run -d macos
```

## Release Build

### Quick Start

```bash
# From project root
./build-macos.sh
```

This script:
1. Compiles tools to `bin/`
2. Builds the release app with `flutter build macos --release`
3. Copies tools into the app bundle at `Contents/Resources/tools/`

### Manual Steps

```bash
# 1. Compile tools
mkdir -p bin
dart compile exe tools/torch-lighter/main.dart -o bin/torch-lighter
dart compile exe tools/door-examiner/main.dart -o bin/door-examiner

# 2. Build Flutter app
cd src
flutter build macos --release

# 3. Copy tools to app bundle
BUNDLE="build/macos/Build/Products/Release/narratoria.app"
mkdir -p "$BUNDLE/Contents/Resources/tools"
cp ../bin/* "$BUNDLE/Contents/Resources/tools/"
chmod +x "$BUNDLE/Contents/Resources/tools/"*
```

The built app will be at:
```
src/build/macos/Build/Products/Release/narratoria.app
```

## How Tools Are Located

The app searches for tools in this order (see [main_screen.dart](src/lib/ui/screens/main_screen.dart)):

1. **App Bundle Resources** (release builds):
   - `Contents/Resources/tools/` within the `.app` bundle
   - Tools are bundled during the build process

2. **Development bin/ Directory**:
   - Walks up from the executable to find `bin/` with compiled tools
   - Works when running via `flutter run`

3. **Hardcoded Path** (fallback):
   - Absolute development path as last resort

## Adding New Tools

To add a new tool to Narratoria:

1. **Create the tool**:
   ```bash
   mkdir -p tools/my-new-tool
   # Create tools/my-new-tool/main.dart with protocol-compliant code
   ```

2. **Compile it**:
   ```bash
   dart compile exe tools/my-new-tool/main.dart -o bin/my-new-tool
   ```

3. **Reference it in NarratorAIStub**:
   ```dart
   // In lib/services/narrator_ai_stub.dart
   ToolInvocation(
     toolId: 'my-tool-1',
     toolPath: '$toolBasePath/my-new-tool',
     input: {...},
   )
   ```

4. **Update build scripts** (if using them):
   - Add compile command to `build-macos.sh` and `run-dev.sh`

## Sandboxing & Entitlements

- **Debug builds** (`DebugProfile.entitlements`): Sandbox disabled for development
- **Release builds** (`Release.entitlements`): Sandbox enabled with:
  - JIT execution permissions
  - Temp file read/write for assets
  - Bundled executable execution

## Testing

Run all tests (including integration tests with compiled tools):

```bash
cd src
flutter test
```

Integration tests expect tools in `bin/` at the workspace root.

## Troubleshooting

### "No such file or directory" when executing tools

**Cause**: Tools not compiled or not in expected location.

**Fix**:
```bash
# Recompile tools
dart compile exe tools/torch-lighter/main.dart -o bin/torch-lighter
dart compile exe tools/door-examiner/main.dart -o bin/door-examiner
```

### "Failed to foreground app"

**Cause**: macOS window management warning (usually harmless).

**Fix**: Ignore this warning. The app should still run. If the window doesn't appear, check Console.app for crash logs.

### Tools work in debug but not release

**Cause**: Tools not copied to app bundle.

**Fix**: Use `build-macos.sh` script or manually copy tools after building (see Release Build steps above).
