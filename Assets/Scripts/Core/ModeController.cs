using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class ModeController : MonoBehaviour
{
    public static ModeController Instance { get; private set; }

    public enum Mode { Browse, Review }
    public Mode CurrentMode { get; private set; } = Mode.Browse;

    public event System.Action<Mode> OnModeSelected;

    [SerializeField] private GameObject _albumRolodex;
    [SerializeField] private GameObject _playlistRolodex;
    [SerializeField] private GameObject _listPanel;
    [SerializeField] private GameObject _playlistPanel;
    [SerializeField] private RolodexController _albumRolodexController;
    [SerializeField] private RolodexController _playlistRolodexController;
    [SerializeField] private ListController _listController;
    [SerializeField] private ListController _playlistController;
    [SerializeField] private TrackListDataSource _trackListDataSource;
    [SerializeField] private EditablePlaylistDataSource _editablePlaylistDataSource;

    private bool _subscribed;
    private bool _isCreating;
    private bool _isEditing;

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        EnsureUIHierarchyExists();
        UpdateUIVisibility(CurrentMode);
        TrySubscribe();
    }

    void Update()
    {
        if (!_subscribed) TrySubscribe();
    }

    void EnsureUIHierarchyExists()
    {
        // Find or create UIContainer
        GameObject uiContainer = GameObject.Find("UIContainer");
        if (uiContainer == null)
        {
            uiContainer = new GameObject("UIContainer");
        }

        // Create AlbumRolodex if missing
        if (_albumRolodex == null)
        {
            _albumRolodex = new GameObject("AlbumRolodex");
            _albumRolodex.transform.SetParent(uiContainer.transform);
            _albumRolodexController = _albumRolodex.AddComponent<RolodexController>();

            GameObject albumDataSourceObj = new GameObject("AlbumDataSource");
            albumDataSourceObj.transform.SetParent(uiContainer.transform);
            AlbumDataSource albumDataSource = albumDataSourceObj.AddComponent<AlbumDataSource>();
            _albumRolodexController.SetDataSource(albumDataSource);
        }

        // Create PlaylistRolodex if missing
        if (_playlistRolodex == null)
        {
            _playlistRolodex = new GameObject("PlaylistRolodex");
            _playlistRolodex.transform.SetParent(uiContainer.transform);
            _playlistRolodexController = _playlistRolodex.AddComponent<RolodexController>();

            GameObject playlistDataSourceObj = new GameObject("PlaylistDataSource");
            playlistDataSourceObj.transform.SetParent(uiContainer.transform);
            PlaylistDataSource playlistDataSource = playlistDataSourceObj.AddComponent<PlaylistDataSource>();
            _playlistRolodexController.SetDataSource(playlistDataSource);
        }

        // Create ListPanel if missing
        if (_listPanel == null)
        {
            _listPanel = new GameObject("ListPanel");
            _listPanel.transform.SetParent(uiContainer.transform);
            _listController = _listPanel.AddComponent<ListController>();

            GameObject trackDataSourceObj = new GameObject("TrackListDataSource");
            trackDataSourceObj.transform.SetParent(uiContainer.transform);
            _trackListDataSource = trackDataSourceObj.AddComponent<TrackListDataSource>();
            _listController.SetDataSource(_trackListDataSource);
        }

        // Create PlaylistPanel if missing
        if (_playlistPanel == null)
        {
            _playlistPanel = new GameObject("PlaylistPanel");
            _playlistPanel.transform.SetParent(uiContainer.transform);
            _playlistController = _playlistPanel.AddComponent<ListController>();

            GameObject editablePlaylistDataSourceObj = new GameObject("EditablePlaylistDataSource");
            editablePlaylistDataSourceObj.transform.SetParent(uiContainer.transform);
            _editablePlaylistDataSource = editablePlaylistDataSourceObj.AddComponent<EditablePlaylistDataSource>();
            _playlistController.SetDataSource(_editablePlaylistDataSource);
        }

        // Create NavigationUI if missing
        if (GameObject.Find("NavigationUI") == null)
        {
            GameObject navObj = new GameObject("NavigationUI");
            navObj.transform.SetParent(uiContainer.transform);
            navObj.AddComponent<TextMeshPro>();
            navObj.AddComponent<NavigationUI>();
        }

        Debug.Log("UI hierarchy ensured");
    }

    void TrySubscribe()
    {
        if (_subscribed || XRInputManager.Instance == null) return;
        XRInputManager.Instance.OnLeftTriggerPressed += ToggleMode;
        XRInputManager.Instance.OnBackPressed += ExitPanel;
        _subscribed = true;

        if (_albumRolodexController != null)
        {
            _albumRolodexController.SetActiveMode(Mode.Browse);
            _albumRolodexController.OnItemSelected += OnAlbumSelected;
        }

        if (_playlistRolodexController != null)
        {
            _playlistRolodexController.SetActiveMode(Mode.Review);
            _playlistRolodexController.OnItemSelected += OnPlaylistSelected;
            Debug.Log("ModeController subscribed to PlaylistRolodexController");
        }
        else
        {
            Debug.LogError("_playlistRolodexController is null");
        }

        if (_listController != null)
        {
            _listController.OnItemSelected += OnTrackSelected;
        }

        if (_playlistController != null)
        {
            _playlistController.OnItemSelected += OnPlaylistTrackSelected;
        }

        Debug.Log("ModeController subscribed to XRInputManager");
    }

    void OnAlbumSelected(int albumIndex)
    {
        Debug.Log($"OnAlbumSelected called with index: {albumIndex}");

        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager.Instance is null");
            return;
        }

        var albums = NetworkManager.Instance.GetAllAlbums();
        Debug.Log($"Total albums: {albums.Count}");

        if (albumIndex < 0 || albumIndex >= albums.Count)
        {
            Debug.LogError($"Album index {albumIndex} out of range");
            return;
        }

        Album selectedAlbum = albums[albumIndex];
        Debug.Log($"Selected album: {selectedAlbum.Title}, tracks: {selectedAlbum.Tracks.Count}");

        if (_trackListDataSource != null)
        {
            Debug.Log("Setting album on TrackListDataSource");
            _trackListDataSource.SetAlbum(selectedAlbum);
        }
        else
        {
            Debug.LogError("_trackListDataSource is null");
        }

        if (_listPanel != null)
        {
            _listPanel.SetActive(true);
            Debug.Log($"Showing tracks for album: {selectedAlbum.Title}");
        }
        else
        {
            Debug.LogError("_listPanel is null");
        }

        // Show playlist panel and populate with centered playlist
        if (_playlistPanel != null && _editablePlaylistDataSource != null)
        {
            int playlistIndex = _playlistRolodexController.CurrentIndex;
            var playlists = PlaylistManager.Instance.GetAllPlaylists();
            if (playlistIndex >= 0 && playlistIndex < playlists.Count)
            {
                _editablePlaylistDataSource.SetPlaylist(playlists[playlistIndex]);
                Debug.Log($"Playlist panel showing: {playlists[playlistIndex].Name}");
            }
            else
            {
                _editablePlaylistDataSource.Clear();
                Debug.Log("Playlist panel cleared (no valid playlist selected)");
            }
            _playlistPanel.SetActive(true);
        }

        _isCreating = true;
        UpdateUIVisibility(CurrentMode);
    }

    void ToggleMode()
    {
        Mode newMode = CurrentMode == Mode.Browse ? Mode.Review : Mode.Browse;
        SetMode(newMode);
    }

    public void SetMode(Mode newMode)
    {
        CurrentMode = newMode;
        Debug.Log($"Mode changed to: {newMode}");
        UpdateUIVisibility(newMode);
        OnModeSelected?.Invoke(newMode);
    }

    void UpdateUIVisibility(Mode mode)
    {
        bool showCarousels = !_isCreating && !_isEditing;
        bool showListPanel = _isCreating || _isEditing;
        bool showPlaylistPanel = _isCreating;

        Debug.Log($"UpdateUIVisibility: _isCreating={_isCreating}, _isEditing={_isEditing}, showPlaylistPanel={showPlaylistPanel}, _playlistPanel={_playlistPanel}");

        if (_albumRolodex != null)
            _albumRolodex.SetActive(showCarousels);

        if (_playlistRolodex != null)
            _playlistRolodex.SetActive(showCarousels);

        if (_listPanel != null)
            _listPanel.SetActive(showListPanel);

        if (_playlistPanel != null)
        {
            _playlistPanel.SetActive(showPlaylistPanel);
            Debug.Log($"PlaylistPanel.SetActive({showPlaylistPanel})");
        }
        else
        {
            Debug.LogWarning("_playlistPanel is null in UpdateUIVisibility");
        }

        Debug.Log($"UI visibility updated for mode: {mode}, carousels: {showCarousels}, listPanel: {showListPanel}, playlistPanel: {showPlaylistPanel}");
    }

    void OnPlaylistSelected(int playlistIndex)
    {
        Debug.Log($"OnPlaylistSelected called with index: {playlistIndex}");

        if (PlaylistManager.Instance == null)
        {
            Debug.LogError("PlaylistManager.Instance is null");
            return;
        }

        var playlists = PlaylistManager.Instance.GetAllPlaylists();
        Debug.Log($"Total playlists: {playlists.Count}");

        if (playlistIndex < 0 || playlistIndex >= playlists.Count)
        {
            Debug.LogError($"Playlist index {playlistIndex} out of range");
            return;
        }

        Playlist selectedPlaylist = playlists[playlistIndex];
        Debug.Log($"Selected playlist: {selectedPlaylist.Name}, tracks: {selectedPlaylist.Tracks.Count}");

        if (_editablePlaylistDataSource != null)
        {
            Debug.Log("Setting playlist on EditablePlaylistDataSource for editing");
            _editablePlaylistDataSource.SetPlaylist(selectedPlaylist);
            _listController.SetDataSource(_editablePlaylistDataSource);
        }

        if (_listPanel != null)
        {
            _listPanel.SetActive(true);
            Debug.Log($"Showing tracks for playlist: {selectedPlaylist.Name}");
        }
        else
        {
            Debug.LogError("_listPanel is null");
        }

        _isEditing = true;
        UpdateUIVisibility(CurrentMode);
    }

    public void ExitPanel()
    {
        _isCreating = false;
        _isEditing = false;
        UpdateUIVisibility(CurrentMode);
        Debug.Log("Exited Create/Edit mode");
    }

    void OnTrackSelected(int trackIndex)
    {
        if (_isCreating && _trackListDataSource != null && _editablePlaylistDataSource != null)
        {
            Track track = _trackListDataSource.GetTrack(trackIndex);
            if (track != null)
            {
                _editablePlaylistDataSource.AddTrack(track);
                Debug.Log($"Added track to playlist: {track.Title}");
            }
        }
        else if (_isEditing && _editablePlaylistDataSource != null)
        {
            Track track = _editablePlaylistDataSource.GetTrack(trackIndex);
            if (track != null)
            {
                _editablePlaylistDataSource.RemoveTrack(track);
                Debug.Log($"Removed track from playlist: {track.Title}");
            }
        }
    }

    void OnPlaylistTrackSelected(int trackIndex)
    {
        // Not currently used - OnTrackSelected handles both modes
    }

    void OnDestroy()
    {
        if (XRInputManager.Instance != null)
        {
            XRInputManager.Instance.OnLeftTriggerPressed -= ToggleMode;
            XRInputManager.Instance.OnBackPressed -= ExitPanel;
        }

        if (_albumRolodexController != null)
            _albumRolodexController.OnItemSelected -= OnAlbumSelected;

        if (_playlistRolodexController != null)
            _playlistRolodexController.OnItemSelected -= OnPlaylistSelected;

        if (_listController != null)
            _listController.OnItemSelected -= OnTrackSelected;

        if (_playlistController != null)
            _playlistController.OnItemSelected -= OnPlaylistTrackSelected;
    }
}
