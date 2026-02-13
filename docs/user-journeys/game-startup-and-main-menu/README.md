# Game Startup and Main Menu Journey

## Overview

This journey covers the player experience from launching Narratoria through character selection and campaign selection, ending when the first scene of a campaign begins loading.

## Entry Point
User taps or double-clicks the Narratoria application icon.

## Exit Point
The first scene of the selected campaign loads and displays narrative content.

## Goals
- Application launches successfully
- Main menu displays with clear action options
- Player can select a character (from predefined character templates)
- Player can browse and select a campaign
- Player is informed of content warnings before proceeding
- Campaign initializes and first scene renders

## Sub-journeys
This journey contains several nested decision branches:
1. **Main Menu Navigation** - Choose between New Game, Load, Options, or Exit
2. **Campaign Selection** - Browse available campaigns with summaries
3. **Character Selection** - Pick from character templates allowed by the selected campaign
4. **Content Warning Acknowledgment** - Confirm understanding of warnings
5. **Campaign Startup** - Initial scene loads and renders

## Related Files
- [steps.md](steps.md) - Detailed step-by-step breakdown
- [states.md](states.md) - UI and application states
- [interactions.md](interactions.md) - Key interactions and decision points

