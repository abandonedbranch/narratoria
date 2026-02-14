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
- Option 1: Character creation form slides in with empty fields, Save disabled until description entered AND portrait uploaded
- Option 2: Character detail view opens with description, portrait, and realization history
- Option 3: Platform file picker opens filtered to `.json` files
- Option 4: Returns to OPTIONS_PANEL or MAIN_MENU

**Accessibility Notes**:
- New Character button: Min 44×44pt touch target (iOS), 48×48dp (Android)
- Keyboard navigation: Tab order should be New Character → Character Cards → Menu → Back
- Screen reader: "Create new character" button, character cards announced as "Character: [description snippet], [status], [N] campaigns"

---

### Decision 2: Describe Character and Save
**User Choice**: How should I describe my character?

**Options**:
1. `Brief Description` → Enter 1-2 sentence description (e.g., "A brave knight"), upload portrait, tap Save
2. `Detailed Description` → 50-500 word description with personality, background, goals, upload portrait, tap Save
3. `Minimal Description` → Very short description (10+ characters minimum), upload portrait, tap Save
4. `Cancel` → Tap Cancel button without saving

**System Response**:
- Options 1-3: Fresh character JSON created instantly (<500ms) with description + portrait, gallery displays character with "Fresh" badge
- Option 4: If no text entered, returns to gallery immediately; if text/portrait added, shows "Discard changes?" confirmation

**Accessibility Notes**:
- Text area announced by screen reader: "Describe your character, text area, multiline"
- Character count indicator announced: "25 characters entered. 10 character minimum required"
- Save button: Min 44×44pt/48×48dp, visually distinct enabled vs disabled state
- Portrait upload required: Screen reader announces "Portrait required, upload button"

**Design Note**:
- Portrait is **required** before saving (system cannot generate images with in-process models)
- Text area has example prompt (collapsible): "Try: 'A gruff, battle-scarred knight who secretly loves poetry...'"
- Minimum 10 characters required for meaningful character description
- Character realization (LLM generation of name, archetype, personality) happens at campaign start, not during creation
- Same fresh character can be realized differently across campaigns (wizard in fantasy, detective in noir)

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
- Option 2: Confirmation dialog: "Delete this character? This action cannot be undone." → On confirm, character deleted and gallery updated
- Option 3: File save dialog with suggested name `character_{id_prefix}_{date}.json`
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
- Delete → Character files deleted (JSON + portrait), card removed from gallery with fade-out animation, toast message "Character deleted"
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
3. `Skip (Not Allowed)` → Save button remains disabled; system displays "Portrait required: System cannot generate images"

**System Response**:
- Option 1 & 2: Selected image validated (format + size), cropped/resized to 512×512px, preview displayed, Save button enabled (if description also meets minimum)
- Option 3: User cannot proceed without portrait (Save button disabled)

**Validation Errors**:
- Invalid format: "Portrait must be PNG, JPEG, or WebP"
- File too large: "Portrait must be under 5MB"
- Unreadable file: "Could not load selected image"

**Accessibility Notes**:
- Upload button: Large touch target (80×80pt minimum for portrait tap area)
- Screen reader: "Add portrait, required, button", "Portrait preview: [filename or 'not set']"
- Alternative text: After upload, "Character portrait" used as alt text for image
- Error messages announced immediately by screen reader

**Design Note**:
- Portrait is **required** before saving (in-process LLM cannot generate images)
- Future enhancement: If image generation capability added (external API or future model), make portrait optional with "Generate Portrait" button
- Portrait upload and description entry can happen in any order; both required before Save

---

## Secondary Interactions

### Scrolling and Navigation
- **Character Gallery**: Vertical scrolling for large character lists (50+ cards), smooth scroll with momentum
- **Character Detail**: Vertical scrolling for long descriptions and campaign realization history lists
- **Form Fields**: Description text area should auto-expand as user types or support scrolling within field

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
- Dialog: "Discard changes? Your character will not be saved."
- Detection: Form tracks whether description or portrait has been modified since opening

---

## Analytics Points

Track these user behaviors for product insights:

1. **Character Creation Rate**:
   - How many players create characters before starting first campaign?
   - Average number of characters per player

2. **Description Depth**:
   - Average description length (characters and words)
   - Do players who write longer descriptions have better realization outcomes?

3. **Character Reuse**:
   - How many campaigns does average character participate in (via realizations array)?
   - Do players prefer creating new characters or reusing existing ones?

4. **Portrait Upload**:
   - Average time from form open to portrait upload
   - Common portrait file formats (PNG vs JPEG vs WebP)

5. **Import/Export Usage**:
   - How many players export characters (for backup or sharing)?
   - How many players import characters (from friends or online)?

6. **Edit Frequency**:
   - How often do players edit character descriptions after creation?
   - Do players update portraits more or less often than descriptions?

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
   - Form fields have associated labels (description text area, portrait upload)
   - Character cards announce: "Character: [description snippet], [status], [N] campaigns. Double-tap to view details."
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

1. **Visibility of System Status**: Form validation provides real-time feedback (character count indicator, portrait status)
2. **User Control**: Cancel buttons always available, no autosave (explicit user control)
3. **Consistency**: Same form layout for create and edit (reduces learning curve)
4. **Error Prevention**: Confirm dialogs for destructive actions (delete, discard changes)
5. **Recognition over Recall**: Recent characters appear first in gallery, campaign history visible in detail view
6. **Flexibility**: Description field accepts minimal (10 chars) or detailed (5000 chars) input based on player preference
7. **Aesthetic and Minimalist Design**: Minimal creation form (description + portrait only) reduces friction
