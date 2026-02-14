# Options → Character Management Journey

## Overview

This journey covers the player's experience managing persistent character profiles from the Options menu. Players can create new characters, edit existing ones, delete characters, and view their character gallery with campaign history.

## Entry Point
From the Main Menu, player taps `Options` button, then taps `Characters` in the settings panel.

## Exit Point
Player returns to Main Menu or Settings panel, with character profiles saved to local storage.

## Goals
- Browse existing character profiles in gallery view
- Create new character profiles with name, archetype, personality, and backstory
- Edit existing character profiles (modify any field, change portrait)
- Delete unwanted character profiles
- View campaign history for each character
- Import/export character profiles as JSON files (for backup or sharing)

## Sub-journeys
This journey contains several nested flows:
1. **Character Gallery Viewing** - Browse all saved characters with thumbnail cards
2. **Character Creation** - Form-based or freeform input to create new character
3. **Character Editing** - Modify existing character data
4. **Character Deletion** - Remove character with confirmation
5. **Campaign History Viewing** - See which campaigns a character has been used in
6. **Import/Export** - JSON file operations for character backup/sharing

## Related Files
- [steps.md](steps.md) - Detailed step-by-step breakdown
- [states.md](states.md) - UI and application states
- [interactions.md](interactions.md) - Key interactions and decision points
- [metadata.txt](metadata.txt) - Architecture alignment and contracts
