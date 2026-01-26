// Narratoria Material Design 3 Theme
// Spec 001 §12.4: Dark-themed scheme suitable for immersive storytelling

import 'package:flutter/material.dart';

/// Narratoria app theme configuration.
/// Uses Material Design 3 with deep purple seed color and dark brightness.
class NarratoriaTheme {
  NarratoriaTheme._();

  /// The primary theme for Narratoria.
  /// Per spec §12.4: Dark theme with deep purple accent for immersive storytelling.
  static ThemeData get darkTheme {
    return ThemeData(
      useMaterial3: true,
      colorScheme: ColorScheme.fromSeed(
        seedColor: Colors.deepPurple,
        brightness: Brightness.dark,
      ),
      typography: Typography.material2021(),
      // Enhanced card theme for panels
      cardTheme: CardThemeData(
        elevation: 2,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
        ),
      ),
      // Input decoration for player input field
      inputDecorationTheme: InputDecorationTheme(
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
        ),
        filled: true,
      ),
      // Navigation rail styling
      navigationRailTheme: const NavigationRailThemeData(
        labelType: NavigationRailLabelType.all,
        useIndicator: true,
      ),
    );
  }

  /// Semantic colors for tool execution states
  static const Color successColor = Colors.green;
  static const Color errorColor = Colors.red;
  static const Color warningColor = Colors.orange;
  static const Color infoColor = Colors.blue;

  /// Log level colors for Tool Execution Panel
  static Color logLevelColor(String level) {
    switch (level.toLowerCase()) {
      case 'error':
        return errorColor;
      case 'warn':
        return warningColor;
      case 'info':
        return infoColor;
      case 'debug':
      default:
        return Colors.grey;
    }
  }
}
