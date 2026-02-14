# Options → Character Management - Interactions

## Primary Decision Points

### Decision 1: Create New Character
**User Choice**: Should I create a new character or use/edit an existing one?

**Options**:
1. `Create New Character` → Opens character creation form (CHARACTER_CREATE state)
2. `Browse Existing Characters` → Tap any character card to view details
3. `Import Character` → Tap menu (⋮) → Import to load JSON file
4. `Return to Main Menu` → Tap Back button

**System Response**:
- Option 1: Character creation form slides in with empty fields, Save disabled until name entered
- Option 2: Character detail view opens with full profile and campaign history
- Option 3: Platform file picker opens filtered to `.json` files
- Option 4: Returns to OPTIONS_PANEL or MAIN_MENU

**Accessibility Notes**:
- New Character button: Min 44×44pt touch target (iOS), 48×48dp (Android)
- Keyboard navigation: Tab order should be New Character → Character Cards → Menu → Back
- Screen reader: "Create new character" button, character cards announced as "Character: [Name], [Class], [N] campaigns"

---

### Decision 2: Describe Character and Generate Profile
**User Choice**: How should I create my character?

**Options**:
1. `Freeform Description` → Enter natural language description in text area, upload portrait, tap Generate
2. `Minimal Description` → Brief 1-2 sentence description (e.g., "A brave knight"), upload portrait, tap Generate
3. `Detailed Description` → 50-500 word description with personality, background, goals, upload portrait, tap Generate
4. `Cancel` → Tap Cancel button without generating

**System Response**:
- Options 1-3: LLM (Phi-4/Phi-4-mini) generates structured character profile (2-5 seconds) → Preview displays generated name, archetype, personality, background, goals, speech patterns → User can Save, Regenerate, or Edit
- Option 4: If no text entered, returns to gallery immediately; if text entered, shows "Discard changes?" confirmation

**Accessibility Notes**:
- Text area announced by screen reader: "Describe your character, text area, multiline"
- Character count indicator announced: "250 characters entered. Recommendation: 50 to 500 words for best results"
- Generate button: Min 44×44pt/48×48dp, visually distinct enabled vs disabled state
- During generation: Screen reader announces "Creating character, please wait. Estimated time: 2 to 5 seconds"

**Design Note**:
- Portrait is **required** before generation (system cannot generate images with in-process models)
- Text area has example prompt (collapsible): "Try: 'A grizzled veteran warrior haunted by past battles...'"
- Minimum ~25 words recommended for meaningful generation
- User can regenerate with same prompt to get different interpretation (names, traits vary)

---

### Decision 2.1: Review Generated Character
**User Choice**: What should I do with the LLM-generated character profile?

**Options**:
1. `Save As-Is` → Accept generated profile, tap Save → Character saved to local storage
2. `Regenerate` → Tap Regenerate with same prompt → LLM creates new interpretation (2-5 seconds)
3. `Edit Before Saving` → Tap Edit → Opens edit form with generated data pre-filled → Make changes → Save
4. `Discard` → Tap Cancel → Returns to gallery, character not saved

**System Response**:
- Option 1: Character profile JSON created with UUID, saved to `<user_data>/characters/{id}.json`, returns to gallery
- Option 2: LLM re-runs generation with same prompt, new preview displayed (different name/traits/details possible)
- Option 3: Edit form opens (CHARACTER_EDIT state) with all generated fields editable, user can refine before saving
- Option 4: Returns to gallery, generated data discarded

**Accessibility Notes**:
- Preview fields: All generated data announced by screen reader ("Name: [generated name]", "Race: [race]", etc.)
- Action buttons clearly labeled: "Save character", "Regenerate character", "Edit character", "Cancel"
- Regenerate status: "Regenerating character, please wait" announced during generation

**Design Note**:
- Inline editing: User can click any field in preview to edit directly (without opening full edit form)
- Original freeform description stored in `creation_prompt` field for future regeneration
- Regeneration preserves portrait (only character data regenerated)

---

### Decision 3: Edit or Delete Character
**User Choice**: What action should I take on this character?

**Options**:
1. `Edit` → Opens character edit form with pre-populated data
2. `Delete` → Shows confirmation dialog → Permanently removes character
3. `Export` → Opens platform save dialog → Saves character JSON to chosen location
4. `View Campaign History` → Scroll to campaign history section in detail view
5. `Return to Gallery` → Tap Back button

**System Response**:
- Option 1: Edit form opens (CHARACTER_EDIT state), all fields editable
- Option 2: Confirmation dialog: "Delete [Name]? This action cannot be undone." → On confirm, character deleted and gallery updated
- Option 3: File save dialog with suggested name `{character_name}_{date}.json`
- Option 4: Detail view scrolls to campaign history section
- Option 5: Returns to CHARACTER_GALLERY

**Accessibility Notes**:
- Edit button: pencil icon + text label "Edit" for clarity
- Delete button: red/destructive styling + text label "Delete" + confirmation required
- Export button: share/download icon + text label "Export"
- Confirmation dialog: Focus should move to Cancel button by default (safer option)
- All buttons: Min 44×44pt/48×48dp touch target

---

### Decision 4: Confirm Destructive Actions
**User Choice**: Am I sure I want to delete this character or discard changes?

**Options**:
1. `Delete Character Confirmation`:
   - `Delete` → Permanently removes character (JSON + portrait)
   - `Cancel` → Returns to CHARACTER_DETAIL
2. `Discard Changes Confirmation` (during create/edit):
   - `Discard` → Returns to origin state (gallery or detail), unsaved data lost
   - `Keep Editing` → Returns to form with data preserved

**System Response**:
- Delete → Character files deleted, card removed from gallery with fade-out animation, toast message "[Name] deleted"
- Cancel/Keep Editing → Dialog dismissed, user returns to previous state
- Discard → Form closed, unsaved data discarded, returns to previous state

**Accessibility Notes**:
- Confirmation dialogs: Use modal dialogs with distinct button styling (primary vs destructive)
- Default focus: Cancel/Keep Editing (safer option) should be focused by default
- Screen reader: Announce full warning text including character name and campaign count
- High contrast mode: Destructive buttons (Delete, Discard) must have red text or border in addition to color

---

### Decision 5: Upload Character Portrait (Required)
**User Choice**: Which portrait image should I upload for my character?

**Options**:
1. `Upload Custom Portrait` → Tap upload button → Platform file picker → Select PNG/JPEG/WebP (max 5MB)
2. `Use Existing Image` → Select from device photo library or files
3. `Skip (Not Allowed)` → Generate button remains disabled; system displays "Portrait required: System cannot generate images"

**System Response**:
- Option 1 & 2: Selected image validated (format + size), cropped/resized to 512×512px, preview displayed, Generate button enabled
- Option 3: User cannot proceed without portrait (Generate button disabled)

**Validation Errors**:
- Invalid format: "Portrait must be PNG, JPEG, or WebP"
- File too large: "Portrait must be under 5MB"
- Unreadable file: "Could not load selected image"

**Accessibility Notes**:
- Upload button: Large touch target (80×80pt minimum for portrait tap area)
- Screen reader: "Add portrait, required, button", "Portrait preview: [filename or 'not set']"
- Alternative text: After upload, character name (or "Character portrait") used as alt text for image
- Error messages announced immediately by screen reader

**Design Note**:
- Portrait is **required** before character generation (in-process LLM cannot generate images)
- Future enhancement: If image generation capability added (external API or future model), make portrait optional with "Generate Portrait" button
- Portrait upload precedes character generation (Step 3.3 before Step 3.4 in steps.md)

---

## Secondary Interactions

### Scrolling and Navigation
- **Character Gallery**: Vertical scrolling for large character lists (50+ cards), smooth scroll with momentum
- **Character Detail**: Vertical scrolling for long backstories and campaign history lists
- **Form Fields**: Multiline text areas (backstory, voice description) should auto-expand as user types or support scrolling within field

### Long-Press Actions (Optional Enhancement)
- **Character Card Long-Press**: Quick action menu appears with Edit, Delete, Export options (skips detail view)
- **Portrait Long-Press**: Quick replace/remove portrait menu

### Keyboard Shortcuts (Desktop)
- `Cmd/Ctrl + N` → New Character (when in gallery)
- `Cmd/Ctrl + S` → Save (when in create/edit form)
- `Escape` → Cancel (close current form or dialog)
- `Cmd/Ctrl + Backspace/Delete` → Delete character (when in detail view, shows confirmation)

### Unsaved Changes Warning
- Trigger: User taps Cancel or Back button in CHARACTER_CREATE or CHARACTER_EDIT with modified data
- Dialog: "Discard changes? [Character Name or 'Your new character'] will not be saved."
- Detection: Form tracks whether any field has been modified since opening

---

## Analytics Points

Track these user behaviors for product insights:

1. **Character Creation Rate**:
   - How many players create characters before starting first campaign?
   - Average number of characters per player

2. **Form Completion**:
   - Which fields are most commonly filled vs left empty?
   - Do players who fill backstories play campaigns longer?

3. **Character Reuse**:
   - How many campaigns does average character participate in?
   - Do players prefer creating new characters or reusing existing ones?

4. **Portrait Usage**:
   - What percentage of characters have custom portraits?
   - How many players use generated portraits vs upload custom images?

5. **Import/Export Usage**:
   - How many players export characters (for backup or sharing)?
   - How many players import characters (from friends or online)?

6. **Edit Frequency**:
   - How often do players edit characters after creation?
   - Which fields are most commonly edited?

7. **Deletion Patterns**:
   - How long (on average) between character creation and deletion?
   - Do players delete characters with many campaigns, or only unused ones?

---

## Platform-Specific Considerations

### iOS
- **File Picker**: Use `UIDocumentPickerViewController` for import/export
- **Sharing**: Export should use `UIActivityViewController` (share sheet) for AirDrop, Files app, etc.
- **Haptic Feedback**: Provide subtle haptic on Save, Delete, and Cancel actions
- **Safe Area**: Ensure form buttons respect iPhone notch and Dynamic Island

### Android
- **File Picker**: Use `Intent.ACTION_OPEN_DOCUMENT` for import, `Intent.ACTION_CREATE_DOCUMENT` for export
- **Material Design**: Use Material 3 components (TextField, Card, FAB for New Character)
- **Snackbar**: Use Snackbar for brief confirmation messages (e.g., "Character deleted")
- **Navigation**: Support gesture navigation (swipe from left edge to go back)

### Desktop (macOS/Windows/Linux)
- **File Dialogs**: Use native file open/save dialogs
- **Keyboard Navigation**: Full keyboard support (Tab, Arrow keys, Enter, Escape)
- **Window Management**: Character detail view can open in separate window or modal overlay
- **Drag & Drop**: Support drag-and-drop of portrait images onto upload button

---

## Accessibility Requirements

### WCAG 2.1 Level AA Compliance

1. **Color Contrast**:
   - Text on backgrounds: minimum 4.5:1 ratio (3:1 for large text >18pt)
   - Destructive buttons: red must contrast against background and adjacent elements

2. **Focus Indicators**:
   - All interactive elements (buttons, text inputs) must have visible focus state
   - Focus ring: 2px solid, high-contrast color (blue or platform default)

3. **Screen Reader Support**:
   - All buttons labeled with purpose: "Edit character", "Delete character", "New character"
   - Form fields have associated labels (name, race, class, etc.)
   - Character cards announce: "Character: [Name], [Class], [Race], [N] campaigns. Double-tap to view details."
   - Validation errors announced immediately when triggered

4. **Touch Target Size**:
   - iOS: minimum 44×44 points
   - Android: minimum 48×48 dp
   - Desktop: minimum 24×24 pixels (larger recommended)

5. **Keyboard Navigation**:
   - All interactive elements reachable via Tab key
   - Logical tab order: top-to-bottom, left-to-right
   - Form fields support Shift+Tab for reverse navigation
   - Escape key closes dialogs and forms

6. **Motion Reduction**:
   - Respect `prefers-reduced-motion` setting
   - Disable slide-in/fade-out animations if user prefers reduced motion
   - Use instant transitions instead

7. **Text Scaling**:
   - UI must remain usable at 200% text scale (WCAG requirement)
   - Form layout should reflow gracefully with larger text sizes
   - Portrait images should not overlap text when scaled

---

## Usability Heuristics

1. **Visibility of System Status**: Form validation provides real-time feedback (✓ for valid name, ✗ for conflict)
2. **User Control**: Cancel buttons always available, no autosave (explicit user control)
3. **Consistency**: Same form layout for create and edit (reduces learning curve)
4. **Error Prevention**: Confirm dialogs for destructive actions (delete, discard changes)
5. **Recognition over Recall**: Recent characters appear first in gallery, campaign history visible in detail view
6. **Flexibility**: Optional fields allow players to create minimal or detailed characters based on preference
7. **Aesthetic and Minimalist Design**: Only essential fields shown, optional fields grouped or collapsible
