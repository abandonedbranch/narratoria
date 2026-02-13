# Game Startup and Main Menu - States

## UI/Application States

The application progresses through distinct UI states as the player navigates from launch to active gameplay. Each state has specific UI elements, available actions, and transition conditions.

### State Hierarchy

```
APP_LAUNCHING
  ↓
APP_INITIALIZING (Model download/load, Campaign discovery)
  ↓
MAIN_MENU
  ├─ Transitions to: CAMPAIGN_DISCOVERY, SETTINGS, EXIT
  │
  ├→ CAMPAIGN_DISCOVERY
  │   ├─ Transitions to: MAIN_MENU, CAMPAIGN_DETAIL
  │   └─ Active: Campaign list viewing and scrolling
  │
  ├→ CAMPAIGN_DETAIL
  │   ├─ Transitions to: CAMPAIGN_DISCOVERY, CHARACTER_SELECTION
  │   └─ Active: Campaign info display with "Select Character" button
  │
  ├→ CHARACTER_SELECTION (filtered by campaign)
  │   ├─ Transitions to: CAMPAIGN_DISCOVERY, CONTENT_WARNINGS, CAMPAIGN_LOADING
  │   └─ Active: Character card selection (only templates allowed by campaign)
  │
  ├→ CONTENT_WARNINGS (if campaign defines warnings)
  │   ├─ Transitions to: CAMPAIGN_DISCOVERY, CAMPAIGN_LOADING
  │   └─ Active: Warning acknowledgment
  │
  ├→ CAMPAIGN_LOADING
  │   ├─ Transitions to: GAMEPLAY (on success), MAIN_MENU (on error)
  │   └─ Active: Progress indication, no user input
  │
  └→ GAMEPLAY
      └─ First scene narrative and choices visible
```

---

## State Definitions

### APP_LAUNCHING
**Duration**: ~1 second
**UI Elements**:
- Splash screen with logo
- App name and version
- No interactive elements

**Data Loaded**:
- None (startup phase)

**Transitions**:
- → `APP_INITIALIZING` (immediately)

---

### APP_INITIALIZING
**Duration**: 2-30 seconds (depending on model cache)
**UI Elements**:
- Splash screen
- Loading indicator (spinner or progress bar)
- Status message: "Initializing..." → "Loading AI models..." → "Discovering campaigns..." → "Ready"
- If models not cached: Progress dialog with percentage ("Downloading models: 45%")

**Data Loaded**:
- Phi-4 or Phi-4-mini language model (from cache or HuggingFace Hub)
- sentence-transformers embedding model (from cache)
- Built-in campaign manifests
- User-downloaded campaign list
- iCloud campaign metadata (iOS only)

**Transitions**:
- → `MAIN_MENU` (on successful initialization)
- → Error dialog (if initialization fails; user can retry or exit)

---

### MAIN_MENU
**Duration**: Open-ended (user choice)
**UI Elements**:
- Title/Logo at top
- Four primary buttons:
  - `New Game` (start new playthrough with campaign selection)
  - `Load Game` (resume saved playthrough)
  - `Options` (settings panel)
  - `Exit` (close application)
- Optional: News/Update panel, Recent playthroughs preview

**Data Loaded**:
- None (waiting for user action)

**Transitions**:
- → `CAMPAIGN_DISCOVERY` (tap New Game)
- → `LOAD_GAME_LIST` (tap Load Game)
- → `SETTINGS` (tap Options)
- → Exit application (tap Exit)

**Notes**:
- This is the "safe" landing state; user can always return here or start fresh

---

### CAMPAIGN_DISCOVERY
**Duration**: 10-60 seconds (user browsing campaigns)
**UI Elements**:
- "Select Campaign" heading
- Loading indicator (if campaigns still being scanned)
- Campaign list:
  - Scrollable list or gallery view
  - Each entry shows: Title, Thumbnail (if available), Badges (Official/User Imported/Cloud/Resume)
  - Tap to view details
- Back button
- Options button (accessible without interrupting)
- Featured/Recent campaigns section (optional)

**Data Loaded**:
- Campaign manifests from all sources:
  - Built-in campaigns (always displayed first)
  - Downloaded campaigns from app-specific directory
  - iCloud campaigns (if iOS and enabled)
  - Previous playthroughs (grouped separately)
- Campaign artwork thumbnails (if ingestion found them)

**Transitions**:
- → `MAIN_MENU` (tap Back)
- → `CAMPAIGN_DETAIL` (tap campaign entry)
- → `SETTINGS` (tap Options)

**Notes**:
- This state may show a "No campaigns found" message if no campaigns are available
- Loading state persists while iCloud campaigns are being checked (iOS)

---

### CAMPAIGN_DETAIL
**Duration**: 5-20 seconds (user reading campaign info)
**UI Elements**:
- Campaign title and author (if provided)
- Full description
- Genre/Tone tags (e.g., "High Fantasy", "Epic", "Moral Complexity")
- Content rating badge (if provided)
- Estimated playtime
- Art gallery (if campaign ingestion found assets):
  - Swipeable carousel of character portraits, locations, item art
  - Fallback to placeholder if no art available
- **Content warning badges** (if any present in manifest):
  - Color-coded tags displayed prominently near top of description
  - Each tag is tappable to reveal full warning description
  - Example visual: `[⚠️ Violence]` `[⚠️ Psychological Themes]` (with color coding)
  - Users see warnings immediately without needing to scroll
- Back button
- Select Character button

**Data Loaded**:
- Full campaign manifest
- Campaign artwork (character portraits, location art, etc.)
- Content warnings list

**Transitions**:
- → `CAMPAIGN_DISCOVERY` (tap Back)
- → `CHARACTER_SELECTION` (tap Select Character)

**Notes**:
- This view shows campaign details without yet committing to character selection
- User can explore multiple campaigns before choosing which one to play

---

### CHARACTER_SELECTION
**Duration**: 5-30 seconds (user browsing characters)
**UI Elements**:
- "Select Your Character" heading
- Campaign name displayed in subheading (showing which campaign they're playing)
- Character cards (grid or list) showing:
  - Portrait image
  - Name
  - Class/Role badge
  - Brief description (1-2 lines)
  - Starting stats preview (optional)
  - Selection highlight when tapped
- Back button (returns to campaign selection to choose different campaign)
- Continue button (active only when character selected)

**Data Loaded**:
- Character templates filtered by campaign's `allowed_classes` or `allowed_races` from [Campaign Format Section 6.2.6](../../../architecture.md#6.2.6-player-character-template)
- Only character sheets matching campaign constraints displayed
- Character artwork (from template library or campaign-specific overrides)

**Session State Updated**:
- `campaign.selected_id` = selected campaign ID
- `player.character_template_id` = selected character ID
- `player.base_stats` = character's starting stats merged with campaign defaults

**Transitions**:
- → `CAMPAIGN_DISCOVERY` (tap Back; deselects campaign, returns to campaign list)
- → `CONTENT_WARNINGS` (if campaign defines warnings) OR `CAMPAIGN_LOADING` (if no warnings)

**Notes**:
- Character selection is now filtered based on the campaign chosen
- Not all campaigns support all character types
- Going back returns to campaign selection (character choice is lost)

### CONTENT_WARNINGS
**Duration**: 3-15 seconds (user reading warnings)
**UI Elements**:
- Modal dialog
- "Content Warning" heading
- Warning list:
  - Bullet points or tags (e.g., "Violence", "Psychological Themes", "Moral Complexity")
  - Expanded descriptions (optional)
- Checkbox: "I understand and wish to proceed" (if author requires acknowledgment)
- Back button (return to campaign discovery, not character detail)
- Proceed button (disabled until checkbox ticked, if required)

**Data Loaded**:
- Content warnings from campaign manifest
- Warning descriptions from campaign manifest

**Transitions**:
- → `CAMPAIGN_DISCOVERY` (tap Back; returns to campaign list, losing character and campaign selections)
- → `CAMPAIGN_LOADING` (tap Proceed, after acknowledging)

**Notes**:
- This state only appears if campaign manifest includes `content_warnings` array
- Warnings are **informational**; player can always proceed after reading
- Going back abandons both campaign and character selection

---

### CAMPAIGN_LOADING
**Duration**: 5-30 seconds (depends on campaign size and hardware)
**UI Elements**:
- Loading screen (often with campaign artwork as background)
- Campaign title centered
- Progress message: "Preparing your story..." → "Loading campaign assets..." → "Generating first scene..."
- Progress bar (if granular feedback available)
- Cancelable (back button or swipe to exit) — cancels campaign load and returns to campaign selection

**Data Loaded** (Backend Operations):
1. Campaign ingestion:
   - Filesystem scan of campaign directory
   - Semantic chunking and embedding of all `.txt` files (lore, world descriptions, etc.)
   - Metadata extraction from JSON files
   - Asset path validation
2. Session state initialization:
   - Empty session state created
   - Character template stats injected
   - Skill state initialized
3. Persistence database setup:
   - Embedded database created for playthrough
   - Memory storage initialized
   - NPC reputation/perception tracking initialized
4. Plan generation:
   - Narrative context populated (campaign constraints, character, starting stats)
   - Phi-4 or Phi-4-mini generates Plan JSON for opening scene
5. Plan execution:
   - Skills execute to prepare opening scene narrative, assets, choices
   - State patches merge into session state
   - Assets (portraits, music) staged for rendering

**Transitions**:
- → `GAMEPLAY` (on successful load and plan execution)
- → Error dialog (on failure, with option to Retry or Return to Campaign Selection)

**Error Handling**:
- If plan generation fails: Replan loop attempts up to 5 times
- If all replans fail: Fallback template narration displays
- If critical error (campaign corrupted): Error message directs user back to campaign selection

---

### GAMEPLAY
**Duration**: Open-ended (user playing campaign)
**UI Elements**:
- Story View (main narrative display):
  - Narrative prose (left/center panel)
  - Character portraits (right panel or overlaid)
  - Ambient music player (minimized control)
- Choice buttons (3-4 options below narrative)
- State panel (inventory, stats, NPC relationships) — collapsible or tab-based
- Pause/Menu button (in-game options without exiting)
- Save/Load indicators (if applicable)

**Data Loaded**:
- Campaign content (lore, NPCs, plot beats)
- Session state (player stats, inventory, flags)
- Embedded models (Phi-4 for narrative generation)
- Memory database (for cross-session continuity)

**Transitions**:
- → Cyclic: Player choice → Plan generation → Scene renders → Loop
- → `SETTINGS` (pause menu, tap Options)
- → `MAIN_MENU` (pause menu, tap Exit, with save prompt)
- → `LOAD_GAME_LIST` (pause menu, tap Load)

**Notes**:
- This state can last hours (for long campaigns)
- Session auto-saves at configurable intervals (e.g., every 5 minutes)
- Player can manually save/load within this journey

---

### LOAD_GAME_LIST (Alternative Flow)
**Duration**: 5-20 seconds
**UI Elements**:
- "Load Game" heading
- Playthrough list:
  - Each entry: Campaign title, Character name, Play time (HH:MM), Last played date
  - Scene summary (first 1-2 lines of narrative)
  - Tap to view details
- Back button

**Data Loaded**:
- All saved playthroughs (from persistence layer)
- Playthrough metadata (campaign ID, character, timestamps)

**Transitions**:
- → `MAIN_MENU` (tap Back)
- → `PLAYTHROUGH_DETAIL` (tap playthrough entry)

---

### PLAYTHROUGH_DETAIL (Alternative Flow)
**Duration**: 3-10 seconds
**UI Elements**:
- Campaign title and character name
- Progress summary:
  - Playtime (e.g., "2h 34m played")
  - Current location/scene (if available)
  - Last played: Date and time
- Option preview (last 3 choices made)
- Back button
- Resume button

**Transitions**:
- → `LOAD_GAME_LIST` (tap Back)
- → `GAMEPLAY` (tap Resume, after loading playthrough from persistence)

---

### SETTINGS (Alternative Flow)
**Duration**: 5-30 seconds
**UI Elements**:
- Sliding panel (typically from right edge)
- Settings categories:
  - **Audio**: Master volume, music volume, SFX volume, narration volume
  - **Display**: Text size, brightness, theme (light/dark)
  - **Accessibility**: High contrast, dyslexia-friendly font, reduced motion
  - **Cloud Sync** (iOS): iCloud sync toggle, last sync timestamp
  - **Data Management**: Clear cache, export save, import save
- Done/Back button

**Transitions**:
- → Previous state (tap Done/Back)
  - If opened from MAIN_MENU → back to MAIN_MENU
  - If opened from CAMPAIGN_DISCOVERY → back to CAMPAIGN_DISCOVERY
  - If opened from GAMEPLAY → back to GAMEPLAY (pause menu)

---

## State Transition Rules

1. **User can always return to previous state** — Back buttons are always available
2. **No state saves intermediate progress** except CAMPAIGN_LOADING (which is non-interactive)
3. **Character and campaign selections are locked** once CAMPAIGN_LOADING begins
4. **Only GAMEPLAY state** can transition to itself (player making choices, scenes regenerating)
5. **SETTINGS can be accessed from most states** without interrupting the parent state
6. **Error states** always have a path back to MAIN_MENU as a fallback

