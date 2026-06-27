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

### 2. Browse Mode → Create (Select tracks from album)
```
User: Left Joystick X (scroll albums), Right Trigger to select
  ↓
Album selected
  ↓
ListPanel expands (bottom)
  Shows: Album's tracklist (all tracks unselected)
  ↓
User: Right Joystick Y to scroll, Right Trigger on each track to select
  Track highlights when selected (cumulative selection)
  ↓
User: Left A button when done selecting
  ↓
PlaylistRolodex slides up (top)
  Shows: [BLANK CARD] [Playlist 1] [Playlist 2] ...
  Blank card centered by default
```

### 3. Browse Mode → Create (Target playlist)
```
PlaylistRolodex visible with destination options
  ↓
User: Left Joystick X to center desired playlist/blank
  ↓
If BLANK CARD centered:
  → Auto-generate playlist name (random bird name)
  → Right Trigger creates new playlist with selected tracks
  ↓
If EXISTING PLAYLIST centered:
  → Right Trigger adds selected tracks to existing playlist
  ↓
Playlist updated → scrolls down to show newly added tracks
  ↓
Left X button to save or Left A to close
  ↓
PlaylistRolodex and ListPanel fade out
  ↓
Return to Browse mode (AlbumRolodex visible)
  ↓
PlaylistManager.SavePlaylist()
  ↓
MQTT: Publish "playlist.created" or "playlist.modified" → Node-RED
```

### 4. Browse Mode → Review (Toggle mode)
```
User: Left Trigger
  ↓
ModeController: Toggle Browse → Review
  ↓
AlbumRolodex fades, PlaylistRolodex appears
  ↓
PlaylistRolodex: Shows all playlists (no blank card in Review mode)
  ↓
User: Left Joystick X (scroll playlists), Right Trigger to select
  ↓
Playlist selected
  ↓
ListPanel expands (center)
  Shows: Playlist's tracklist (for editing/deleting)
  ↓
User: Right Joystick Y (scroll tracks), Right Trigger to delete selected track
  ↓
Left X button to save changes, Left A button to close ListPanel
  ↓
PlaylistManager.SavePlaylist() after each deletion
  ↓
MQTT: Publish "playlist.modified" → Node-RED
  ↓
Return to PlaylistRolodex
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

### 3. ListController (Track List - Browse/Review/Create)
```
Displays:  6 visible rows (center row = selected/highlighted)
Scroll:    Right Joystick Y-axis (up/down)
Selection: Center row is always selected
Display:   Track# | Title | Artist/Album (multi-source playlists)
Interact:  Right Trigger = select/delete, Left A = close
Save:      Left X button saves changes
Behavior:  When deleted, re-center selection on remaining tracks
```

**v1 Fix Applied:**
- Artist display for multi-album playlists
- Scroll limits calculated correctly: maxOffset = max(0, totalTracks - visibleRows)
- Input blocking prevents album rotation

### 3. NavigationUI (Status Display, Always Visible)
```
Displays:  Current mode name only (TextMeshPro)
           "BROWSE ALBUMS" (Browse mode)
           "REVIEW PLAYLISTS" (Review mode)
Updates:   When left trigger pressed
Interact:  No clickable elements (read-only status)
Behavior:  Shows which mode you're in for reference
           Mode switching via left trigger only
```

**Design Rationale:**
- Purely informational, not interactive
- Reduces UI complexity in VR
- Left trigger is single input for mode switching
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
