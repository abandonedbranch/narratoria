// Narratoria - Interactive Agent-Driven Storytelling
// Spec 001: Tool Protocol Implementation

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'ui/theme.dart';
import 'ui/screens/main_screen.dart';
import 'services/state_manager.dart';
import 'services/asset_registry.dart';
import 'services/plan_executor.dart';

void main() {
  runApp(const NarratoriaApp());
}

/// The root Narratoria application widget.
/// 
/// Provides global state management via Provider and configures
/// Material Design 3 theming per Spec 001 §12.
class NarratoriaApp extends StatelessWidget {
  const NarratoriaApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        // Session state management (T032a: Initialize empty SessionState on startup)
        ChangeNotifierProvider(create: (_) => StateManager()),
        // Asset registry for tool-generated assets
        ChangeNotifierProvider(create: (_) => AssetRegistry()),
        // Plan executor for running tool invocations
        ChangeNotifierProvider(create: (_) => PlanExecutor()),
      ],
      child: MaterialApp(
        title: 'Narratoria',
        debugShowCheckedModeBanner: false,
        theme: NarratoriaTheme.darkTheme,
        home: const MainScreen(),
      ),
    );
  }
}
