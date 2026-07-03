using UnityEngine;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    public event System.Action<List<Album>> OnAlbumsLoaded;

    [SerializeField] private bool _useMusicLibrary = false;
    [SerializeField] private string _musicLibraryPath = "F:\\Music - Flac";

    private List<Album> _albums = new List<Album>();

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        Debug.Log("NetworkManager initialized");
        if (_useMusicLibrary)
            LoadMusicLibrary();
        else
            LoadDummyAlbums();
    }

    public List<Album> GetAllAlbums() => _albums;

    void LoadMusicLibrary()
    {
        _albums = MusicLibraryScanner.ScanMusicLibrary(_musicLibraryPath);
        OnAlbumsLoaded?.Invoke(_albums);
    }

    void LoadDummyAlbums()
    {
        List<Album> albums = new List<Album>
        {
            new Album {
                Title = "Abbey Road",
                Artist = "The Beatles",
                Tracks = new List<Track>
                {
                    new Track { Title = "Come Together" },
                    new Track { Title = "Something" },
                    new Track { Title = "Maxwell's Silver Hammer" },
                }
            },
            new Album {
                Title = "Dark Side of the Moon",
                Artist = "Pink Floyd",
                Tracks = new List<Track>
                {
                    new Track { Title = "Speak to Me" },
                    new Track { Title = "Breathe" },
                    new Track { Title = "On the Run" },
                    new Track { Title = "Time" },
                }
            },
            new Album {
                Title = "Led Zeppelin IV",
                Artist = "Led Zeppelin",
                Tracks = new List<Track>
                {
                    new Track { Title = "Black Dog" },
                    new Track { Title = "Rock and Roll" },
                    new Track { Title = "Stairway to Heaven" },
                }
            },
            new Album {
                Title = "Hotel California",
                Artist = "Eagles",
                Tracks = new List<Track>
                {
                    new Track { Title = "Hotel California" },
                    new Track { Title = "New Kid in Town" },
                }
            },
            new Album {
                Title = "Rumours",
                Artist = "Fleetwood Mac",
                Tracks = new List<Track>
                {
                    new Track { Title = "Second Hand News" },
                    new Track { Title = "Dreams" },
                    new Track { Title = "Don't Stop" },
                }
            },
            new Album {
                Title = "The Wall",
                Artist = "Pink Floyd",
                Tracks = new List<Track>
                {
                    new Track { Title = "In the Flesh?" },
                    new Track { Title = "The Happiest Days of Our Lives" },
                    new Track { Title = "Another Brick in the Wall" },
                }
            },
            new Album {
                Title = "Born to Run",
                Artist = "Bruce Springsteen",
                Tracks = new List<Track>
                {
                    new Track { Title = "Jungleland" },
                    new Track { Title = "Born to Run" },
                }
            },
            new Album {
                Title = "Purple",
                Artist = "Deep Purple",
                Tracks = new List<Track>
                {
                    new Track { Title = "Smoke on the Water" },
                    new Track { Title = "Hush" },
                }
            },
            new Album {
                Title = "OK Computer",
                Artist = "Radiohead",
                Tracks = new List<Track>
                {
                    new Track { Title = "Paranoid Android" },
                    new Track { Title = "Karma Police" },
                    new Track { Title = "Lucky" },
                }
            },
            new Album {
                Title = "In Rainbows",
                Artist = "Radiohead",
                Tracks = new List<Track>
                {
                    new Track { Title = "15 Step" },
                    new Track { Title = "Bodysnatchers" },
                    new Track { Title = "Nude" },
                }
            },
        };

        _albums = albums;
        OnAlbumsLoaded?.Invoke(albums);
    }
}
