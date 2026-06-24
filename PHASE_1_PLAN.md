# Phase 1 Implementation Plan: Browse + Create

**Goal:** Working Browse mode with integrated playlist creation  
**Duration:** Estimated 8-10 working sessions  
**Checkpoint:** GitHub commit after each major component

---

## Task Breakdown

### BLOCK 1: Foundation (Sessions 1-2)
Core infrastructure that everything else depends on.

#### 1.1 Scene Setup & XR Configuration
- [ ] Create `Assets/Scenes/MusicBox.unity` scene
- [ ] Add XRRig prefab (Camera + Controllers)
- [ ] Configure XR Plugin Manager (OpenXR, Meta Quest Touch Plus)
- [ ] Test: Controllers detected, button input flows
- **GitHub Checkpoint:** `git commit "Phase 1.1: XRRig + OpenXR configured"`

#### 1.2 Singleton Managers (empty shells)
Create all manager scripts with basic Singleton pattern, no logic yet.

- [ ] `Assets/Scripts/Core/ModeController.cs` (mode state, events)
- [ ] `Assets/Scripts/Core/NetworkManager.cs` (placeholder for UPnP)
- [ ] `Assets/Scripts/Core/PlaylistManager.cs` (placeholder for JSON I/O)
- [ ] `Assets/Scripts/Input/XRInputManager.cs` (controller polling)
- [ ] Instantiate all in scene as root-level GameObjects
- [ ] Test: All managers initialize without errors
- **GitHub Checkpoint:** `git commit "Phase 1.2: Singleton managers scaffolding"`

#### 1.3 NavigationUI (status display)
- [ ] Create `Assets/Scripts/UI/NavigationUI.cs`
- [ ] Create scene GameObject under XRRig/UIContainer
- [ ] TextMeshPro for mode name ("BROWSE ALBUMS" / "REVIEW PLAYLISTS")
- [ ] Subscribe to ModeController.OnModeSelected event
- [ ] Test: Text updates when mode changes
- **GitHub Checkpoint:** `git commit "Phase 1.3: NavigationUI status display"`

---

### BLOCK 2: Input System (Session 2-3)
XR controller input handling (non-UI, just polling).

#### 2.1 XRInputManager - Trigger Polling
- [ ] `Assets/Scripts/Input/XRInputManager.cs`
- [ ] Poll left trigger (CommonUsages.trigger)
- [ ] Detect press-edge (not held, once per press)
- [ ] Publish OnLeftTriggerPressed event
- [ ] Add debug log for testing
- [ ] Test: Left trigger press detected in console
- **GitHub Checkpoint:** `git commit "Phase 1.4: Left trigger input detection"`

#### 2.2 XRInputManager - Button & Joystick Polling
- [ ] Poll right A button (CommonUsages.primaryButton)
- [ ] Poll right B button (CommonUsages.secondaryButton)
- [ ] Poll left joystick X/Y (CommonUsages.primary2DAxis)
- [ ] Publish events: OnAButtonPressed, OnBButtonPressed, OnJoystickMoved
- [ ] Test: All inputs detected in console
- **GitHub Checkpoint:** `git commit "Phase 1.5: Complete XR input polling"`

#### 2.3 ModeController - Mode Switching
- [ ] Listen to XRInputManager.OnLeftTriggerPressed
- [ ] Toggle mode: Browse ↔ Review
- [ ] Invoke OnModeSelected(newMode)
- [ ] Update NavigationUI text
- [ ] Test: Left trigger toggles Browse ↔ Review, text updates
- **GitHub Checkpoint:** `git commit "Phase 1.6: Mode switching via left trigger"`

---

### BLOCK 3: Generic Controllers (Sessions 3-5)
Stateless, data-driven UI components.

#### 3.1 RolodexController (Generic Carousel)
- [ ] Create `Assets/Scripts/UI/RolodexController.cs`
- [ ] Public methods: `SetDataSource(ITileDataSource)`, `Refresh()`
- [ ] Properties: `CurrentIndex` (read-only), configuration (visibleTiles, arcRadius, etc.)
- [ ] Input handling: left joystick X-axis for scroll
- [ ] Tile positioning: arc math (9 tiles, center highlighted)
- [ ] Event: `OnTileSelected(int index)` on A button
- [ ] Test: Instantiate with dummy data, scroll and select
- **GitHub Checkpoint:** `git commit "Phase 1.7: Generic RolodexController"`

#### 3.2 ListController (Generic Vertical List)
- [ ] Create `Assets/Scripts/UI/ListController.cs`
- [ ] Public methods: `SetDataSource(IListDataSource)`, `Refresh()`, `ScrollToIndex(int)`
- [ ] Properties: `CurrentIndex` (read-only), `ScrollOffset`, configuration
- [ ] Input handling: left joystick Y-axis for scroll
- [ ] Row positioning: 6 visible rows, center highlighted
- [ ] Track per-row selection state (highlight)
- [ ] Event: `OnRowSelected(int index)` on A button
- [ ] Test: Instantiate with dummy data, scroll and select
- **GitHub Checkpoint:** `git commit "Phase 1.8: Generic ListController"`

---

### BLOCK 4: Data Sources (Sessions 5-6)
Pluggable data providers for RolodexController and ListController.

#### 4.1 Interfaces & Base Classes
- [ ] Create `Assets/Scripts/Data/ITileDataSource.cs` interface
  - Methods: `int Count`, `string GetTitle(idx)`, `string GetArtist(idx)`, `string GetArtUrl(idx)`, `OnSelected(idx)`
- [ ] Create `Assets/Scripts/Data/IListDataSource.cs` interface
  - Methods: `int Count`, `string GetTitle(idx)`, `string GetSubtitle(idx)`, `OnSelected(idx)`
- [ ] Test: Interfaces compile
- **GitHub Checkpoint:** `git commit "Phase 1.9: Data source interfaces"`

#### 4.2 AlbumDataSource
- [ ] Create `Assets/Scripts/Data/DataSources/AlbumDataSource.cs`
- [ ] Implement ITileDataSource
- [ ] Load albums from NetworkManager (empty list initially, will populate when UPnP discovery works)
- [ ] OnSelected: emit album index for track list display
- [ ] Test: Compile and instantiate
- **GitHub Checkpoint:** `git commit "Phase 1.10: AlbumDataSource implementation"`

#### 4.3 PlaylistDataSource
- [ ] Create `Assets/Scripts/Data/DataSources/PlaylistDataSource.cs`
- [ ] Implement ITileDataSource
- [ ] First item: BLANK CARD (special case for new playlist creation)
- [ ] Remaining items: load from PlaylistManager
- [ ] OnSelected: emit playlist index for track list display
- [ ] Test: Compile and instantiate
- **GitHub Checkpoint:** `git commit "Phase 1.11: PlaylistDataSource with blank card"`

#### 4.4 TrackListDataSource (selection mode)
- [ ] Create `Assets/Scripts/Data/DataSources/TrackListDataSource.cs`
- [ ] Implement IListDataSource
- [ ] Load tracks from selected album
- [ ] Track per-track selection state (A-button toggles highlight)
- [ ] OnSelected: emit track index (for visual feedback)
- [ ] Test: Compile and instantiate with sample album
- **GitHub Checkpoint:** `git commit "Phase 1.12: TrackListDataSource (selection mode)"`

---

### BLOCK 5: UPnP Discovery (Session 6-7)
Album loading from network.

#### 5.1 NetworkManager - UPnP Discovery
- [ ] Implement M-SEARCH (3-second discovery window)
- [ ] Parse device descriptions, filter for MediaServers
- [ ] For each MediaServer: call Browse("0") and Browse("0$albums")
- [ ] Handle pagination (200 albums per request)
- [ ] Collect all albums into list
- [ ] Emit OnAlbumsLoaded(albums) event
- [ ] Test: Discover and load albums (will fail without music server, mock data for now)
- **GitHub Checkpoint:** `git commit "Phase 1.13: UPnP album discovery"`

#### 5.2 AlbumDataSource - Hook to NetworkManager
- [ ] Listen to NetworkManager.OnAlbumsLoaded
- [ ] Update internal album list
- [ ] Trigger RolodexController.Refresh()
- [ ] Test: Albums appear in rolodex when mocked
- **GitHub Checkpoint:** `git commit "Phase 1.14: Albums populate from network"`

---

### BLOCK 6: Scene Wiring (Sessions 7-8)
Connect all pieces in the scene hierarchy.

#### 6.1 RolodexContainer Setup
- [ ] Create empty GameObject "RolodexContainer" under UIContainer
- [ ] Position at shared rotation point (center, in front of player)
- [ ] Add AlbumRolodex as child (Y = -0.5)
- [ ] Add PlaylistRolodex as child (Y = +0.5)
- [ ] Configure each RolodexController component
- [ ] Set data sources: Album and Playlist respectively
- [ ] Test: Both visible, rotating correctly
- **GitHub Checkpoint:** `git commit "Phase 1.15: Rolodex scene hierarchy"`

#### 6.2 ListPanel Setup
- [ ] Create empty GameObject "ListPanel" under UIContainer
- [ ] Position center, slightly forward
- [ ] Add ListController component
- [ ] Configure for 6 visible rows
- [ ] Hide ListPanel initially
- [ ] Test: ListPanel can be toggled visible/hidden
- **GitHub Checkpoint:** `git commit "Phase 1.16: ListPanel scene hierarchy"`

#### 6.3 Wire Mode Switching
- [ ] ModeController.OnModeSelected event:
  - Browse: AlbumRolodex visible, PlaylistRolodex hidden, ListPanel hidden
  - Review: PlaylistRolodex visible, AlbumRolodex hidden, ListPanel hidden (until playlist selected)
- [ ] Test: Mode toggle correctly shows/hides rolodexes
- **GitHub Checkpoint:** `git commit "Phase 1.17: Mode-driven visibility"`

---

### BLOCK 7: Playlist Manager & Persistence (Session 8-9)
Track listing and creation with random bird names.

#### 7.1 PlaylistManager - Load & Save
- [ ] Create `Assets/Scripts/Core/PlaylistManager.cs`
- [ ] LoadAllPlaylists: async load JSON files from disk
- [ ] SavePlaylist(playlist): write JSON to disk
- [ ] Emit OnPlaylistsLoaded(playlists) event when loaded
- [ ] Test: Load existing playlists, save new one
- **GitHub Checkpoint:** `git commit "Phase 1.18: Playlist persistence"`

#### 7.2 Random Bird Name Generator
- [ ] Create `Assets/Scripts/Utils/BirdNameGenerator.cs`
- [ ] Simple list of 20+ bird names (sparrow, cardinal, raven, hawk, etc.)
- [ ] Public method: `string GetRandomName()`
- [ ] Test: Generate 10 names, verify variation
- **GitHub Checkpoint:** `git commit "Phase 1.19: Random bird name generator"`

#### 7.3 PlaylistDataSource - Blank Card Logic
- [ ] First item in playlist list is BLANK
- [ ] OnSelected(0) with blank card: create new playlist with random bird name
- [ ] OnSelected(n) with existing: target that playlist for track addition
- [ ] Test: Blank card creates new playlist, existing card doesn't
- **GitHub Checkpoint:** `git commit "Phase 1.20: Blank card playlist creation"`

---

### BLOCK 8: Create Flow Integration (Sessions 9-10)
Per-track selection and playlist targeting.

#### 8.1 Track Selection in ListController
- [ ] Track selected/unselected state per row
- [ ] A-button toggles selection (visual highlight changes)
- [ ] B-button confirms selection → show PlaylistRolodex
- [ ] Test: Select multiple tracks, B button shows playlists
- **GitHub Checkpoint:** `git commit "Phase 1.21: Per-track selection flow"`

#### 8.2 Playlist Targeting & Track Addition
- [ ] When playlist centered in PlaylistRolodex:
  - A-button adds selected tracks to that playlist (or creates new)
- [ ] Playlist scrolls down to show newly added tracks
- [ ] Fade transition: PlaylistRolodex and ListPanel fade out
- [ ] Return to Browse mode (AlbumRolodex visible)
- [ ] PlaylistManager.SavePlaylist() called
- [ ] Test: Full flow: album → tracks → playlist → saved
- **GitHub Checkpoint:** `git commit "Phase 1.22: Complete Create flow"`

#### 8.3 Input Blocking
- [ ] When ListPanel visible: only handle A/B/joystick Y (track scrolling)
- [ ] Block left joystick X (prevent album rotation)
- [ ] When PlaylistRolodex visible for targeting: only joystick X matters
- [ ] Test: No cross-UI input interference
- **GitHub Checkpoint:** `git commit "Phase 1.23: Input blocking guards"`

---

### BLOCK 9: Polish & Testing (Session 10+)
Frame rate, visual polish, headset testing.

#### 9.1 Performance Optimization
- [ ] Profile frame rate (target 60 FPS)
- [ ] Optimize tile rendering (batching, LOD)
- [ ] Memory profiling (no leaks on repeated album/playlist loads)
- [ ] Test: Sustained 60 FPS during Browse and Create
- **GitHub Checkpoint:** `git commit "Phase 1.24: Performance optimization"`

#### 9.2 Visual Polish
- [ ] Fade transitions timing (0.3-0.5s)
- [ ] Highlight colors (yellow for selected, white for normal)
- [ ] Text sizing and positioning
- [ ] Album art loading UI (placeholder while loading)
- [ ] Test: Visually appealing, responsive feels fast
- **GitHub Checkpoint:** `git commit "Phase 1.25: Visual polish"`

#### 9.3 Headset Testing
- [ ] Build APK for Quest 3S
- [ ] Test full Browse → Create → Browse flow on headset
- [ ] Verify controller input works correctly in VR
- [ ] Test 512 albums don't cause performance drops
- [ ] Test creating multiple playlists, verify save/load
- **GitHub Checkpoint:** `git commit "Phase 1.26: Headset tested and working"`

---

## Dependency Graph

```
Phase 1.1: XRRig
  ↓
Phase 1.2: Managers (blocking)
  ↓
Phase 1.3: NavigationUI
Phase 1.4-6: Input System (blocking for everything below)
  ↓
Phase 1.7-8: Generic Controllers (blocking for data sources)
  ↓
Phase 1.9-12: Data Sources (can start once controllers done)
  ↓
Phase 1.13-14: UPnP Discovery (parallel with data sources)
  ↓
Phase 1.15-17: Scene Wiring (can start once controllers + managers done)
  ↓
Phase 1.18-23: Playlist Manager + Create Flow
  ↓
Phase 1.24-26: Polish + Headset Testing
```

---

## Critical Path (Fastest Route)

1. **Session 1-2:** Blocks 1 & 2 → XRRig, Managers, Input
2. **Session 3:** Block 3 → Generic controllers
3. **Session 4:** Block 4 → Data sources
4. **Session 5:** Block 5 → UPnP (parallel with Block 6)
5. **Session 6:** Block 6 → Scene wiring
6. **Session 7-8:** Block 7-8 → Playlist + Create flow
7. **Session 9-10:** Block 9 → Polish + headset test

---

## GitHub Commit Checkpoints

Commits to make at each major milestone (26 total):
- 1.1-1.3: Foundation (3 commits)
- 1.4-1.6: Input system (3 commits)
- 1.7-1.8: Generic controllers (2 commits)
- 1.9-1.12: Data sources (4 commits)
- 1.13-1.14: UPnP discovery (2 commits)
- 1.15-1.17: Scene wiring (3 commits)
- 1.18-1.23: Playlist + Create flow (6 commits)
- 1.24-1.26: Polish + testing (3 commits)

**Rule:** No more than 2 hours of work between commits. If something feels like 2+ hours, split it into smaller milestones.

---

## Success Criteria for Phase 1 Complete

- [ ] Browse 512 albums smoothly (60 FPS)
- [ ] Select album → see tracks
- [ ] Select tracks per-item (A-button toggle)
- [ ] B button shows playlists (with blank card at top)
- [ ] Center blank card → A button creates "SomeBirdName" playlist with selected tracks
- [ ] Center existing playlist → A button adds tracks to it
- [ ] Playlist saved to disk and reappears in list
- [ ] B button or completion → fade back to Browse
- [ ] Left trigger toggles Browse ↔ Review (Review shows PlaylistRolodex, no blank card)
- [ ] Runs at 60 FPS sustained
- [ ] Tested on headset

---

**Status:** Ready to start Session 1  
**Next:** Open Unity, configure XR, create MusicBox scene
