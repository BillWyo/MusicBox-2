# MusicBox 2.0 Architecture

## Vision

Clean VR music browser for Quest 3S that:
- Discovers and browses 500+ album library via UPnP/DLNA
- Manages playlists with full CRUD operations
- Syncs state to Node-RED for external control/monitoring
- Runs entirely on C drive (no D/C split)
- Uses GitHub as source of truth

**v1 Lessons Applied:**
- Left trigger for mode switching (not A button)
- Rolodex UI proven scalable for 512 albums
- MQTT→Node-RED pipeline working reliably
- Input blocking prevents cross-UI interference
- Single-drive deployment eliminates I/O corruption

---

## Module Architecture

```
MusicBox/
├── Core/
│   ├── NetworkManager          (UPnP discovery, session lifecycle)
│   ├── PlaylistManager         (JSON persistence, CRUD)
│   └── AudioController         (Playback state, volume)
│
├── UI/ (Generic Controllers + Data Sources)
│   ├── ModeController          (Browse/Create/Review state machine)
│   ├── RolodexController       (Generic carousel: albums or playlists)
│   ├── ListController          (Generic vertical list: tracks or items)
│   └── NavigationUI            (Mode toggle buttons, always visible)
│
├── Data/
│   ├── DataSources/
│   │   ├── ITileDataSource     (Interface: carousel items)
│   │   │   ├── AlbumDataSource
│   │   │   └── PlaylistDataSource
│   │   └── IListDataSource     (Interface: list items)
│   │       ├── TrackListDataSource (read-only for browsing)
│   │       └── EditableTrackListDataSource (deletable for review)
│   └── Models/
│       ├── Playlist            (name, tracks[])
│       ├── Track               (title, artist, album, uri)
│       └── Album               (title, artist, art, tracks[])
│
├── Input/
│   ├── XRInputManager          (Controller polling, mapping)
│   └── InputRouter             (Distribute input by mode/focus)
│
└── Network/
    ├── MQTTManager             (Broker connection, publish/subscribe)
    ├── UPnPManager             (Device discovery, ContentDirectory queries)
    └── PlaybackController      (Send play/pause/next to renderer)
```

**Key Design Pattern: Data Source Abstraction**

Instead of separate `AlbumRolodex` + `PlaylistRolodex`, use:
- **One `RolodexController`** that takes any `ITileDataSource`
  - Same carousel logic, scrolling, positioning, highlighting
  - Swaps data source (AlbumDataSource ↔ PlaylistDataSource)
  - Same for ListController with IListDataSource implementations

**Key Principle:** Managers are **singletons** with clear ownership:
- NetworkManager owns all UPnP state
- PlaylistManager owns all playlist state
- MQTTManager owns all broker communication
- ModeController owns all UI visibility/state
- RolodexController is stateless (data from source)
- ListController is stateless (data from source)

---

## Data Flow

### 1. Startup
```
App Launch
  ↓
XRRig + Controllers initialized
  ↓
ModeController: Start in Browse mode
  ↓
NetworkManager: Discover UPnP devices (async)
  ↓
PlaylistManager: Load playlists from disk (async)
  ↓
AlbumRolodex: Populate when discovery complete
PlaylsitRolodex: Populate when playlist load complete
```

### 2a. Browse Mode → Create via N Key (New Playlist)
```
User: Press N key (in Browse mode)
  ↓
Auto-generate playlist name (random bird name)
  ↓
AlbumRolodex + PlaylistRolodex fade to background
  ↓
Layout: Side-by-side cards appear front/center
  ┌─────────────────────┐  ┌──────────────────┐
  │   ListPanel (left)  │  │ Playlist Card    │
  │  Album's tracks:    │  │ (right - NEW)    │
  │  ☐ Track 1          │  │ [Auto-named]     │
  │  ☐ Track 2          │  │ Tracks added: 0  │
  │  ☐ Track 3          │  │                  │
  └─────────────────────┘  └──────────────────┘
  ↓
User: Right Joystick Y (scroll tracks), Right Trigger to add each track
  → Track highlights in ListPanel (center row, yellow)
  → Selected tracks accumulate in Playlist Card (real-time feedback)
  → Counter updates: "Tracks added: 2"
  ↓
When done selecting tracks:
  → X key (Save) to commit playlist to disk
  → PlaylistManager.SavePlaylist(newPlaylist)
  → MQTT: Publish "playlist.created"
  → Both cards fade out, PlaylistPanel resets to blank
  ↓
Right A button (Back) to cancel:
  → Discard unsaved tracks
  → Both cards fade out
  → Unsaved playlist is NOT discarded (persists in EditablePlaylistDataSource)
  ↓
Return to Browse mode: AlbumRolodex comes forward
  → Can select different album and continue adding to same unsaved playlist
  → Can press N again to start fresh blank playlist
```

### 2b. Browse Mode → Create via Existing Playlist Selection
```
User: Left Joystick X (scroll albums), Right Trigger to select album
  ↓
Album selected → AlbumRolodex + PlaylistRolodex fade to background
  ↓
Layout: Side-by-side cards appear front/center
  ┌─────────────────────┐  ┌──────────────────┐
  │   ListPanel (left)  │  │ Playlist Card    │
  │  Album's tracks:    │  │ (right - EXIST)  │
  │  ☐ Track 1          │  │ [Selected name]  │
  │  ☐ Track 2          │  │ Tracks: 5        │
  │  ☐ Track 3          │  │                  │
  └─────────────────────┘  └──────────────────┘
  ↓
User: Right Joystick Y (scroll tracks), Right Trigger to add selected track
  → Right Trigger adds track to existing playlist (no duplicates)
  → Selected tracks accumulate in Playlist Card
  → Track count updates
  ↓
Left X button (Save) to commit:
  → PlaylistManager.SavePlaylist(updatedPlaylist)
  → MQTT: Publish "playlist.modified"
  → Both cards fade out
  ↓
Right A button (Back) to cancel:
  → Discard pending additions
  → Existing playlist unchanged
  → Both cards fade out
  ↓
Return to Browse mode: AlbumRolodex comes forward
```

### 3. Browse Mode → Review (Toggle mode)
```
User: Left Trigger (or Spacebar)
  ↓
ModeController: Toggle Browse → Review
  ↓
AlbumRolodex fades to background
  ↓
PlaylistRolodex comes forward (focused)
  Shows: [Playlist 1] [Playlist 2] [Playlist 3] ...
  Note: ONLY saved playlists (no blank card)
  To create new: return to Browse, press N key
  ↓
User: Left Joystick X (scroll playlists), Right Trigger to select
  ↓
Playlist selected → PlaylistRolodex fades to background
  ↓
ListPanel (track edit card) comes forward (centered)
  Shows: Playlist's tracklist (with delete capability)
  ┌────────────────────────┐
  │  "My Playlist" (5)     │
  │  ☑ Track 1 (delete)    │
  │  ☑ Track 2 (delete)    │
  │  ☑ Track 3 (delete)    │
  └────────────────────────┘
  ↓
User: Right Joystick Y (scroll tracks), Right Trigger to delete selected track
  → Track removed from playlist
  → ListController re-centers selection on next track
  → If last track deleted: ListPanel closes, return to PlaylistRolodex
  → PlaylistManager.SavePlaylist() immediately (auto-save on delete)
  → MQTT: Publish "playlist.modified"
  ↓
Left X button to confirm deletions (or just closes ListPanel)
Right A button to close ListPanel (discards unsaved changes if any)
  ↓
ListPanel fades out
  ↓
PlaylistRolodex comes forward (Review state)
  ↓
User: Left Trigger to return to Browse mode
```

---

## Scene Structure (Single Scene)

```
MusicBox (Master Scene)
│
├── XRRig (Camera + Controllers)
│   ├── LeftController (XR Input)
│   └── RightController (XR Input)
│   │
│   └── UIContainer
│       ├── NavigationUI (Status display, always visible)
│       │   └── ModeText (TextMeshPro: "BROWSE ALBUMS" or "REVIEW PLAYLISTS")
│       │
│       ├── RolodexContainer (Shared rotation point for both rolodexes)
│       │   ├── AlbumRolodex (lower height, visible in Browse mode)
│       │   │   └── 9 tile instances
│       │   │
│       │   └── PlaylistRolodex (upper height, visible in Review mode)
│       │       └── 6 tile instances
│       │
│       └── ListPanel (Visible in Review/Create modes when expanded)
│           └── ListController instance
│               └── 6 row instances
│
└── Managers (Root level, persist across modes)
    ├── ModeController (Singleton)
    ├── NetworkManager (Singleton)
    ├── PlaylistManager (Singleton)
    ├── MQTTManager (Singleton)
    └── XRInputManager (Singleton)
```

**Why single scene?**
- Eliminates scene load delays
- Preserves singleton state (managers stay in memory)
- Simpler XR session management
- Easier to debug mode switching
- All UI stays relative to player head (child of XRRig)

**Two-Mode Design:**
- **Browse**: AlbumRolodex visible, PlaylistRolodex hidden, ListPanel hidden
- **Review**: PlaylistRolodex visible, AlbumRolodex hidden, ListPanel expands on playlist select
- **Create**: (embedded in Browse) ListPanel pops up with track-add mode

---

## UI Layering & Focus Management

**Layering Strategy:** Depth-based focus (Z-position + Alpha fade)

**Browse Mode (Default State):**
```
Front (Focused):  AlbumRolodex (opaque, interactive)
                  PlaylistRolodex (behind, semi-transparent)
Back (Hidden):    ListPanel (inactive)
```

**Browse → Album Selected (Track Selection):**
```
Front (Focused):  ┌─────────────┐  ┌──────────────┐
                  │ ListPanel   │  │ Playlist     │
                  │ (left)      │  │ Card (right) │
                  │ Tracks      │  │ Real-time    │
                  └─────────────┘  │ feedback     │
                                   └──────────────┘
Back (Faded):     AlbumRolodex + PlaylistRolodex (50% alpha)
```

**Browse → Album Selected → Choose Target (older flow, now via N key):**
```
Front (Focused):  PlaylistRolodex (carousel, pick target) [DEPRECATED]
                  ↓ (Left Joystick X to scroll)
                  └─ Existing Playlist centered = append
Back (Faded):     ListPanel + AlbumRolodex (25% alpha)

NEW (N key): Press N to create blank instead of browsing carousel
```

**Browse → Album Selected → Save/Cancel:**
```
Action:           Left X (Save) or Right A (Back)
                  ↓
                  Both cards fade out (alpha → 0)
                  ListPanel + PlaylistRolodex + AlbumRolodex return to front
                  ↓
Return to Browse: AlbumRolodex (front), PlaylistRolodex (back)
```

**Review Mode (Default State):**
```
Front (Focused):  PlaylistRolodex (opaque, interactive)
                  Shows: ONLY saved playlists (no blank card)
                  AlbumRolodex (hidden)
Back (Hidden):    ListPanel (inactive)
```

**Review → Playlist Selected (Track Edit):**
```
Front (Focused):  ListPanel (track list, centered)
Back (Faded):     PlaylistRolodex (50% alpha, visible for context)
```

**Review → Playlist Selected → Save/Cancel:**
```
Action:           Left X (Save) or Right A (Back)
                  ↓
                  ListPanel fades out
                  ↓
Return to Review: PlaylistRolodex (front)
```

**Key Principles:**
1. **Only one card "front/focused" at a time** (opaque, interactive)
2. **Rolodexes fade to background when cards appear** (prevents input interference)
3. **Alpha transitions smooth the focus shift** (visual clarity, natural UX)
4. **Z-position keeps focus order consistent** (prevents accidentally clicking behind)
5. **Side-by-side layout (Create mode)** provides real-time feedback without modal dialog

---

## List Panel Operations

### Panel Types

**PlaylistPanel (Editable Playlist):**
- Shows currently-editing playlist
- Displays: Playlist name (title), track count, 6 visible tracks
- Each track shows: Track# | Title | Artist | Album
- Selection model: One track always highlighted (center row, yellow/bright)
- Operations: addTrack, deleteTrack, moveTrackUp, moveTrackDown

**AlbumListPanel (Browse Mode Tracks):**
- Shows selected album's track list (read-only for browsing)
- Displays: Album name (title), track count, 6 visible tracks
- Each track shows: Track# | Title | Artist
- Selection model: One track always highlighted (center row, yellow/bright)
- Operations: addTrack (to playlist), no delete in browse mode

### List Operations

**addTrack(track) — Append Track**
- Adds track to end of list if not already present
- If track already in list: no duplicate, list unchanged
- After add: newly-added track becomes selectedItem
- Feedback: Track count updates, list scrolls to show new track if needed

**deleteTrack(itemIndex) — Remove Track**
- Removes track at itemIndex from list
- Remaining tracks shift up (move up one position)
- After delete: 
  - If deleted item was NOT last: selectedItem = item at same index in shortened list
  - If deleted item WAS last: selectedItem = new last item in list
- If list becomes empty after deletion: ListPanel closes, return to rolodex
- Feedback: Track count updates, selection re-centered

**moveTrackUp(itemIndex) — Reorder**
- Moves track at itemIndex up one position (swap with itemIndex-1)
- Fails silently if itemIndex == 0 (already at top)
- After move: movedTrack remains selectedItem
- Feedback: Visual reflow of list items

**moveTrackDown(itemIndex) — Reorder**
- Moves track at itemIndex down one position (swap with itemIndex+1)
- Fails silently if itemIndex == last (already at bottom)
- After move: movedTrack remains selectedItem
- Feedback: Visual reflow of list items

### Selection Model

**SelectedItem (Within a ListPanel):**
- The currently-highlighted track (center row, yellow/bright color)
- Always exactly one selected item when list is open
- Updated after every operation: add, delete, moveUp, moveDown
- Scroll with Right Joystick Y (up/down) to change selection
- Press Right Trigger to perform action (add/delete/confirm)

**SelectedAlbum (Browse Mode Carousel):**
- The centered album tile in AlbumRolodex
- Updated when Left Joystick X scrolls carousel
- Press Right Trigger to select → shows album tracks in ListPanel

**SelectedPlaylist (Review Mode Carousel):**
- The centered playlist tile in PlaylistRolodex
- Updated when Left Joystick X scrolls carousel
- Press Right Trigger to select → shows playlist tracks in ListPanel (edit mode)

### Unsaved Playlist Persistence

**Key Rule:** A playlist that has tracks added but NOT saved persists across album selections.

**Workflow:**
1. User in Browse mode, selects Album A → shows AlbumListPanel (read-only) + PlaylistPanel (blank)
2. User adds tracks from Album A → PlaylistPanel now shows 3 tracks (not saved yet)
3. User presses B (back) → returns to Browse mode, tracks still in memory
4. User selects Album B → shows AlbumListPanel (Album B tracks) + PlaylistPanel (still shows Album A's 3 tracks!)
5. User can continue adding tracks from Album B to same playlist
6. When user saves (X button) → playlist is written to disk
7. After save → PlaylistPanel resets to blank "new playlist" for next creation

**Benefit:** Users can build cross-album playlists without saving between each album selection.

**Limitation:** If user doesn't save before quitting → unsaved playlist is lost.

### N-Key Workflow (Complete Flow)

```
Browse Mode (AlbumRolodex centered)
  ↓
Press N Key
  ↓
Generate random bird name: "Wren's Mix"
  ↓
Fade AlbumRolodex to background (50% alpha)
  ↓
Show side-by-side:
  ┌─────────────────────┐  ┌──────────────────┐
  │   AlbumListPanel    │  │  PlaylistPanel   │
  │  (current album)    │  │ [Wren's Mix]     │
  │  Tracks: 0/8        │  │ Tracks: 0        │
  │                     │  │                  │
  │  ☑ Track 1          │  │                  │
  │  ☐ Track 2 (sel)    │  │                  │
  │  ☐ Track 3          │  │                  │
  └─────────────────────┘  └──────────────────┘
  ↓
User adds tracks (Right Trigger on AlbumListPanel)
  │ Track 2 → PlaylistPanel (added)
  │ Scroll to Track 5
  │ Track 5 → PlaylistPanel (added)
  ↓
PlaylistPanel now shows:
  Wren's Mix
  Tracks: 2
  ☑ Track 2
  ☑ Track 5
  ↓
User Press X (Save)
  │ PlaylistManager.SavePlaylist("Wren's Mix", [Track2, Track5])
  │ MQTT.Publish("playlist.created", ...)
  │ Both panels fade out
  ↓
PlaylistPanel resets to blank, ready for next N press
  ↓
Return to Browse: AlbumRolodex comes forward
  ↓
User can:
  • Press N again for new blank playlist
  • Select different album (or same album) and press N
  • Press Enter to add to EXISTING saved playlist
  • Press Spacebar to switch to Review mode
```

---

## Playlist Concept

**Creating New Playlists:**
- In Browse mode: Press "N" key → blank playlist opens (auto-generated bird name)
- In Review mode: NOT available (Review is edit-only mode)
- Blank playlists do NOT appear in PlaylistRolodex (use N key instead)
- Once playlist has ≥ 1 track: user can save (X button) or discard (B button)

**Existing Playlists (in PlaylistRolodex):**
- Appear as carousel tiles (in order of load from disk)
- In Browse mode: can append selected tracks to any existing playlist
- In Review mode: can view/edit existing playlist (delete tracks, rename)

**No Blank Card Policy:**
- PlaylistRolodex contains ONLY saved playlists (no dummy "create new" card)
- To create new playlist in Browse: Use N key
- To create new playlist in Review: Not possible (go to Browse, use N key)

**Logic:**
- Right Trigger on playlist in Browse → Enter Create mode, append tracks
- Right Trigger on playlist in Review → Enter Edit mode, delete/reorder tracks
- N key in Browse → Create mode with fresh blank playlist

---

## Input Mapping (Quest 3S Touch Plus)

### Left Controller
| Input | Action | Mode |
|-------|--------|------|
| Trigger | Toggle Browse ↔ Review | All |
| Joystick X | Scroll carousel left/right | Browse/Review/Create |
| X button | Save (playlist, changes) | Create/Review |
| A button | Back / Close panel | Create/Review |

### Right Controller
| Input | Action | Mode |
|-------|--------|------|
| Trigger | Select item (album/playlist/track) | Browse/Create/Review |
| Joystick Y | Scroll track list up/down | Review/Create (list open) |

**Key Design:**
- **Left trigger for mode toggle** (proven working in v1, natural pointing gesture)
- **Right trigger for selection** (natural point-and-shoot gesture, avoids A-button conflicts)
- **Joystick isolation** (left=horizontal carousel, right=vertical track list) minimizes spurious signals
- **Explicit Save/Back** (left X and A buttons, clear intentionality)
- **Right joystick utilized** for track list scrolling (vertical separation from carousel)

### Editor Keyboard Equivalents

| Quest 3S | Editor Key | Action |
|----------|-----------|--------|
| Left Trigger | Spacebar | Toggle Browse ↔ Review |
| Left Joystick X (left) | A | Scroll carousel/list left |
| Left Joystick X (right) | D | Scroll carousel/list right |
| Left X button | X | Save (playlist, changes) |
| Left Y button | Y | Album Context Menu (sort, filter) |
| Right Trigger | Enter | Select album/playlist/track |
| Right Joystick Y (up) | W | Scroll track list up |
| Right Joystick Y (down) | S | Scroll track list down |
| Right A button | B | Back / Close panel |
| Right B button | Backspace | Playlist Context Menu (delete, reorder) |
| **N Key (Browse only)** | **N** | **Create blank playlist (new workflow)** |

**New in v2:** N key creates fresh blank playlist in Browse mode (no blank card in PlaylistRolodex)

---

## Mode State Machine

```
┌─────────────────────────────────────────────────────────────────┐
│                      ModeController                              │
│              (Toggled by Left Trigger / Spacebar)                │
└─────────────────────────────────────────────────────────────────┘

Browse (Default)
├─ Shows: AlbumRolodex (lower), NavigationUI ("BROWSE ALBUMS")
├─ Input: Left Joystick X (scroll albums), Right Trigger (select album)
│         N Key (create blank playlist)
│         Left Trigger (mode toggle to Review)
├─ OnEnter: 
│   - AlbumRolodex visible
│   - PlaylistRolodex hidden
│   - ListPanel hidden
│   - ModeText = "BROWSE ALBUMS"
│   - Restore unsaved playlist if it exists
└─ OnExit: Close ListPanel if open (before mode switch)

Browse → Create (Album Selected)
├─ Shows: AlbumListPanel (left, track list) + PlaylistPanel (right, target)
├─ Input: Right Joystick Y (scroll tracks), Right Trigger (add selected track)
│         Left Joystick X (scroll PlaylistRolodex at background)
│         X Key (save playlist), B Key (back to Browse)
├─ Behavior:
│   - Album tracks shown on left (read-only list)
│   - Target playlist shown on right (can be existing or new)
│   - Selected track in AlbumListPanel highlighted (yellow)
│   - Right Trigger adds selected track to PlaylistPanel
│   - Unsaved playlist persists when user presses B
│   - X Key saves playlist and resets to blank for next creation
└─ Return to Browse: Pressing B returns to AlbumRolodex (unsaved tracks preserved)

Browse → Create via N Key
├─ Shows: AlbumListPanel (left) + Blank PlaylistPanel (right, auto-named)
├─ Behavior:
│   - N key opens blank playlist with auto-generated bird name
│   - User can add tracks from currently-selected or nearby albums
│   - Same save/back flow as standard Create mode
└─ Return to Browse: Same as above

Review (Toggled via Left Trigger)
├─ Shows: PlaylistRolodex (upper), NavigationUI ("REVIEW PLAYLISTS")
├─ Input: Left Joystick X (scroll playlists), Right Trigger (select playlist)
│         Left Trigger (mode toggle to Browse)
├─ OnEnter:
│   - PlaylistRolodex visible (contains ONLY saved playlists, NO blank card)
│   - AlbumRolodex hidden
│   - ListPanel hidden (expands on playlist select)
│   - ModeText = "REVIEW PLAYLISTS"
│   - Refresh playlists from PlaylistManager (discard unsaved edits)
└─ OnExit: Close ListPanel if open, save any pending deletions

Review → Edit (Playlist Selected)
├─ Shows: PlaylistPanel (edit mode, center screen)
├─ Input: Right Joystick Y (scroll tracks), Right Trigger (delete selected track)
│         X Key (save changes), B Key (close without saving)
├─ Behavior:
│   - Playlist tracks shown as editable list
│   - Selected track highlighted (yellow)
│   - Right Trigger DELETES selected track
│   - Tracks automatically shift up after deletion
│   - If last track deleted → playlist closes, return to PlaylistRolodex
│   - X Key confirms deletions, B Key discards changes
└─ Return to Review: Pressing B returns to PlaylistRolodex

State Safety:
- Only Browse or Review active at a time
- Mode switch via left trigger only (no accidental UI clicks)
- ListPanel visibility independent of mode (can appear/disappear in either)
- Input handler respects current mode and ListPanel visibility
- Unsaved playlists in Browse mode persist across album selections
- Saved playlists in Review mode do not persist unsaved edits (reload on mode enter)
```

---

---

## Context Menus

**Album Context Menu (Left Y Button)**

Triggered in Browse mode to access sorting and filtering options.

```
Position:  Centered on screen
Display:   Vertical list overlay, center item highlighted
Scroll:    Right Joystick Y (up/down)
Select:    Right Trigger to execute option
Close:     Right B button or timeout (auto-close after selection)
```

**Menu Options:**
- Sort by Album Name (default, ✓ indicator)
- Sort by Artist Name
- (Reserved for: Genre, Year, Recently Added, etc.)

**Behavior:**
- Opens over current view without disrupting carousel
- Center-selected option highlighted
- Selection applies sort immediately, playlist refreshes
- Menu closes after selection or button press

---

**Playlist Context Menu (Right B Button)**

Triggered in Review or Create mode to manage playlist operations.

```
Position:  Upper-left of PlaylistRolodex card
           (Anchored to visible card, not screen-centered)
Display:   Vertical list overlay, center item highlighted
Scroll:    Right Joystick Y (up/down)
Select:    Right Trigger to execute option
Close:     Right B button or timeout (auto-close after selection)
```

**Menu Options:**
- Delete All Tracks (confirm prompt)
- Reorder Tracks (?)
- (Reserved for: Rename, Export, Duplicate, etc.)

**Behavior:**
- Opens near PlaylistRolodex to show contextual relationship
- Center-selected option highlighted
- Destructive actions (Delete All) prompt for confirmation
- Menu closes after selection or button press
- Changes saved immediately to PlaylistManager

---

## UI Components

### 1. AlbumRolodex (Browse Mode)
```
Displays:  9 visible tiles in arc (center = selected)
Scroll:    Left Joystick X-axis
Interaction: Right Trigger to select
Behavior:  Animate selected album to sides when Right Trigger pressed
Load:      Album art from network (cached locally)
```

**v1 Fix Applied:** Works with 512 albums, smooth 60 FPS

### 2. PlaylistRolodex (Review Mode)
```
Displays:  6 visible tiles in arc (center = selected)
Contents:  ONLY saved playlists (no blank card)
Scroll:    Left Joystick X-axis (blocked when TrackList open)
Interaction: Right Trigger to select → show tracks in edit mode
Behavior:  Show track list when Right Trigger pressed
Load:      Trigger ListController.Show() with edit mode enabled
Note:      To create new playlist, return to Browse and press N key
```

**v1 Fix Applied:** Input blocking prevents playlist rotation while viewing tracks
**v2 Update:** Blank card removed; use N key in Browse mode for new playlists

### 3. ListController (Track Selection & Track List)

**In Create Mode (Browse → Album Selected):**
```
Position:  Left side of screen (side-by-side with Playlist Card)
Displays:  6 visible rows (center row = selected/highlighted)
Scroll:    Right Joystick Y-axis (up/down)
Selection: Center row always selected
Display:   Checkbox | Title | Artist
Interact:  Right Trigger = toggle track selection
Behavior:  Selected tracks accumulate, highlight in different color
Feedback:  Playlist Card on right updates in real-time with count
```

**In Review Mode (Playlist Selected):**
```
Position:  Center of screen
Displays:  6 visible rows (center row = selected/highlighted)
Scroll:    Right Joystick Y-axis (up/down)
Selection: Center row always selected
Display:   Track# | Title | Artist
Interact:  Right Trigger = delete selected track
Behavior:  Track removed immediately, PlaylistManager saves
Feedback:  Track count updates, re-center selection on remaining tracks
```

**Shared Controls:**
- Left X button: Save (Create: commit playlist, Review: confirm deletions)
- Right A button: Back/Close panel
- Input blocking prevents carousel rotation when list active

**v1 Fix Applied:**
- Artist display for multi-album playlists
- Scroll limits calculated correctly: maxOffset = max(0, totalTracks - visibleRows)
- Input blocking prevents album rotation and carousel interference

### 3. NavigationUI (Status Display, Always Visible)
```
Displays:  Mode name + current selection position
           Browse mode:   "BROWSE ALBUMS | Album 5 of 512"
           Review mode:   "REVIEW PLAYLISTS | Playlist 2 of 5"
           
           Format: "{Mode} | {Item} {index} of {total}"
           
Updates:   - When left trigger pressed (mode change)
           - When carousel selection changes (index change)
           
Interact:  No clickable elements (read-only status)

Behavior:  Shows which mode you're in + current position
           Provides visual feedback during carousel scrolling
```

**Data Sources:**
- Mode: ModeController.CurrentMode
- Index: RolodexController.CurrentIndex (new event)
- Total: RolodexController.DataSource.Count

**Design Rationale:**
- Purely informational, not interactive
- Reduces UI complexity in VR
- Position display aids navigation in large collections
- Real-time feedback during scrolling
- No accidental UI interactions

---

## MQTT Integration (Node-RED)

### Topics Published (MusicBox → Node-RED)

```
musicbox/mode
  Payload: "browse" | "review" | "create"
  Frequency: On mode change

musicbox/album/current
  Payload: { "title": "...", "artist": "...", "index": 0 }
  Frequency: On album selection

musicbox/playlist/list
  Payload: [{ "name": "...", "trackCount": 0 }, ...]
  Frequency: On Review mode entry, after playlist changes

musicbox/playlist/current
  Payload: { "name": "...", "tracks": [{ "title": "...", "artist": "...", "uri": "..." }] }
  Frequency: On playlist selection

musicbox/playback/state
  Payload: { "playing": true, "track": "...", "position": 0 }
  Frequency: On play/pause/skip
```

### Topics Subscribed (Node-RED → MusicBox)

```
musicbox/command/play
  Payload: 1
  Behavior: Play current track

musicbox/command/pause
  Payload: 1
  Behavior: Pause playback

musicbox/command/next
  Payload: 1
  Behavior: Skip to next track

musicbox/command/mode
  Payload: "browse" | "review" | "create"
  Behavior: Switch mode
```

**v1 Validated:** MQTT pipeline working reliably, low latency

---

## Network Architecture

### UPnP Discovery
```
NetworkManager (on app start):
  1. Send M-SEARCH for ssdp:all (3-second timeout)
  2. Collect device descriptions
  3. Filter for MediaServers (UPnP device type)
  4. For each MediaServer:
     - Call Browse("0") → Get root items
     - Call Browse("0$albums") → Get album list (paginated, 200 at a time)
  5. When complete, fire OnAlbumsLoaded event
  6. AlbumRolodex populates from event
```

**Threading:** UPnP queries run in background coroutines (don't block UI)

### Playlist Persistence
```
PlaylistManager:
  Location: C:\Users\johan\AppData\Local\MusicBox\playlists\
  Format: JSON (one file per playlist)
  Load: On app start, async
  Save: After each modification (immediate)
  Backup: Git commit + MQTT publish (external logging)
```

**File Format:**
```json
{
  "name": "10,000 Days",
  "created": "2026-06-24T10:30:00Z",
  "tracks": [
    {
      "title": "Jambi",
      "artist": "Tool",
      "album": "10,000 Days",
      "uri": "http://192.168.1.18:9790/..."
    }
  ]
}
```

---

## Implementation Phases

### Phase 1: Foundation (Core + Browse + Create)
**Browse mode with integrated playlist creation**

Core:
- [ ] XRRig + Input system (left trigger, A/B buttons, joystick)
- [ ] NetworkManager (UPnP discovery, album list)
- [ ] PlaylistManager (load/save JSON, create new playlists)
- [ ] ModeController + NavigationUI (status display)
- [ ] XRInputManager (controller polling, input routing)

Browse UI:
- [ ] RolodexController (generic carousel)
- [ ] AlbumDataSource (album art loading, caching)
- [ ] ListController (generic track list)
- [ ] TrackListDataSource (per-track selection, highlight)

Create Flow (embedded in Browse):
- [ ] PlaylistRolodex with blank card (first item)
- [ ] Random bird name generator for new playlists
- [ ] Playlist scroll-down on track add
- [ ] Fade transitions (ListPanel + PlaylistRolodex)

Tests:
- [ ] Browse 512 albums, smooth 60 FPS
- [ ] Select album, see tracks, select per-track
- [ ] Target blank card, create new playlist
- [ ] Target existing playlist, add to it
- [ ] Verify new playlist saved and populated

**GitHub:** Commit "Phase 1: Browse + Create working"

### Phase 2: Review (Playlist Management)
- [ ] PlaylistRolodex (Review mode, no blank card)
- [ ] ListController in edit mode (delete tracks)
- [ ] Input blocking (joystick Y blocked during list scroll)
- [ ] Playlist scroll-down on track deletion
- [ ] Track deletion with re-centering

Tests:
- [ ] Load playlists in Review mode
- [ ] Select playlist, see tracks
- [ ] Delete tracks, playlist updates
- [ ] B button closes track list

**GitHub:** Commit "Phase 2: Review mode working"

### Phase 3: Polish (MQTT, Optimization, Headset Testing)
- [ ] MQTT integration (publish all playlist events)
- [ ] Playback controller (send play/pause to renderer)
- [ ] Error handling (network timeouts, file I/O)
- [ ] Performance optimization (frame rate stability, memory profiling)
- [ ] Test full flow on headset (Browse→Create→Review→Browse loop)
- [ ] Validate audio playback integration

**GitHub:** Commit "Phase 3: Polish and MQTT ready"

### Phase 4: Deployment
- [ ] Build APK for Quest 3S
- [ ] Final headset testing
- [ ] GitHub release v2.0.0
- [ ] Document secrets.h setup and deployment instructions

---

## Known Constraints

1. **Scroll Limits:** With visibleRows=6, can only select tracks that fit pattern: offset ∈ [0, maxOffset] where maxOffset = max(0, totalTracks - visibleRows)
   - Solution: Accept this UX, don't scroll for playlists < 6 tracks

2. **Stereo Rendering:** VR laser pointer caused double-dot artifact on Quest 3S
   - Solution: Use controller input (left trigger) instead, avoid laser pointer

3. **Input Blocking:** Joystick up/down in track list must not scroll playlists
   - Solution: Check if TrackListController.Canvas is active before responding to input

4. **Mode Switching:** Pressing A button while browsing would toggle mode AND select item
   - Solution: Use left trigger exclusively for mode toggle, A button only for selection

---

## Testing Checklist

- [ ] Browse: Scroll through 512 albums, 60 FPS stable
- [ ] Browse: A button selects album, shows NowPlaying card
- [ ] Review: Left trigger switches Browse → Review
- [ ] Review: Playlists load and display
- [ ] Review: Select playlist → track list appears
- [ ] Review: Up/Down arrows scroll track list, center track highlighted
- [ ] Review: A button deletes selected track
- [ ] Review: B button closes track list
- [ ] Create: Select album in Browse, enter Create mode
- [ ] Create: A button adds tracks, card updates
- [ ] Create: B button saves playlist
- [ ] MQTT: All topics publish correctly
- [ ] Headset: Build APK, deploy to Quest 3S, test full flow

---

## File Reference

**Config Files:**
- `Assets/Resources/secrets.h.example` → Copy to `secrets.h` and fill in:
  ```
  const string MQTT_BROKER = "192.168.1.x";
  const int MQTT_PORT = 1883;
  ```

**Key Scripts (to implement):**
- Core/NetworkManager.cs
- Core/PlaylistManager.cs
- UI/ModeController.cs
- UI/RolodexController.cs
- UI/PlaylistTrackListController.cs
- Input/XRInputManager.cs
- Network/MQTTManager.cs

---

**Status:** Architecture ready for Phase 1 implementation
**Next:** Initialize Unity project and create folder structure
