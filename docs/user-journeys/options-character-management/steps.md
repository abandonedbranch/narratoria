# Options → Character Management - Steps

## Phases

### Phase 1: Entering Character Management

**Step 1.1: Open Options from Main Menu**
- Player action: Tap `Options` button on Main Menu
- System response: Settings panel slides in from right edge (~300ms animation)
- Settings panel displays categories: Audio, Display, Accessibility, Cloud Sync, **Characters**, Data Management
- Next step: Step 1.2

**Step 1.2: Navigate to Characters Section**
- Player action: Tap `Characters` option in settings panel
- System response: 
  - Character gallery view loads
  - Settings panel breadcrumb updates: "Options > Characters"
  - Character profiles load from local storage (~100-500ms)
- Data loaded: All character profile JSON files from user data directory (see [architecture.md Section 6.2.7](../../architecture.md#627-player-character-profiles))
- Next step: Phase 2 (browsing gallery)

---

### Phase 2: Browsing Character Gallery

**Step 2.1: View Character Gallery**
- Player action: Scroll through character cards in grid layout
- System response:
  - Each card displays: portrait (or placeholder), name, class/race, campaign count
  - Cards are tappable to view details
  - "New Character" button visible at top
- Duration: 5-30 seconds (user browsing)
- Next step: Step 2.2 (tap card) or Step 3.1 (tap New Character)

**Step 2.2: View Character Details**
- Player action: Tap character card
- System response:
  - Detail view opens (full-screen or modal)
  - Displays: full portrait, name, archetype (race/class/subclass), personality traits, backstory (scrollable), campaign history list
  - Action buttons visible: Edit, Delete, Export, Back
- Next step: Step 2.3 (tap action button) or return to gallery (tap Back)

**Step 2.3: View Campaign History**
- Player action: Scroll campaign history section in character details
- System response:
  - Each campaign entry shows: campaign title, completion status (✓/in progress), playtime hours, ending reached (if completed)
  - Example: "Chronicles of Merlin - Completed (8.5 hours) - Redemption ending"
- Next step: Return to character details (scroll back up) or tap Back to gallery

---

### Phase 3: Creating New Character

**Step 3.1: Initiate Character Creation**
- Player action: Tap `New Character` button in gallery view
- System response:
  - Character creation interface slides in
  - Interface displays: Large freeform text area with prompt "Describe your character..." (multiline, expandable), Portrait upload button (required - user must provide portrait), Generate button (enabled when text entered)
  - Cancel button visible
- Next step: Step 3.2

**Step 3.2: Enter Character Description**
- Player action: Type freeform character description in text area
- System response:
  - Text area accepts natural language description (e.g., "A grizzled veteran warrior haunted by past battles, seeking redemption through mentoring young adventurers")
  - Character count indicator shown (recommendation: 50-500 words for best results)
  - Generate button enables once minimum text entered (~25 words)
- Duration: 1-5 minutes (user composing description)
- Next step: Step 3.3 (tap Generate) or cancel (returns to gallery)

**Step 3.3: Upload Portrait (Required)**
- Player action: Tap portrait upload button → select image from device
- System response:
  - File picker opens (platform-specific)
  - Selected image validated: must be PNG, JPEG, or WebP; max 5MB
  - Image cropped/resized to 512x512px
  - Preview displayed in interface
- Error case: If invalid format/size, show error: "Portrait must be PNG, JPEG, or WebP under 5MB"
- Note: Portrait is required before generation (system cannot generate images in-process)
- Next step: Continue to Step 3.4 (tap Generate)

**Step 3.4: Generate Character Profile**
- Player action: Tap `Generate` button
- System response:
  - Progress indicator shows: "Creating character... (~2-5 seconds)"
  - In-process LLM (Phi-4/Phi-4-mini) generates structured JSON profile from freeform text
  - Generated profile includes: name, archetype (race/class/subclass), personality (traits/flaws/virtues), background (expanded from user description), goals, speech patterns
  - Preview screen displays generated character with all fields visible
  - Action buttons: Regenerate (create new interpretation), Edit (manual adjustment), Save, Cancel
- Duration: 2-5 seconds (LLM generation)
- Next step: Step 3.5 (review and save) or Step 3.4 (regenerate with same prompt)

**Step 3.5: Review and Save Generated Character**
- Player action: Review generated profile, optionally tap `Edit` to adjust fields, then tap `Save`
- System response:
  - If editing: form opens with all generated fields editable (name, archetype, personality, background, goals, speech patterns)
  - On Save: Character profile JSON created with UUID-based ID
  - File saved to local storage: `<user_data>/characters/{id}.json`
  - Portrait saved: `<user_data>/characters/{id}_portrait.{ext}`
  - Original freeform description stored in `creation_prompt` field for future regeneration
  - Character card appears in gallery with visual confirmation (slide-in animation)
  - Returns to gallery view
- Duration: <500ms (save operation)
- Next step: Return to Phase 2 (gallery browsing)

---

### Phase 4: Editing Existing Character

**Step 4.1: Initiate Character Edit**
- Player action: From character detail view, tap `Edit` button
- System response:
  - Edit interface opens with two options:
    - **Edit Fields**: Manual adjustment of character data (name, archetype, personality, background, goals, speech patterns, portrait)
    - **Regenerate from Prompt**: Re-run LLM generation using stored `creation_prompt` (available only if character was LLM-generated)
  - Cancel button visible
- Next step: Step 4.2 (manual edit) or Step 4.3 (regenerate)

**Step 4.2: Modify Character Data (Manual Edit)**
- Player action: Tap "Edit Fields" → change any field
- System response:
  - Form displays all character fields as editable
  - Changes validated in real-time (e.g., name uniqueness check if name changed)
  - Can also change portrait by tapping portrait upload button
  - Save button enabled if all validations pass
- Duration: 30 seconds to 5 minutes
- Next step: Step 4.4 (tap Save) or cancel (reverts changes, returns to detail view)

**Step 4.3: Regenerate from Original Prompt**
- Player action: Tap "Regenerate from Prompt" (only available for LLM-generated characters)
- System response:
  - Text area displays original `creation_prompt` (editable)
  - User can modify prompt before regenerating
  - Generate button visible
  - On tap Generate: Same workflow as Step 3.4 (LLM re-generates character profile)
  - Preview shows new generated profile with Save/Cancel/Regenerate buttons
- Duration: 2-5 seconds (LLM generation)
- Next step: Step 4.4 (save regenerated profile) or cancel (keep existing profile)

**Step 4.4: Save Changes**
- Player action: Tap `Save` button
- System response:
  - Character profile JSON updated with new data
  - `last_used` timestamp **not** updated (only updated when character used in campaign)
  - Visual confirmation: brief toast message "Character updated"
  - Returns to detail view with updated data
- Duration: <300ms
- Next step: Return to detail view (Step 2.2) or back to gallery

---

### Phase 5: Deleting Character

**Step 5.1: Initiate Character Deletion**
- Player action: From character detail view, tap `Delete` button
- System response:
  - Confirmation dialog appears: "Delete [Name]?" with warning text
  - Warning: "This character has been used in [N] campaign(s). Deleting will not affect saved games, but this character profile will be permanently removed."
  - Buttons: Cancel, Delete (destructive style, typically red)
- Next step: Step 5.2 (tap Delete) or cancel (returns to detail view)

**Step 5.2: Confirm Deletion**
- Player action: Tap `Delete` in confirmation dialog
- System response:
  - Character profile JSON deleted from local storage
  - Portrait file deleted (if exists)
  - Character card removed from gallery (fade-out animation)
  - Returns to gallery view
  - Brief toast message: "[Name] deleted"
- Duration: <300ms
- Next step: Return to Phase 2 (gallery browsing)

---

### Phase 6: Importing/Exporting Characters

**Step 6.1: Export Character**
- Player action: From character detail view, tap `Export` button
- System response:
  - Platform-specific file save dialog opens
  - Suggested filename: `{character_name}_{date}.json` (e.g., `knight_eredin_2026-02-13.json`)
  - JSON file written to chosen location
  - Portrait **not** included (only JSON; user must manually copy portrait if desired)
- Duration: <1 second
- Next step: Return to detail view

**Step 6.2: Import Character**
- Player action: From character gallery, tap menu icon (⋮) → Import
- System response:
  - Platform-specific file picker opens (filter: `.json` files)
  - Selected file validated:
    - Must be valid JSON
    - Must contain required fields (`name`, `version`, `created_at`)
    - If `id` exists and conflicts with existing character, new UUID generated
    - If `name` conflicts, user prompted to rename or skip
  - Character profile created in local storage
  - New card appears in gallery
- Error cases:
  - Invalid JSON: "Could not read character file (invalid format)"
  - Missing required fields: "Character file is incomplete (missing: name, version)"
- Duration: 1-3 seconds
- Next step: Return to gallery (Step 2.1)

---

### Alternative Flow 1: Empty Gallery (First Use)

**Step A.1: No Characters Exist**
- Player action: Navigate to Characters section (Step 1.2)
- System response:
  - Gallery shows empty state: placeholder icon, text "No characters yet"
  - Prominent "Create Your First Character" button
- Next step: Step 3.1 (tap button to create character)

---

### Alternative Flow 2: Cancel During Creation/Edit

**Step B.1: Cancel Character Creation**
- Player action: Tap `Cancel` button during character creation (Step 3.2) or edit (Step 4.2)
- System response:
  - Unsaved changes prompt (if any data entered): "Discard changes?"
  - Buttons: Keep Editing, Discard
  - If Discard: returns to gallery (creation) or detail view (edit)
  - If Keep Editing: returns to form
- Next step: Return to previous state or continue editing

---

### Alternative Flow 3: Portrait Generation (Future Enhancement - Out of Scope)

**Note:** Portrait generation via AI is not available in current design. In-process models (Phi-4/Phi-4-mini) are text-only and cannot generate images. Image generation would require external API calls or future model capabilities.

**Hypothetical Future Step C.1: Generate Portrait with AI**
- Player action: Tap "Generate Portrait" button (not in current scope)
- System response:
  - Would require external API or future visual model integration
  - Progress indicator: "Generating portrait... (~10-30 seconds)"
  - Generated image displayed with `generated: true` metadata
  - Player can accept, regenerate, or upload custom image instead
- Next step: Continue with character creation/edit
