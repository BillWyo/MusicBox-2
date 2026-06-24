# CLAUDE.md — MusicBox 2.0 Development Guide

## Project Overview

**MusicBox 2.0** is a VR music browser for Meta Quest 3S that discovers UPnP/DLNA music servers, manages playlists, and syncs state to Node-RED via MQTT.

- **Repository**: https://github.com/BillWyo/MusicBox-2
- **Local Path**: `C:\Users\johan\Documents\MusicBox2`
- **Target**: Meta Quest 3S (Android, OpenXR)
- **Engine**: Unity 2022.3 LTS
- **Architecture**: See `ARCHITECTURE.md`

---

## Project Structure

```
C:\Users\johan\Documents\MusicBox2/
├── .git/                          # Git repository
├── Assets/
│   ├── Scripts/
│   │   ├── Core/                 # Managers: Network, Playlist, Audio
│   │   ├── UI/                   # Mode, Rolodex, TrackList, Creator
│   │   ├── Input/                # XR input handling and routing
│   │   ├── Network/              # MQTT, UPnP, Playback control
│   │   └── Data/                 # Models: Playlist, Track, Album
│   ├── Scenes/
│   │   └── MusicBox.unity        # Master scene (single scene arch)
│   ├── Prefabs/                  # Reusable UI components
│   ├── Resources/                # Runtime config (secrets.h)
│   └── Plugins/                  # Third-party: M2Mqtt.dll
├── ProjectSettings/               # Unity project config (XR, build)
├── Packages/manifest.json         # Unity dependencies
├── ARCHITECTURE.md               # Full system design
├── README.md                      # Quick start
├── CLAUDE.md                      # This file
└── .gitignore                     # Excludes Library/, Temp/, etc.
```

---

## Setup Instructions

### 1. Clone & Open Project

```bash
git clone https://github.com/BillWyo/MusicBox-2.git
cd MusicBox-2
```

Open in **Unity Hub** → Add project → `C:\Users\johan\Documents\MusicBox2`

### 2. Configure XR Plugin

**Window** → **XR Plugin Management**

- ✅ Install: **OpenXR Plugin**
- ✅ Set active loader: **OpenXR**
- ✅ Edit: **OpenXR Settings**
  - Add interaction profile: **Meta Quest Touch Plus Controller**
  - Enable: Touch controller input

**ProjectSettings** → **Player** → **XR Plugin Management**
- ✅ OpenXR enabled for Android

### 3. Configure Build Settings

**File** → **Build Settings**
- Platform: **Android**
- Texture Compression: **ASTC**
- API Level: **29** (minimum for Quest 3S)
- Graphics API: **OpenGL ES 3.0** (or Vulkan)

### 4. Network Configuration

Create `Assets/Resources/secrets.h`:

```csharp
// EXAMPLE - DO NOT commit real credentials!
public static class NetworkConfig
{
    public const string MQTT_BROKER = "192.168.1.18";     // Node-RED laptop IP
    public const int MQTT_PORT = 1883;
    public const string MQTT_USERNAME = "";               // if needed
    public const string MQTT_PASSWORD = "";               // if needed
    
    public const string UPNP_DISCOVERY_ST = "ssdp:all";
    public const int UPNP_DISCOVERY_TIMEOUT_MS = 3000;
}
```

**DO NOT commit secrets.h** — it's in .gitignore

### 5. Verify Setup

- ✅ No compilation errors (`Ctrl+Shift+R` refresh if needed)
- ✅ XR Plugin Management shows OpenXR active
- ✅ Assets/Resources/secrets.h exists and compiles
- ✅ Git status clean (only temporary files in Temp/, Library/)

---

## Coding Conventions

### Scripts Organization

**By Responsibility:**
- `Core/` → Singleton managers (NetworkManager, PlaylistManager, ModeController)
- `UI/` → Visual controllers (Rolodex, TrackList, Creator)
- `Input/` → XR controller input handling
- `Network/` → External communication (MQTT, UPnP)
- `Data/` → Models (no logic, just data containers)

### Naming

- **Classes**: PascalCase (e.g., `NetworkManager`, `AlbumRolodex`)
- **Methods**: PascalCase (e.g., `OnPlaylistsLoaded()`, `HandleInput()`)
- **Fields**: camelCase + underscore prefix for private (e.g., `_currentMode`, `_rolodexController`)
- **Events**: PascalCase, `On` prefix (e.g., `OnModeSelected`, `OnPlaylistDeleted`)

### Singleton Pattern

All managers are singletons:

```csharp
public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }
}
```

### Comments

- **NO** multi-line docstrings or explanations of what code does
- **YES** brief comments for non-obvious behavior or workarounds
- Example:
  ```csharp
  // v1 fix: left trigger prevents A-button conflicts in mode switching
  if (_leftController.TryGetFeatureValue(CommonUsages.trigger, out float value))
  ```

### Input Handling

**Safe pattern** for detecting button press (not held):

```csharp
bool isPressed = _controller.TryGetFeatureValue(CommonUsages.primaryButton, out bool pressed);
if (isPressed && pressed && !_wasButtonPressed)
{
    // Execute action once per press
}
_wasButtonPressed = pressed;
```

---

## Git Workflow

### Branch Strategy

- **main**: Release-ready, fully tested
- **develop**: Integration branch (if team grows)
- **feature/**: Feature work (e.g., `feature/phase1-browse`)

### Commit Style

```
git commit -m "Short imperative summary (under 50 chars)

Detailed explanation if needed. References ARCHITECTURE.md phases.
Includes what changed and why."
```

**Prefix examples:**
- `Phase 1: Browse mode working`
- `Fix: Input blocking prevents playlist rotation`
- `Feat: MQTT integration for Node-RED`

### Always Push After Commits

```bash
git push origin main
```

GitHub is **source of truth** — local disk is just working copy.

---

## Common Tasks

### Build for Quest 3S

```bash
# In Unity:
# 1. Connect Quest via USB (enable Developer Mode)
# 2. File → Build Settings → Build And Run
# OR
adb install -r Builds/MusicBox2.apk
```

### Run in Editor

- **Play** → Editor simulates VR (no actual controllers)
- **Input simulated**: Spacebar (left trigger), Arrow keys (joystick), Return (A button)
- **Useful for**: UI layout, scene structure, networking flow

### Debug on Headset

```bash
# View console output from Quest while running
adb logcat | grep "Unity\|MusicBox\|MQTT"
```

### Reset Project State

```bash
# Clean generated files (safe, will regenerate)
rm -r Library Temp obj *.log

# Then reload in Unity (reimports everything)
```

---

## Known Constraints & Workarounds

### 1. Scroll Range Limits (v1 Issue)

**Problem**: With `visibleRows=6`, playlists with <6 tracks can't scroll.

**Why**: `maxOffset = max(0, totalTracks - visibleRows)`

**Solution**: Accept this UX. Don't try to scroll playlists <6 tracks. For larger playlists (10+), scrolling works smoothly.

**Code reference**: `PlaylistTrackListController.cs` line ~235

### 2. Input Blocking (v1 Issue)

**Problem**: Joystick up/down for track list scrolling was also rotating playlist carousel.

**Why**: Both RolodexController and TrackListController listening to same input.

**Solution**: Check if `TrackListController.Canvas.activeSelf` before responding to scroll input.

**Code reference**: `RolodexController.cs` line ~184

### 3. Mode Switching (v1 Issue)

**Problem**: A button selected album AND toggled mode, preventing proper flow.

**Why**: NavigationUIController listening to A button globally.

**Solution**: Use left trigger ONLY for mode toggle. A button reserved for selection only.

**Code reference**: `NavigationUIController.cs` line ~99

### 4. Stereo Rendering (v1 Abandoned)

**Attempted**: Laser pointer from controller position for UI input.

**Issue**: Double-dot artifact on Quest 3S stereo rendering at certain depths.

**Decision**: Abandoned. Use XR input directly (triggers, buttons, joystick).

---

## Testing Checklist

Before committing to main:

- [ ] No compilation errors
- [ ] Scene loads without errors
- [ ] XR rig initializes (controllers detected in logs)
- [ ] Mode toggle works (Spacebar in editor, left trigger on headset)
- [ ] Input logging shows controller data flowing
- [ ] MQTT connects to broker (check logs)
- [ ] No memory leaks (check Profiler)
- [ ] Runs 60 FPS stable (Profiler window)

---

## Debugging

### Quick Diagnostics

**Check Controller Input:**
```csharp
// Add to XRInputManager.cs temporarily
Debug.Log($"Left trigger: {leftTrigger}, Right A: {rightA}, Joystick: {joystick}");
```

**Check MQTT:**
```csharp
// In MQTTManager.cs
Debug.Log($"MQTT connected: {_client.IsConnected}");
Debug.Log($"Publishing: {topic} = {payload}");
```

**Check Scene Load:**
```csharp
// In ModeController.cs
Debug.Log($"Mode changed to: {newMode}, UI visibility updated");
```

### Useful Commands

```bash
# Monitor logs while playing in editor
# Window → General → Console (tail output as you interact)

# Monitor XR input
# Window → TextMesh Pro → Import TMP Essentials (for debugging UI)

# Profile performance
# Window → Analysis → Profiler (watch CPU, Memory, Draw calls)
```

---

## v1 Lessons Applied

✅ **Single drive only** (C drive) — avoids I/O corruption from D/C split

✅ **GitHub as source of truth** — local disk is just working copy

✅ **Left trigger for mode switching** — prevents A-button conflicts

✅ **Input blocking guards** — prevents cross-UI interference

✅ **MQTT→Node-RED pipeline validated** — use existing pattern

✅ **Rolodex UI scales** — proven with 512 albums in v1

✅ **Mode state machine** — clear guards for Browse/Create/Review

---

## When This File Needs Updates

- New coding conventions adopted
- New workarounds discovered
- Build/setup process changes
- Directory structure reorganized
- Git workflow changes

**Update immediately** so both of us stay in sync.

---

## Quick Links

- **Architecture**: `ARCHITECTURE.md`
- **README**: `README.md`
- **GitHub**: https://github.com/BillWyo/MusicBox-2
- **v1 Reference**: https://github.com/BillWyo/MusicBox (legacy)

---

**Last Updated**: 2026-06-24

**Next Phase**: Phase 1 (Browse mode) — See ARCHITECTURE.md
