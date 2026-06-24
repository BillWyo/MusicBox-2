# MusicBox 2.0

Quest 3S VR application for browsing and managing music playlists via UPnP/DLNA music servers.

## Architecture

- **Browse Mode**: Rolodex carousel UI for browsing 500+ albums
- **Review Mode**: Playlist management and track editing
- **Create Mode**: Playlist creation from album tracks
- **Integration**: MQTT → Node-RED for external control/monitoring
- **Network**: UPnP/DLNA discovery and playback control
- **Target**: Meta Quest 3S with OpenXR

## Setup

1. Unity 2022.3 LTS or later
2. Configure XR Plugin Manager for Meta Quest Touch Plus Controller
3. Set MQTT broker address in `Assets/Resources/secrets.h` (copy from `secrets.h.example`)
4. Load `Assets/Scenes/MusicBox.unity`

## Project Structure

```
Assets/
├── Scripts/           # Core game logic
├── Scenes/           # Unity scenes
├── Prefabs/          # Reusable components
├── Resources/        # Runtime configuration
└── Plugins/          # Third-party libraries (M2Mqtt)
```

## Lessons from v1

- Single drive for project (avoid D/C split)
- GitHub as source of truth
- MQTT→Node-RED pattern validated
- Rolodex UI scaling tested
- Mode switching via left trigger (not A button)
- Playlist scroll limits require track count consideration

## Known Issues

- Track list scroll range limited by visible rows vs. total tracks
- Blank card appears briefly on Review mode entry

## Next Steps

See ARCHITECTURE.md for full design plan.
