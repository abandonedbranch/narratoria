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

### Phase 3: Creating Fresh Character

**Step 3.1: Initiate Character Creation**
- Player action: Tap `New Character` button in gallery view
- System response:
  - Character creation interface slides in
  - Interface displays: Large freeform text area with prompt "Describe your character..." (multiline, expandable), Portrait upload button (required - user must provide portrait), Save button (disabled until text and portrait provided)
  - Cancel button visible
- Next step: Step 3.2

**Step 3.2: Enter Character Description**
- Player action: Type freeform character description in text area
- System response:
  - Text area accepts natural language description (e.g., "A gruff, battle-scarred knight who secretly loves poetry. Failed to save his king and now seeks redemption.")
  - Character count indicator shown (10-5000 characters)
  - Save button enables once minimum text entered (10 characters) AND portrait uploaded
- Duration: 1-5 minutes (user composing description)
- Next step: Step 3.3 (upload portrait) or cancel (returns to gallery)

**Step 3.3: Upload Portrait (Required)**
- Player action: Tap portrait upload button → select image from device
- System response:
  - File picker opens (platform-specific)
  - Selected image validated: must be PNG, JPEG, or WebP; max 5MB
  - Image cropped/resized to 512×512px
  - Preview displayed in interface
  - Save button enables if description also provided
- Error case: If invalid format/size, show error: "Portrait must be PNG, JPEG, or WebP under 5MB"
- Note: Portrait is required (system cannot generate images in-process)
- Next step: Step 3.4 (tap Save)

**Step 3.4: Save Fresh Character**
- Player action: Tap `Save` button
- System response:
  - Fresh character JSON created with UUID-based ID
  - File saved to local storage: `<user_data>/characters/{id}.json`
  - Portrait saved: `<user_data>/characters/{id}_portrait.{ext}`
  - Fresh character has status "fresh" (never used in campaign)
  - Character card appears in gallery with "Fresh" badge and visual confirmation (slide-in animation)
  - Returns to gallery view
- Duration: <500ms (no LLM generation—instant save)
- Next step: Return to Phase 2 (gallery browsing)

**Note on Character Realization**: Fresh characters do not have names, archetypes, or structured data until used in a campaign. At campaign start, the LLM generates a complete character profile from the description, tailored to the campaign's world and tone. The same fresh character can be realized differently in different campaigns (wizard in fantasy, detective in noir, pilot in sci-fi). See architecture.md Section 6.2.7 for realization details.

---

### Phase 4: Editing Fresh Character

**Step 4.1: Initiate Character Edit**
- Player action: From character detail view, tap `Edit` button
- System response:
  - Edit form opens showing:
    - Freeform text area with existing description (editable)
    - Portrait preview with "Change Portrait" button
  - Save and Cancel buttons visible
- Next step: Step 4.2

**Step 4.2: Modify Character Description or Portrait**
- Player action: Edit description text and/or tap "Change Portrait" to upload new image
- System response:
  - Text area allows full editing of description
  - Portrait upload follows same validation as creation (PNG/JPEG/WebP, <5MB, resized to 512×512px)
  - Save button enabled (no validation beyond text length 10-5000 chars)
- Duration: 30 seconds to 5 minutes
- Next step: Step 4.3 (tap Save) or cancel (reverts changes, returns to detail view)

**Step 4.3: Save Changes**
- Player action: Tap `Save` button
- System response:
  - Fresh character JSON updated with new description/portrait
  - Existing realizations (campaign usages) are **not** affected (they have their own realized character data)
  - Visual confirmation: brief toast message "Character updated"
  - Returns to detail view with updated data
- Duration: <300ms
- Next step: Return to detail view (Step 2.2) or back to gallery

**Note on Realized Characters**: Editing a fresh character does not change any existing realizations in campaigns. Each campaign has its own generated character data based on the description at the time of campaign start. To change a character within a campaign, the player must modify campaign-specific data (not implemented in this journey).

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
