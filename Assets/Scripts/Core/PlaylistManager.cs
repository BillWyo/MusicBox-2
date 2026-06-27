using UnityEngine;
using System.Collections.Generic;

public class PlaylistManager : MonoBehaviour
{
    public static PlaylistManager Instance { get; private set; }

    public event System.Action<List<Playlist>> OnPlaylistsLoaded;

    private List<Playlist> _playlists = new List<Playlist>();

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        Debug.Log("PlaylistManager initialized");
        LoadDummyPlaylists();
    }

    public List<Playlist> GetAllPlaylists() => _playlists;

    void LoadDummyPlaylists()
    {
        _playlists = new List<Playlist>
        {
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
}
