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

### 2. Browse Mode → Create (Album Selection & Track Selection)
```
User: Left Joystick X (scroll albums), Right Trigger to select
  ↓
Album selected
  ↓
AlbumRolodex + PlaylistRolodex fade to background
  ↓
Layout: Side-by-side cards appear front/center
  ┌─────────────────────┐  ┌──────────────────┐
  │   ListPanel (left)  │  │ Playlist Card    │
  │  Album's tracks:    │  │ (right - target) │
  │  ☐ Track 1          │  │ Blank or Exist   │
  │  ☐ Track 2          │  │ Tracks added: 0  │
  │  ☐ Track 3          │  │                  │
  └─────────────────────┘  └──────────────────┘
  ↓
User: Right Joystick Y (scroll tracks), Right Trigger to select each track
  → Track highlights in ListPanel
  → Selected tracks accumulate in Playlist Card (real-time feedback)
  → Counter updates: "Tracks added: 2"
  ↓
When done selecting tracks:
  → User can scroll PlaylistRolodex with Left Joystick X to pick target
  → Blank Card centered = create new playlist
  → Existing Playlist centered = append to that playlist
  ↓
Left X button (Save) to commit:
  → PlaylistManager.SavePlaylist() (new or updated)
  → MQTT: Publish "playlist.created" or "playlist.modified"
  → Both cards fade out
  ↓
Right A button (Back) to cancel:
  → Discard selected tracks
  → Both cards fade out
  ↓
Return to Browse mode: AlbumRolodex + PlaylistRolodex come forward
```

### 3. Browse Mode → Review (Toggle mode)
```
User: Left Trigger
  ↓
ModeController: Toggle Browse → Review
  ↓
AlbumRolodex + PlaylistRolodex fade/move to background
  ↓
PlaylistRolodex comes forward (focused)
  Shows: [BLANK CARD] [Playlist 1] [Playlist 2] ...
  Note: Blank card present for creating new playlists in Review mode
  ↓
User: Left Joystick X (scroll playlists), Right Trigger to select
  ↓
Playlist selected
  ↓
PlaylistRolodex fades to background
  ↓
ListPanel (track edit card) comes forward (centered)
  Shows: Playlist's tracklist (with delete capability)
  ┌────────────────────────┐
  │  "My Playlist"         │
  │  ☑ Track 1 (deletable) │
  │  ☑ Track 2 (deletable) │
  │  ☑ Track 3 (deletable) │
  └────────────────────────┘
  ↓
User: Right Joystick Y (scroll tracks), Right Trigger to delete selected track
  → Track removed from playlist
  → PlaylistManager.SavePlaylist() immediately
  → MQTT: Publish "playlist.modified"
  ↓
Left X button to save (confirm changes), Right A button to close ListPanel
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

**Browse → Album Selected → Playlist Selection:**
```
Front (Focused):  PlaylistRolodex (carousel, pick target)
                  ↓ (Left Joystick X to scroll)
                  ├─ Blank Card centered = create new
                  └─ Existing Playlist centered = append
Back (Faded):     ListPanel + AlbumRolodex (25% alpha)
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

## Playlist Concept

**Blank Card (Create New):**
- Always appears as first card in PlaylistRolodex
- In Browse mode: available for creating new playlists during track selection
- In Review mode: available for creating new playlists on-the-fly
- Display: `[+ NEW PLAYLIST]` or similar
- When centered + Right Trigger: auto-generate name (random bird name), create with selected tracks

**Existing Playlists:**
- Appear as carousel tiles after blank card
- In Browse: can append selected tracks to any existing playlist
- In Review: can view/edit existing playlist (delete tracks, rename)

**Logic:**
- If blank card centered when Right Trigger pressed → **Create mode**
- If existing playlist centered when Right Trigger pressed → **Append/Edit mode**

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
| Left X button | ? | Save (playlist, changes) |
| Left Y button | ? | (Reserved for future) |
| Right Trigger | Enter | Select album/playlist/track |
| Right Joystick Y (up) | W | Scroll track list up |
| Right Joystick Y (down) | S | Scroll track list down |
| Right A button | ? | Back / Close panel |
| Right B button | Backspace | (Reserved for future) |

**TODO:** Assign keyboard keys for Left X, Right A, Y button

---

## Mode State Machine

```
┌─────────────────────────────────────────────────────┐
│                   ModeController                     │
│           (Toggled by Left Trigger)                  │
└─────────────────────────────────────────────────────┘

Browse (Default)
├─ Shows: AlbumRolodex (lower), NavigationUI ("BROWSE ALBUMS")
├─ Input: Left Joystick X (scroll albums), Right Trigger (select album)
│         Left Trigger (mode toggle)
├─ OnEnter: 
│   - AlbumRolodex visible
│   - PlaylistRolodex hidden
│   - ListPanel hidden
│   - ModeText = "BROWSE ALBUMS"
└─ OnExit: Close ListPanel if open (before mode switch)

Review (Toggled via Left Trigger)
├─ Shows: PlaylistRolodex (upper), NavigationUI ("REVIEW PLAYLISTS")
├─ Input: Left Joystick X (scroll playlists), Right Trigger (select playlist)
│         Left Trigger (mode toggle)
│         Right Joystick Y + Right Trigger (when ListPanel expanded)
├─ OnEnter:
│   - PlaylistRolodex visible
│   - AlbumRolodex hidden
│   - ListPanel hidden (expands on playlist select)
│   - ModeText = "REVIEW PLAYLISTS"
│   - Refresh playlists from PlaylistManager
└─ OnExit: Close ListPanel if open, save any pending changes
```

**Create Mode (Embedded in Browse):**
- When album selected in Browse mode → ListPanel expands with TrackList (add-to-playlist)
- B button to save and collapse ListPanel → return to Browse AlbumRolodex
- Still in Browse mode, just ListPanel open

**State Safety:**
- Only Browse or Review active at a time
- Mode switch via left trigger only (no accidental UI clicks)
- ListPanel visibility independent of mode (can appear/disappear in either)
- Input handler respects current mode and ListPanel visibility

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
Scroll:    Left Joystick X-axis (blocked when TrackList open)
Interaction: Right Trigger to select
Behavior:  Show track list when Right Trigger pressed
Load:      Trigger ListController.Show()
```

**v1 Fix Applied:** Input blocking prevents playlist rotation while viewing tracks

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
