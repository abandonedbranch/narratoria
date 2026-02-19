import 'package:tui/models.dart';
import 'package:tui/engine.dart';
import 'package:test/test.dart';

void main() {
  group('models', () {
    test('Attribute creation', () {
      final attr = Attribute(name: 'STR', keywords: ['strength'], value: 6);
      expect(attr.name, 'STR');
      expect(attr.value, 6);
    });
  });

  group('engine', () {
    test('skill check deterministic', () {
      final game = Game(
        title: 'Test',
        summary: 'Test game',
        playerPersona: PlayerPersona(
          attributes: [
            Attribute(name: 'STR', keywords: ['strength'], value: 6),
            Attribute(name: 'WIS', keywords: ['wisdom'], value: 3),
          ],
        ),
        acts: [
          Act(
            title: 'Act 1',
            summary: 'First act',
            scenes: [
              Scene(
                title: 'Scene 1',
                summary: 'First scene',
                narrative: 'You are here.',
                options: [
                  Option(
                    text: 'Easy check',
                    skillCheck: SkillCheck(stat: 'STR', difficulty: 5),
                    onSuccess: 'You succeed.',
                    onFail: 'You fail.',
                  ),
                  Option(
                    text: 'Hard check',
                    skillCheck: SkillCheck(stat: 'WIS', difficulty: 5),
                    onSuccess: 'Wise success.',
                    onFail: 'Wise fail.',
                  ),
                ],
              ),
            ],
          ),
        ],
      );

      final state = GameState(game);
      final engine = GameEngine(state);

      // STR 6 >= difficulty 5 -> success
      final r1 = engine.resolveOptionFull(1)!;
      expect(r1.skillCheckResult!.success, true);

      // WIS 3 < difficulty 5 -> failure
      final r2 = engine.resolveOptionFull(2)!;
      expect(r2.skillCheckResult!.success, false);
    });

    test('option hints', () {
      final game = Game(
        title: 'Test',
        summary: 'Test',
        playerPersona: PlayerPersona(
          attributes: [
            Attribute(name: 'DEX', keywords: ['dexterity'], value: 7),
          ],
        ),
        acts: [
          Act(title: 'A', summary: 'A', scenes: [
            Scene(title: 'S', summary: 'S', narrative: 'N'),
          ]),
        ],
      );
      final engine = GameEngine(GameState(game));

      final likely = Option(
        text: 'easy',
        skillCheck: SkillCheck(stat: 'DEX', difficulty: 5),
      );
      expect(engine.getOptionHint(likely), contains('likely'));

      final risky = Option(
        text: 'hard',
        skillCheck: SkillCheck(stat: 'DEX', difficulty: 9),
      );
      expect(engine.getOptionHint(risky), contains('risky'));
    });
  });
}
