using UnityEngine;
using System.Collections.Generic;

public class PlaylistManager : MonoBehaviour
{
    public static PlaylistManager Instance { get; private set; }

    public event System.Action<List<Playlist>> OnPlaylistsLoaded;

    [SerializeField] private bool _usePlaylistFiles = true;
    [SerializeField] private string _playlistPath = "\\\\HIS-BASE\\Music - FlacPlaylists";

    private List<Playlist> _playlists = new List<Playlist>();

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        Debug.Log("PlaylistManager initialized");
        if (_usePlaylistFiles)
        {
            if (NetworkManager.Instance != null)
            {
                List<Album> albums = NetworkManager.Instance.GetAllAlbums();
                if (albums.Count > 0)
                    LoadPlaylistsFromFiles(albums);
                else
                    NetworkManager.Instance.OnAlbumsLoaded += LoadPlaylistsFromFiles;
            }
            else
                LoadDummyPlaylists();
        }
        else
            LoadDummyPlaylists();
    }

    public List<Playlist> GetAllPlaylists() => _playlists;

    public void SavePlaylist(Playlist playlist)
    {
        Debug.Log($"PlaylistManager.SavePlaylist called: {playlist?.Name}, useFiles={_usePlaylistFiles}");

        if (_usePlaylistFiles)
        {
            Debug.Log($"Saving to file: {_playlistPath}");
            bool saved = PlaylistJsonSaver.SavePlaylist(playlist, _playlistPath);
            Debug.Log($"PlaylistJsonSaver returned: {saved}");

            Debug.Log($"MQTTManager.Instance = {MQTTManager.Instance}");
            if (MQTTManager.Instance != null)
            {
                Debug.Log($"Calling MQTTManager.PublishPlaylist...");
                MQTTManager.Instance.PublishPlaylist(playlist);
                Debug.Log($"Published playlist '{playlist.Name}' to Node-RED");
            }
            else
            {
                Debug.LogError("MQTTManager.Instance is NULL!");
            }
        }
        else
        {
            Debug.LogWarning("_usePlaylistFiles is false, not saving");
        }
    }

    void LoadPlaylistsFromFiles(List<Album> albums = null)
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("PlaylistManager: NetworkManager not initialized");
            LoadDummyPlaylists();
            return;
        }

        if (albums == null)
            albums = NetworkManager.Instance.GetAllAlbums();

        Debug.Log($"PlaylistManager: Found {albums.Count} albums for playlist matching");
        _playlists = PlaylistJsonLoader.LoadPlaylistsFromDirectory(_playlistPath, albums);

        if (_playlists.Count == 0)
        {
            Debug.LogWarning("No playlists loaded from files, using dummy playlists");
            LoadDummyPlaylists();
        }
        else
        {
            OnPlaylistsLoaded?.Invoke(_playlists);
        }
    }

    void LoadDummyPlaylists()
    {
        _playlists = new List<Playlist>
        {
            new Playlist {
                Name = "",
                Tracks = new List<Track>()
            },
            new Playlist {
                Name = "Favorites",
                Tracks = new List<Track>
                {
                    new Track { Title = "Bohemian Rhapsody" },
                    new Track { Title = "Imagine" },
                    new Track { Title = "Stairway to Heaven" },
                }
            },
            new Playlist {
                Name = "Road Trip",
                Tracks = new List<Track>
                {
                    new Track { Title = "Born to Run" },
                    new Track { Title = "Life is a Highway" },
                    new Track { Title = "Don't Stop Believin'" },
                    new Track { Title = "Livin' on a Prayer" },
                }
            },
            new Playlist {
                Name = "Workout",
                Tracks = new List<Track>
                {
                    new Track { Title = "Eye of the Tiger" },
                    new Track { Title = "Pump It Up" },
                    new Track { Title = "Another One Bites the Dust" },
                }
            },
            new Playlist {
                Name = "Chill Vibes",
                Tracks = new List<Track>
                {
                    new Track { Title = "Nuvole Bianche" },
                    new Track { Title = "Weightless" },
                }
            },
            new Playlist {
                Name = "Party Mix",
                Tracks = new List<Track>
                {
                    new Track { Title = "Uptown Funk" },
                    new Track { Title = "Shut Up and Dance" },
                    new Track { Title = "Wonderwall" },
                    new Track { Title = "Mr. Brightside" },
                    new Track { Title = "Don't You (Forget About Me)" },
                }
            },
        };

        OnPlaylistsLoaded?.Invoke(_playlists);
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnAlbumsLoaded -= LoadPlaylistsFromFiles;
    }
}
