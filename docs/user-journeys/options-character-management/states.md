# Options → Character Management - States

## UI/Application States

The character management feature progresses through distinct UI states as the player creates, edits, browses, and deletes character profiles.

### State Hierarchy

```
MAIN_MENU
  ↓
OPTIONS_PANEL
  ↓
CHARACTER_GALLERY
  ├─ Transitions to: MAIN_MENU, OPTIONS_PANEL, CHARACTER_DETAIL, CHARACTER_CREATE
  │
  ├→ CHARACTER_DETAIL
  │   ├─ Transitions to: CHARACTER_GALLERY, CHARACTER_EDIT, CHARACTER_DELETE_CONFIRM, EXPORT_CHARACTER
  │   └─ Active: Viewing character details and campaign history
  │
  ├→ CHARACTER_CREATE
  │   ├─ Transitions to: CHARACTER_GALLERY, CHARACTER_CREATE_CANCEL_CONFIRM, CHARACTER_GENERATE_PREVIEW
  │   └─ Active: Entering freeform character description and uploading portrait
  │
  ├→ CHARACTER_GENERATE_PREVIEW
  │   ├─ Transitions to: CHARACTER_GALLERY, CHARACTER_EDIT, CHARACTER_GENERATE_PREVIEW (regenerate)
  │   └─ Active: Reviewing LLM-generated character profile (2-5s generation + user review)
  │
  ├→ CHARACTER_EDIT
  │   ├─ Transitions to: CHARACTER_DETAIL, CHARACTER_EDIT_CANCEL_CONFIRM, CHARACTER_GENERATE_PREVIEW (regenerate)
  │   └─ Active: Modifying character data manually or regenerating from prompt
  │
  ├→ CHARACTER_DELETE_CONFIRM (modal)
  │   ├─ Transitions to: CHARACTER_DETAIL, CHARACTER_GALLERY (after delete)
  │   └─ Active: Confirmation dialog for deletion
  │
  ├→ IMPORT_CHARACTER
  │   ├─ Transitions to: CHARACTER_GALLERY
  │   └─ Active: File picker and validation
  │
  └→ EXPORT_CHARACTER
      ├─ Transitions to: CHARACTER_DETAIL
      └─ Active: File save dialog
```

---

## State Definitions

### CHARACTER_GALLERY
**Duration**: 5-60 seconds (user browsing characters)

**UI Elements**:
- Heading: "Characters"
- Grid layout of character cards (2-3 columns depending on screen width)
- Each card displays:
  - Portrait thumbnail (or placeholder icon)
  - Character name
  - Class/race badge (e.g., "Knight • Human")
  - Campaign count badge (e.g., "3 campaigns")
- "New Character" button (prominent, typically top-right or floating action button)
- Back button (returns to Options panel or Main Menu)
- Menu icon (⋮) for import action

**Data Loaded**:
- All character profile JSON files from `<user_data>/characters/` directory
- Portrait thumbnails (lazy-loaded as user scrolls)
- Campaign count for each character (computed from `campaign_history` array)

**Transitions**:
- → `OPTIONS_PANEL` or `MAIN_MENU` (tap Back)
- → `CHARACTER_DETAIL` (tap any character card)
- → `CHARACTER_CREATE` (tap New Character button)
- → `IMPORT_CHARACTER` (tap menu icon → Import)

**Notes**:
- Empty state: If no characters exist, show placeholder with "Create Your First Character" button
- Sorting: Characters ordered by `last_used` descending (most recently used first), then alphabetically by name

---

### CHARACTER_DETAIL
**Duration**: 10-60 seconds (user reading character details)

**UI Elements**:
- Full-screen or modal view (depending on platform)
- Large portrait at top (or placeholder)
- Character name (heading)
- Archetype section: Race, Class, Subclass (if defined)
- Personality section:
  - Traits: list of trait tags
  - Flaws: list of flaw tags
  - Virtues: list of virtue tags
  - Voice description: prose text
- Backstory section: scrollable multiline text (if defined)
- Campaign History section:
  - Scrollable list of campaigns this character has been used in
  - Each entry: campaign title, completion status, playtime, ending reached
  - If empty: "Not yet used in any campaigns"
- Action buttons (bottom or toolbar):
  - Edit (pencil icon)
  - Delete (trash icon, destructive style)
  - Export (share/download icon)
  - Back (return to gallery)

**Data Loaded**:
- Full character profile JSON
- Full resolution portrait
- Campaign history with titles (requires lookup to campaign manifests for titles)

**Transitions**:
- → `CHARACTER_GALLERY` (tap Back)
- → `CHARACTER_EDIT` (tap Edit button)
- → `CHARACTER_DELETE_CONFIRM` (tap Delete button)
- → `EXPORT_CHARACTER` (tap Export button)

**Notes**:
- Scrollable content: Backstory and campaign history sections should support vertical scrolling if content exceeds viewport

---

### CHARACTER_CREATE
**Duration**: 1-5 minutes (user composing description + LLM generation)

**UI Elements**:
- Heading: "Create Character"
- **Freeform Description Input**:
  - Large multiline text area with prompt: "Describe your character..."
  - Character count indicator (recommendation: 50-500 words for best results)
  - Example prompt (collapsible): "Try: 'A grizzled veteran warrior haunted by past battles, seeking redemption through mentoring young adventurers...'"
- **Portrait Upload** (required):
  - Upload button → platform file picker (PNG/JPEG/WebP, max 5MB) → displays thumbnail preview
  - Required indicator: "Required: System cannot generate images"
  - Inline error if not uploaded: "Portrait required before generating character"
- **Action buttons**:
  - Cancel (top-left or inline)
  - Generate (bottom-right, enabled only when text area has ~25+ words and portrait uploaded)
- Validation messages: 
  - "Add a portrait to continue" (if Generate tapped without portrait)
  - "Describe your character to continue" (if Generate tapped with insufficient text)

**Data Loaded**:
- None (fresh creation flow)

**State Progression**:
1. User enters freeform text description
2. User uploads portrait (required)
3. User taps Generate → transition to `CHARACTER_GENERATE_PREVIEW`

**Transitions**:
- → `CHARACTER_GALLERY` (tap Cancel, with unsaved changes prompt if text entered)
- → `CHARACTER_GENERATE_PREVIEW` (tap Generate, after validation passes)
- → `CHARACTER_CREATE_CANCEL_CONFIRM` (tap Cancel with text entered)

**Notes**:
- No form fields: Character data derived entirely from LLM generation
- Keyboard should auto-focus on text area when view opens
- Portrait required before generation (in-process model cannot generate images)

---

### CHARACTER_GENERATE_PREVIEW
**Duration**: 2-5 seconds (LLM generation) + 10-60 seconds (user review)

**UI Elements**:
- **Generation Phase** (2-5 seconds):
  - Progress indicator: "Creating character..." with spinner
  - LLM (Phi-4/Phi-4-mini) generates structured JSON from freeform text
- **Preview Phase** (after generation):
  - Heading: "Review Character"
  - Display generated profile:
    - Portrait (uploaded by user)
    - Generated name (editable inline)
    - Generated archetype: Race, Class, Subclass (editable inline)
    - Personality section:
      - Traits: list of trait tags (editable)
      - Flaws: list of flaw tags (editable)
      - Virtues: list of virtue tags (editable)
      - Speech patterns: prose text (editable)
    - Background: scrollable multiline text (editable)
    - Goals: list (editable)
  - Original prompt displayed (collapsible section at bottom)
  - Action buttons:
    - Cancel (returns to gallery, discards character)
    - Regenerate (re-run LLM with same prompt, creates new interpretation)
    - Edit (opens full edit form for manual adjustment)
    - Save (saves generated character to profile)

**Data Loaded**:
- LLM-generated character JSON (stored in memory, not yet saved to disk)
- Uploaded portrait (temporary, not yet saved to disk)
- Original freeform description (stored in `creation_prompt` field)

**Transitions**:
- → `CHARACTER_GALLERY` (tap Cancel)
- → `CHARACTER_GENERATE_PREVIEW` (tap Regenerate, re-runs LLM generation)
- → `CHARACTER_EDIT` (tap Edit, opens manual edit form with generated data pre-filled)
- → `CHARACTER_GALLERY` (tap Save, after successful save)

**Notes**:
- Inline editing: User can click any field to edit directly in preview (without opening full edit form)
- Regenerate creates new interpretation from same prompt (useful if user wants different name/traits)
- LLM generation time: typically 2-5 seconds on device

---

### CHARACTER_EDIT
**Duration**: 30 seconds to 5 minutes (user modifying character or regenerating)

**UI Elements**:
- Heading: "Edit [Character Name]"
- **Edit Mode Selection** (presented as tabs or buttons):
  1. **Edit Fields** (manual adjustment):
     - Same form fields as preview mode (name, archetype, personality, background, goals, speech patterns)
     - All fields pre-populated with existing data
     - Portrait upload button (can replace existing portrait)
  2. **Regenerate from Prompt** (only available for LLM-generated characters):
     - Text area displays original `creation_prompt` (editable)
     - User can modify prompt before regenerating
     - Generate button → re-runs LLM generation (same as Step 3.4 in steps.md)
     - Transition to `CHARACTER_GENERATE_PREVIEW` with new generation
- Action buttons:
  - Cancel (reverts changes, returns to detail view)
  - Save (updates character profile, only in Edit Fields mode)
- Validation messages: Name uniqueness (if name changed), portrait file type

**Data Loaded**:
- Full character profile JSON (pre-populated in form)
- Existing portrait (displayed in upload button preview)
- Original `creation_prompt` (if character was LLM-generated; otherwise Regenerate option not shown)
- Other character names (for uniqueness validation if name changed)

**Transitions**:
- → `CHARACTER_DETAIL` (tap Cancel, with unsaved changes prompt if modified)
- → `CHARACTER_DETAIL` (tap Save, after successful update in Edit Fields mode)
- → `CHARACTER_GENERATE_PREVIEW` (tap Generate in Regenerate mode, re-runs LLM generation)
- → `CHARACTER_EDIT_CANCEL_CONFIRM` (tap Cancel with unsaved changes)

**Notes**:
- Portrait replacement: Tapping upload button replaces existing portrait
- Name change: If name is changed, uniqueness validation runs
- Regenerate preserves portrait: Only character data is regenerated; portrait remains unchanged

---

### CHARACTER_DELETE_CONFIRM
**Duration**: 3-10 seconds (user reading confirmation)

**UI Elements**:
- Modal dialog
- Heading: "Delete [Character Name]?"
- Warning text:
  - "This character has been used in [N] campaign(s)." (if N > 0)
  - "Deleting will not affect saved games, but this character profile will be permanently removed."
  - "This action cannot be undone."
- Action buttons:
  - Cancel (secondary style, dismisses dialog)
  - Delete (destructive style, typically red)

**Data Loaded**:
- Character name
- Campaign count (from `campaign_history` array length)

**Transitions**:
- → `CHARACTER_DETAIL` (tap Cancel)
- → `CHARACTER_GALLERY` (tap Delete, after successful deletion)

**Notes**:
- Deletion is immediate (no undo feature in MVP)
- Both character JSON and portrait file are deleted

---

### CHARACTER_CREATE_CANCEL_CONFIRM
**Duration**: 2-5 seconds

**UI Elements**:
- Modal dialog or action sheet
- Heading: "Discard changes?"
- Message: "Your new character will not be saved."
- Action buttons:
  - Keep Editing (primary, returns to form)
  - Discard (destructive, returns to gallery)

**Transitions**:
- → `CHARACTER_CREATE` (tap Keep Editing)
- → `CHARACTER_GALLERY` (tap Discard)

---

### CHARACTER_EDIT_CANCEL_CONFIRM
**Duration**: 2-5 seconds

**UI Elements**:
- Modal dialog or action sheet
- Heading: "Discard changes?"
- Message: "Changes to [Character Name] will not be saved."
- Action buttons:
  - Keep Editing (primary, returns to edit form)
  - Discard (destructive, returns to detail view)

**Transitions**:
- → `CHARACTER_EDIT` (tap Keep Editing)
- → `CHARACTER_DETAIL` (tap Discard)

---

### IMPORT_CHARACTER
**Duration**: 1-5 seconds (file selection and validation)

**UI Elements**:
- Platform-specific file picker dialog (e.g., iOS document picker, Android file chooser)
- Filter: `.json` files only
- After selection: Brief loading indicator while validating file
- Validation error modals (if necessary):
  - "Invalid character file (not valid JSON)"
  - "Character file is incomplete (missing: [field list])"
  - Name conflict dialog: "A character named '[name]' already exists. Rename imported character?"
    - Text input for new name
    - Buttons: Cancel, Import with New Name

**Data Loaded**:
- Selected JSON file contents
- Existing character names (for conflict detection)

**Transitions**:
- → `CHARACTER_GALLERY` (on successful import, with new card animated in)
- → `CHARACTER_GALLERY` (on cancel or error, with error toast message)

**Notes**:
- UUID conflict: If imported character has an `id` that matches an existing character, a new UUID is generated automatically
- Portrait not imported: Only JSON data is imported; user must manually add portrait if desired

---

### EXPORT_CHARACTER
**Duration**: 1-3 seconds (file save operation)

**UI Elements**:
- Platform-specific file save dialog (e.g., share sheet on iOS, file picker on Android)
- Suggested filename: `{character_name}_{date}.json` (e.g., `knight_eredin_2026-02-13.json`)
- Brief toast message on success: "[Character Name] exported"

**Data Loaded**:
- Character profile JSON (serialized for export)

**Transitions**:
- → `CHARACTER_DETAIL` (after export completes or is cancelled)

**Notes**:
- Portrait not exported: Only JSON file is exported (user must manually copy portrait file if sharing)

---

## State Transition Rules

1. **Back button always returns to previous state** — No state is a dead-end
2. **All create/edit forms validate before saving** — Save button disabled until validation passes
3. **Destructive actions require confirmation** — Delete and Cancel-with-changes show confirm dialogs
4. **Gallery is the "home" state** — Most actions return to gallery after completion
5. **No autosave** — All changes require explicit Save button tap
6. **Character gallery state is persistent** — Returning to gallery shows updated list (no refresh needed)
7. **Import/Export are one-way operations** — Once initiated, they complete or cancel (no partial states)

---

## Error Handling

| Error | State | Recovery |
|-------|-------|----------|
| File system write failure (save character) | CHARACTER_CREATE or CHARACTER_EDIT | Show error dialog: "Could not save character (storage full or read-only)". Retry button or Cancel (returns to previous state). |
| Portrait file too large (>5MB) | CHARACTER_CREATE or CHARACTER_EDIT | Inline error: "Portrait must be under 5MB". User must select different file. |
| Invalid JSON on import | IMPORT_CHARACTER | Error dialog: "Could not read character file (invalid format)". Returns to gallery. |
| Name conflict on import (after user skips rename) | IMPORT_CHARACTER | Character not imported; error toast: "Import cancelled (name conflict)". Returns to gallery. |
| Character deletion fails (file locked) | CHARACTER_DELETE_CONFIRM | Error dialog: "Could not delete character (file in use)". Returns to detail view. |
