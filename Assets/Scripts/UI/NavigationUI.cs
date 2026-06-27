using UnityEngine;
using TMPro;

public class NavigationUI : MonoBehaviour
{
    private TextMeshPro _modeText;
    private RolodexController _albumRolodex;
    private RolodexController _playlistRolodex;

    void Start()
    {
        _modeText = GetComponent<TextMeshPro>();
        ModeController.Instance.OnModeSelected += OnModeChanged;
        UpdateDisplay(ModeController.Instance.CurrentMode, 0, 0);

        TrySubscribeToRolodexes();
    }

    void Update()
    {
        if (_albumRolodex == null || _playlistRolodex == null)
        {
            TrySubscribeToRolodexes();
        }
    }

    void TrySubscribeToRolodexes()
    {
        if (_albumRolodex == null)
        {
            var albRolodex = FindObjectOfType<RolodexController>();
            // Find the one attached to AlbumRolodex GameObject
            foreach (var controller in FindObjectsOfType<RolodexController>())
            {
                if (controller.gameObject.name == "AlbumRolodex")
                {
                    _albumRolodex = controller;
                    _albumRolodex.OnIndexChanged += OnAlbumIndexChanged;
                    break;
                }
            }
        }

        if (_playlistRolodex == null)
        {
            // Find the one attached to PlaylistRolodex GameObject
            foreach (var controller in FindObjectsOfType<RolodexController>())
            {
                if (controller.gameObject.name == "PlaylistRolodex")
                {
                    _playlistRolodex = controller;
                    _playlistRolodex.OnIndexChanged += OnPlaylistIndexChanged;
                    break;
                }
            }
        }
    }

    void OnModeChanged(ModeController.Mode newMode)
    {
        int currentIndex = 0;
        int total = 0;

        if (newMode == ModeController.Mode.Browse && _albumRolodex != null)
        {
            currentIndex = _albumRolodex.CurrentIndex;
            total = _albumRolodex.PublicDataSource?.Count ?? 0;
        }
        else if (newMode == ModeController.Mode.Review && _playlistRolodex != null)
        {
            currentIndex = _playlistRolodex.CurrentIndex;
            total = _playlistRolodex.PublicDataSource?.Count ?? 0;
        }

        UpdateDisplay(newMode, currentIndex, total);
    }

    void OnAlbumIndexChanged(int index, int total)
    {
        if (ModeController.Instance.CurrentMode == ModeController.Mode.Browse)
        {
            UpdateDisplay(ModeController.Mode.Browse, index, total);
        }
    }

    void OnPlaylistIndexChanged(int index, int total)
    {
        if (ModeController.Instance.CurrentMode == ModeController.Mode.Review)
        {
            UpdateDisplay(ModeController.Mode.Review, index, total);
        }
    }

    void UpdateDisplay(ModeController.Mode mode, int currentIndex, int total)
    {
        if (_modeText == null)
        {
            _modeText = GetComponent<TextMeshPro>();
            if (_modeText == null)
            {
                Debug.LogWarning("NavigationUI: TextMeshPro component not found");
                return;
            }
        }

        string modeText = mode == ModeController.Mode.Browse ? "BROWSE ALBUMS" : "REVIEW PLAYLISTS";
        string itemType = mode == ModeController.Mode.Browse ? "Album" : "Playlist";

        if (total > 0)
        {
            _modeText.text = $"{modeText} | {itemType} {currentIndex + 1} of {total}";
        }
        else
        {
            _modeText.text = modeText;
        }
    }

    void OnDestroy()
    {
        if (ModeController.Instance != null)
            ModeController.Instance.OnModeSelected -= OnModeChanged;

        if (_albumRolodex != null)
            _albumRolodex.OnIndexChanged -= OnAlbumIndexChanged;

        if (_playlistRolodex != null)
            _playlistRolodex.OnIndexChanged -= OnPlaylistIndexChanged;
    }
}
