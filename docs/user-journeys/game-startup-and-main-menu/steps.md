# Game Startup and Main Menu - Steps

## Detailed Step-by-Step Breakdown

### Phase 1: Application Launch

**Step 1.1: User launches the application**
- User taps Narratoria icon on home screen (mobile) or double-clicks app icon (desktop)
- Application starts, splash screen displays with logo and loading indicator

**Step 1.2: Application initializes**
- Runtime loads configuration from device storage
- Models download check: Phi-4 (or Phi-4-mini) and sentence-transformers verify cached locally
  - If not present: Download from HuggingFace Hub (~3-8GB total, shows progress dialog)
  - If present: Load from cache (takes ~2-5 seconds)
- Built-in campaigns discovered and indexed
- UI framework initializes

**Step 1.3: Main Menu displays**
- Splash screen fades; main menu appears
- Player sees four primary options: `New Game`, `Load Game`, `Options`, `Exit`
- If no campaigns available: "No campaigns found" message with link to documentation

---

### Phase 2: Campaign Selection

**Step 2.1: Player taps "New Game"**
- Main menu animates out
- Campaign discovery begins: "Loading campaigns..." indicator shows
- Campaign selection screen animates in
- "Select Campaign" heading displays

**Step 2.2: Campaigns are discovered and displayed**
- System scans campaign locations per [Architecture Section 6.3.6](../../../architecture.md#6.3.6-campaign-discovery-and-loading)
  - Built-in campaigns (Official badge)
  - Downloaded campaigns (User Imported badge)
  - iCloud campaigns (Cloud badge, if iOS and enabled)
  - Previous playthroughs (Resume badge)
- Campaign list displays with scrolling
- Each campaign entry shows: title, thumbnail/artwork (if available), brief description, play time estimate
- "Back" button returns to main menu
- "Options" button accessible (without interrupting campaign selection)

**Step 2.3: Player selects a campaign**
- Player taps a campaign title
- Campaign detail view animates in
- Displays: Full title, author (if provided), longer description, genre/tone tags, artwork gallery (if available)
- Shows: Estimated playtime
- **Content Warnings Indicators**: If campaign has warnings, small badges/tags display above or beside the description:
  - Visual style: Color-coded tags (e.g., red for "Violence", purple for "Psychological Themes", orange for "Moral Complexity")
  - Each warning tag is tappable to expand with full description
  - Example: `[⚠️ Violence]` `[⚠️ Strong Language]` `[⚠️ Psychological Themes]`
  - Indicators appear at a glance without requiring scroll to find warnings
- Buttons: "Select Character", "Back"

**Step 2.4: Player taps "Select Character"**
- Detail view closes
- Character selection screen animates in
- Backend loads character templates allowed by this specific campaign (from [Architecture Section 6.2.6](../../../architecture.md#6.2.6-player-character-template))

---

### Phase 3: Character Selection

**Step 3.1: Campaign-filtered character templates load**
- System retrieves campaign's `allowed_classes` or `allowed_races` from campaign manifest
- Only templates matching campaign constraints are displayed
- "Select Your Character" heading displays
- Character sheets display as a scrollable list or grid
- Each character card shows: portrait, name, class/role, brief description, starting stats (merged with campaign defaults if applicable)
- "Back" button available to return to campaign selection

**Step 3.2: Player selects a character**
- Player taps a character card
- Selected character highlights or animates
- Brief confirmation shows selected character details
- "Continue" button becomes active

**Step 3.3: Player confirms character selection and campaign choice**
- Player taps "Continue"
- Character selection screen transitions out
- If campaign has content warnings: Proceed to Phase 4
- If no warnings: Proceed directly to Phase 5 (Campaign Load)

---

### Phase 4: Content Warning Acknowledgment

**Step 4.1: Content warnings displayed**
- Modal dialog appears: "Content Warning"
- Lists all warnings from campaign manifest (e.g., "violence", "moral complexity", "psychological themes")
- Includes expanded descriptions if provided by campaign author
- Checkbox: "I understand and wish to proceed"
- Buttons: "Back", "Proceed"

**Step 4.2: Player acknowledges warnings**
- Player reads warnings and checks confirmation box (if required)
- Player taps "Proceed"
- Dialog closes; Campaign Load phase begins
- If player taps "Back": Return to campaign selection view (character selection skipped to preserve campaign context)

---

### Phase 5: Campaign Loading and First Scene

**Step 5.1: Campaign initialization**
- Loading screen displays: Campaign title, "Preparing your story..." message, progress bar
- Backend performs:
  1. Campaign ingestion (filesystem scan, semantic embedding of content)
  2. Session state initialization (empty or from character template)
  3. Persistence database setup (memories, reputation tracking)
  4. Initial scene plan generation via Phi-4/Phi-4-mini

**Step 5.2: First scene renders**
- Loading screen fades
- Story View appears with:
  - Narrative prose (2-3 paragraphs) describing the opening scene
  - Character portrait(s) if available
  - Ambient music fades in (if campaign includes audio assets)
  - 3-4 choice buttons display below the narrative
- Player is now in active gameplay

---

### Alternative Flow: Load Game

**Step A.1: Player taps "Load Game"**
- Main menu animates out
- Load screen displays list of saved playthroughs
- Each playthrough shows: campaign title, character name, play time, last played date, scene summary
- "Back" button to return to main menu

**Step A.2: Player selects a playthrough**
- Player taps a playthrough entry
- Playthrough detail view shows full progress and optional save point summary
- Buttons: "Resume", "Back"

**Step A.3: Player taps "Resume"**
- Loading screen appears: "Resuming your story..."
- Persistence layer loads: session state, memories, reputation, NPC data
- Campaign index loads from cache (fast if unchanged)
- Current scene re-renders from saved state
- Player returns to active gameplay at their last choice point

---

### Alternative Flow: Options

**Step B.1: Player taps "Options"** (from any screen)
- Settings panel slides in (usually from edge of screen)
- Settings include: Volume, brightness, text size, iCloud sync (iOS), accessibility options
- Can be accessed from main menu or in-game
- "Done" or "Back" closes panel

---

### Alternative Flow: Exit

**Step C.1: Player taps "Exit"**
- If in main menu: Application closes immediately
- If in-game: Confirmation dialog: "Save progress before exiting?" → Yes/No
  - **Yes**: Current state saved to persistence layer; app closes
  - **No**: App closes without saving
- On mobile: App suspends (iOS) or moves to background (Android), not fully terminated

