// Narratoria Widget Tests
// Basic smoke tests for app structure

import 'package:flutter_test/flutter_test.dart';
import 'package:narratoria/main.dart';

void main() {
  testWidgets('App renders with NavigationRail', (WidgetTester tester) async {
    // Build our app and trigger a frame.
    await tester.pumpWidget(const NarratoriaApp());

    // Verify that NavigationRail destinations are present
    expect(find.text('Story'), findsOneWidget);
    expect(find.text('Tools'), findsOneWidget);
    expect(find.text('State'), findsOneWidget);
  });
}
