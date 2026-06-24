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

### 2. Browse Mode (Albums)
```
User: Left Joystick (left/right)
  ↓
XRInputManager: Detect input
  ↓
RolodexController (Browse): Scroll album carousel
  ↓
User: A button
  ↓
AlbumDataSource.OnSelected(index)
  ↓
Enter Create Mode
```

### 3. Create Mode (Add tracks to playlist)
```
User: Browse album tracks with joystick
  ↓
A button: Add track to current playlist (visual feedback)
  ↓
B button: Save playlist, return to Browse
  ↓
PlaylistManager.SavePlaylist()
  ↓
MQTT: Publish "playlist.created" → Node-RED
```

### 4. Review Mode (Edit playlists)
```
User: Left Trigger
  ↓
ModeController: Toggle Browse → Review
  ↓
PlaylistRolodex (Review): Show all playlists
  ↓
User: A button → Select playlist
  ↓
PlaylistTrackListController.Show(playlist)
  ↓
User: Up/Down arrows → Scroll tracks (center track highlighted)
  ↓
User: A button → Delete selected track
  ↓
PlaylistManager.SavePlaylist()
  ↓
MQTT: Publish "playlist.modified" → Node-RED
  ↓
User: B button → Close track list, return to playlist carousel
```

---

## Scene Structure (Single Scene)

```
MusicBox (Master Scene)
│
├── XRRig (Camera + Controllers)
│   ├── LeftController (XR Input)
│   └── RightController (XR Input)
│
├── Managers (Keep across modes)
│   ├── ModeController (Singleton)
│   ├── NetworkManager (Singleton)
│   ├── PlaylistManager (Singleton)
│   ├── MQTTManager (Singleton)
│   └── XRInputManager (Singleton)
│
└── UIContainer (Conditional visibility per mode)
    ├── NavigationUI (Browse/Review toggle, always visible)
    │   └── ModeButtons (visual indicator)
    │
    ├── BrowseUI (Hidden in Review/Create)
    │   ├── AlbumRolodex
    │   └── NowPlaying card
    │
    ├── ReviewUI (Hidden in Browse)
    │   ├── PlaylistRolodex
    │   └── TrackListPanel (toggled on selection)
    │
    └── CreateUI (Hidden until album selected)
        ├── AlbumTrackList
        ├── CurrentPlaylist card
        └── Save/Cancel buttons
```

**Why single scene?**
- Eliminates scene load delays
- Preserves singleton state
- Simpler XR session management
- Easier to debug mode switching

---

## Input Mapping (Quest 3S Touch Plus)

### Left Controller
| Input | Action | Mode |
|-------|--------|------|
| Trigger | Toggle Browse ↔ Review | All |
| Joystick X | Scroll carousel left/right | Browse/Review/Create |
| Joystick Y | Scroll track list up/down | Review (track list open) |
| A button | Select item | Browse/Create/Review |
| B button | Close panel / Return | Create/Review |

### Right Controller
| Input | Action | Mode |
|-------|--------|------|
| A button | Delete selected track | Review (track list open) |
| B button | Close track list / Return | Review (track list open) |

**Key Design:**
- **Left trigger is mode toggle** (proven working in v1)
- **A button is always select** (freed from mode switching conflicts)
- **B button is always back** (consistent navigation)
- **Joystick scrolling has mode guards** to prevent cross-UI interference

---

## Mode State Machine

```
┌─────────────────────────────────────────────────────┐
│                   ModeController                     │
└─────────────────────────────────────────────────────┘

Browse (default)
├─ Shows: AlbumRolodex, NowPlaying
├─ Input: Joystick (scroll albums), A (select)
└─ OnEnter: PlaylistRolodex hidden, AlbumRolodex visible
    OnExit: None

Review
├─ Shows: PlaylistRolodex, (optional) TrackListPanel
├─ Input: Joystick (scroll playlists/tracks), A (delete), B (close)
└─ OnEnter: AlbumRolodex hidden, PlaylistRolodex visible, refresh playlists
    OnExit: Close track list if open

Create
├─ Shows: AlbumTrackList, CurrentPlaylist card
├─ Input: Joystick (scroll tracks), A (add), B (save/cancel)
└─ OnEnter: Triggered by A-button in Browse on album selection
    OnExit: Save playlist, return to Browse via B button
```

**State Safety:**
- Only one mode active at a time
- Mode change clears previous mode UI
- Input handler respects current mode before processing

---

## UI Components

### 1. AlbumRolodex (Browse Mode)
```
Displays:  9 visible tiles in arc (center = selected)
Scroll:    Left joystick X-axis
Interaction: A button to select
Behavior:  Animate selected album to sides when A pressed
Load:      Album art from network (cached locally)
```

**v1 Fix Applied:** Works with 512 albums, smooth 60 FPS

### 2. PlaylistRolodex (Review Mode)
```
Displays:  6 visible tiles in arc (center = selected)
Scroll:    Left joystick X-axis (blocked when TrackList open)
Interaction: A button to select
Behavior:  Show track list when A pressed
Load:      Trigger PlaylisTrackListController.Show()
```

**v1 Fix Applied:** Input blocking prevents playlist rotation while viewing tracks

### 3. PlaylistTrackListController (Review Mode - Track List)
```
Displays:  6 visible rows (center row = selected/highlighted)
Scroll:    Left joystick Y-axis (up/down)
Selection: Center row is always selected
Display:   Track# | Title | Artist/Album (multi-source playlists)
Interact:  A = delete, B = close
Behavior:  When deleted, re-center selection on remaining tracks
```

**v1 Fix Applied:**
- Artist display for multi-album playlists
- Scroll limits calculated correctly: maxOffset = max(0, totalTracks - visibleRows)
- Input blocking prevents album rotation

### 4. NavigationUI (Always Visible)
```
Displays:  Two buttons: "Browse Albums" | "Review Playlists"
Highlight: Current mode highlighted
Interact:  Left trigger to toggle (not clickable)
Behavior:  Shows which mode you're in, no manual mode button needed
```

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

### Phase 1: Foundation (Core + Browse)
- [ ] XRRig + Input system
- [ ] NetworkManager (UPnP discovery)
- [ ] ModeController + NavigationUI
- [ ] AlbumRolodex + album art loading
- [ ] Test: Browse 512 albums, smooth 60 FPS
- **GitHub:** Commit "Phase 1: Browse mode working"

### Phase 2: Review (Playlists)
- [ ] PlaylistManager (load/save JSON)
- [ ] PlaylistRolodex (Review mode)
- [ ] PlaylistTrackListController (track display)
- [ ] Track deletion with re-centering
- [ ] Input blocking (playlist scroll blocked when track list open)
- [ ] Test: Load playlists, scroll, delete tracks
- **GitHub:** Commit "Phase 2: Review mode working"

### Phase 3: Create (Playlist Builder)
- [ ] CreateUI + CurrentPlaylist card
- [ ] AlbumTrackList → Add to playlist
- [ ] Playlist save + MQTT publish
- [ ] Return to Browse after save
- [ ] Test: Create playlist from multiple albums
- **GitHub:** Commit "Phase 3: Create mode working"

### Phase 4: Polish (MQTT, Optimization)
- [ ] MQTT integration (publish all events)
- [ ] Playback controller (send play/pause to server)
- [ ] Error handling (network timeouts, file I/O)
- [ ] Performance optimization (frame rate stability)
- [ ] Test on headset (not just editor)
- **GitHub:** Commit "Phase 4: Polish and MQTT ready"

### Phase 5: Deployment
- [ ] Build APK for Quest 3S
- [ ] Test full flow on headset
- [ ] GitHub release v2.0.0
- [ ] Document secrets.h setup

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
