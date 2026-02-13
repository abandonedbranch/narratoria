# Game Startup and Main Menu - Interactions

## Key Interactions and Decision Points

This document outlines the critical decision points, user inputs, and system responses that define the journey flow.

---

## Primary Decision Points

### Decision 1: Main Menu Action
**User Choice**: Which primary menu button to tap?

**Options**:
1. `New Game` → Initiates campaign selection
2. `Load Game` → Shows saved playthroughs
3. `Options` → Opens settings panel
4. `Exit` → Closes application

**System Response**:
- New Game: Navigate to CAMPAIGN_DISCOVERY state
- Load Game: Scan persistence layer for saved playthroughs; display list
- Options: Slide in settings panel over current view
- Exit: Prompt for confirmation (if unsaved progress); close app

**Accessibility Notes**:
- All buttons must have clear labels and high contrast
- Touch targets ≥44x44 pixels (mobile)
- Keyboard navigation on desktop (arrow keys + Enter)

---

### Decision 2: Campaign Selection
**User Choice**: Which campaign to play?

**Options**:
- All discovered campaigns (built-in, downloaded, cloud-synced)
- Grouped by: Built-in (Official), Downloaded (User Imported), Cloud (if iOS), Resume (saved playthroughs)

**System Response**:
- Tapped campaign entry highlights and expands to detail view
- Backend: Load full campaign manifest and artwork
- Display CAMPAIGN_DETAIL state with extended information

**Player Actions**:
- Scroll through campaign list
- Tap campaign to view details
- Tap campaign title/description to read more
- Swipe through artwork gallery (if available)
- Tap "Back" to return to main menu
- Tap "Select Character" to proceed (with or without content warnings)

**Typical Workflow**:
1. Player sees campaign list with titles, thumbnails, estimated playtime
2. Player taps campaign of interest
3. Campaign detail view slides in with:
   - Full description, artwork gallery
   - **Content warning indicators** (small color-coded badges at top if warnings exist)
   - Player can tap warning badges to expand descriptions if curious
4. Player taps "Select Character" button

**Edge Cases**:
- **No campaigns available**: Display helpful message ("No campaigns found. Download campaigns from...") with link to documentation or campaign marketplace (if implemented)
- **Campaign ingestion failed**: Show error message ("This campaign could not be loaded"); offer Retry or Back options
- **iCloud campaign not downloaded yet**: Show "☁️ Download" button instead of "Select Character"; display progress during download, then enable "Select Character"

**Accessibility Notes**:
- Campaign list must be navigable via keyboard (arrow keys) and screen reader (read title, author, playtime)
- Thumbnails should have alt text describing campaign scene
- Content warning list must be readable and not collapsed by default

---

### Decision 3: Character Selection (Campaign-Filtered)
**User Choice**: Which character template to select for this playthrough?

**Options**:
- Character templates filtered by campaign's `allowed_classes` or `allowed_races` (from Section 6.2.6)
- Example: If campaign is "Medieval Knight's Quest", only Warrior, Knight, Paladin templates shown
- Campaign may restrict: "Only humans allowed" or "No mages in this world"

**System Response**:
- Selected character portrait highlights with visual feedback (shadow, scale, border color)
- "Continue" button becomes active
- Session state captures: `campaign.selected_id`, `player.character_template_id`, `player.base_stats`

**Player Actions**:
- Tap character card to select
- Swipe/scroll to view more characters (if list longer than screen)
- Tap "Back" to return to campaign selection (abandoning campaign choice)

**Important Difference from Old Flow**:
- Character selection is now FILTERED by campaign choice
- Not all campaigns support all character types
- Going back returns to campaign discovery (not character templates), so player can select different campaign

**Accessibility Notes**:
- Character descriptions must be readable (large font option)
- Voice-over support: Read character name, role, stats aloud
- Color should not be the only indicator of selection; use text labels
- Indicate which character archetypes are unavailable due to campaign constraints

---

### Decision 4: Content Warning Acknowledgment
**User Choice**: Do you understand and accept the content warnings?

**Options**:
- `I understand and wish to proceed` (checkbox; some campaigns may require explicit acknowledgment)
- Alternative: Simple acknowledgment dialog ("I understand" button, no checkbox needed)

**System Response**:
- If warning acknowledged: Proceed to CAMPAIGN_LOADING state
- If back button tapped: Return to CAMPAIGN_DISCOVERY state (selections lost)
- If app is closed during warning: Session abandoned; return to MAIN_MENU on next launch

**Player Actions**:
- Read warnings (modal dialog prevents accidental dismissal)
- Check acknowledgment checkbox (if required)
- Tap "Proceed" button
- Or tap "Back" to return to campaign discovery and select different campaign

**Edge Case**:
- **Campaign has no warnings**: Skip CONTENT_WARNINGS state entirely; transition directly to CAMPAIGN_LOADING

**Accessibility Notes**:
- Warning text must be readable (large font option, high contrast)
- Screen reader must announce all warnings
- Do not use color alone to indicate warning severity; use text labels ("Content Warning: Violence", "Content Warning: Psychological Themes")

---

### Decision 5: Campaign Loading Cancellation
**User Choice**: Continue waiting for campaign to load, or cancel and return to campaign selection?

**Options**:
- Wait for campaign to fully load (no interaction needed)
- Tap "Back" or swipe back gesture to cancel loading

**System Response**:
- If waiting: Campaign ingestion, plan generation, and first scene rendering proceed (5-30 seconds)
  - On success: Transition to GAMEPLAY state; narrative and choices display
  - On failure: Error dialog appears ("Campaign could not be loaded"); offer Retry or Return to Campaign Selection
- If cancel: Campaign loading halts; any partial state discarded; return to campaign selection

**Player Actions**:
- Monitor loading progress bar or status message
- Optionally cancel by tapping back button or system back gesture

**System Responsibility**:
- Ensure loading can be safely canceled without corrupting persistence layer
- Clean up any intermediate files from failed loads

**Accessibility Notes**:
- Loading progress should be announced via screen reader ("Loading: 45% complete")
- Estimated time to completion should be displayed (if available)
- Do not use spinning animation alone; include text status message

---

## Secondary Interactions

### Scroll/Navigation
**Context**: Character selection, campaign list, artwork gallery

**Mobile**:
- Vertical swipe to scroll list
- Horizontal swipe to navigate artwork gallery
- Swipe left from screen edge to go back (iOS convention)

**Desktop**:
- Mouse wheel or trackpad to scroll
- Arrow keys to navigate list
- Click card or button to select
- Esc key to go back

---

### Long Interactions (Hold/Press)
**Context**: Character card, campaign entry (potential; TBD)

**Possible Future Interactions**:
- Long-press character to view extended stats
- Long-press campaign to see full description in tooltip
- (Not yet defined in architecture; left for future design)

---

### Settings Adjustments (In-Journey)
**Context**: Character selection, campaign selection, or loading

**Interaction**:
- Option button always accessible (usually top-right corner)
- Tapping opens settings panel (slides in from right, overlaying current view)
- Settings changes take effect immediately (volume, brightness, text size)
- Closing settings returns to previous state (character/campaign selection preserved)

**Example Settings Users Might Adjust**:
- **Text size**: Increase for readability during character/campaign info
- **Volume**: Mute if launching app in quiet environment
- **Theme**: Switch to dark mode for comfortable late-night browsing

---

## System Feedback & Confirmation Messages

### Loading Feedback
- **Model download in progress**: "Downloading AI models... 45%" (shows percentage and file name)
- **Campaign discovery**: "Scanning for campaigns..." (might take 2-5 seconds; show indeterminate spinner)
- **Campaign loading**: "Preparing your story... Step 2 of 4" (shows progress through ingestion, plan generation, etc.)
- **Error feedback**: "Campaign failed to load. Cause: [specific reason, e.g., 'Missing required file: manifest.json']. [Retry] [Return to Selection]"

### Confirmation Dialogs
- **Exit without saving**: "You have unsaved progress. Close anyway?" (only if accessed from gameplay, not main menu)
- **Overwrite in-progress playthrough**: "Resume this campaign or start fresh?" (if returning to previously loaded campaign)

---

## Analytics & Tracking Points

The following interaction points should be tracked (with user consent) to improve UX:

1. **Character selection**: Which characters are most popular?
2. **Campaign selection**: Which campaigns have highest engagement? (Funnel: discovered → selected → started → completed)
3. **Abandonment**: Where do users drop off? (e.g., during model download, campaign loading, or at content warnings?)
4. **Load times**: How long does campaign loading actually take on real devices?
5. **Error encounters**: Which errors are most common? (missing files, failed plan generation, etc.)
6. **Back button usage**: How often do users go back vs. forward?

---

## Known Limitations & Future Enhancements

### Current Limitations
- **Character templates not yet defined**: Campaign selection currently uses predefined system templates (spec TBD)
- **No campaign preview/demo**: Users cannot sample a campaign before fully committing to load
- **No campaign ratings/reviews**: No user feedback mechanism visible at campaign selection stage
- **No search/filter**: Campaign list is un-sortable in current spec; may become unwieldy if 100+ campaigns available
- **Character constraints not shown intuitively**: When a campaign restricts available characters, players might not understand why certain options are unavailable

### Potential Future Enhancements
- **Campaign preview scene**: Play first 30 seconds of campaign to sample narrative style
- **Campaign ratings widget**: Show user scores or "trending" badge
- **Search & filter**: "Search by genre", "Show only short campaigns (<1 hour)"
- **Campaign marketplace**: Browse and download campaigns from community hub
- **Character constraint tooltips**: Hover/tap to see why an archetype is unavailable ("This campaign only allows magic users")
- **Difficulty selection**: "Easy / Normal / Hard" difficulty modes (if campaign supports them)
- **Suggested character pairings**: "This campaign was designed for Warrior + Healer combinations" (for future multiplayer)

