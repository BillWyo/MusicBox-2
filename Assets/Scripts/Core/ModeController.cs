using UnityEngine;
using TMPro;

public class ModeController : MonoBehaviour
{
    public static ModeController Instance { get; private set; }

    public enum Mode { Browse, Review }
    public Mode CurrentMode { get; private set; } = Mode.Browse;

    public event System.Action<Mode> OnModeSelected;

    [SerializeField] private GameObject _albumRolodex;
    [SerializeField] private GameObject _playlistRolodex;
    [SerializeField] private GameObject _listPanel;
    [SerializeField] private RolodexController _albumRolodexController;
    [SerializeField] private RolodexController _playlistRolodexController;
    [SerializeField] private ListController _listController;
    [SerializeField] private TrackListDataSource _trackListDataSource;

    private bool _subscribed;

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
            albumDataSourceObj.AddComponent<AlbumDataSource>();
        }

        // Create PlaylistRolodex if missing
        if (_playlistRolodex == null)
        {
            _playlistRolodex = new GameObject("PlaylistRolodex");
            _playlistRolodex.transform.SetParent(uiContainer.transform);
            _playlistRolodexController = _playlistRolodex.AddComponent<RolodexController>();

            GameObject playlistDataSourceObj = new GameObject("PlaylistDataSource");
            playlistDataSourceObj.transform.SetParent(uiContainer.transform);
            playlistDataSourceObj.AddComponent<PlaylistDataSource>();
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
        _subscribed = true;

        if (_albumRolodexController != null)
        {
            _albumRolodexController.OnItemSelected += OnAlbumSelected;
        }

        if (_playlistRolodexController != null)
        {
            _playlistRolodexController.OnItemSelected += OnPlaylistSelected;
            Debug.Log("ModeController subscribed to PlaylistRolodexController");
        }
        else
        {
            Debug.LogError("_playlistRolodexController is null");
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
        if (_albumRolodex != null)
            _albumRolodex.SetActive(mode == Mode.Browse);

        if (_playlistRolodex != null)
            _playlistRolodex.SetActive(mode == Mode.Review);

        if (_listPanel != null)
            _listPanel.SetActive(false);

        Debug.Log($"UI visibility updated for mode: {mode}");
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

        if (_trackListDataSource != null)
        {
            Debug.Log("Setting playlist tracks on TrackListDataSource");
            _trackListDataSource.SetTracks(selectedPlaylist.Tracks);
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
    }

    void OnDestroy()
    {
        if (XRInputManager.Instance != null)
            XRInputManager.Instance.OnLeftTriggerPressed -= ToggleMode;

        if (_albumRolodexController != null)
            _albumRolodexController.OnItemSelected -= OnAlbumSelected;

        if (_playlistRolodexController != null)
            _playlistRolodexController.OnItemSelected -= OnPlaylistSelected;
    }
}
