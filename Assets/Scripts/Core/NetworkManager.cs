using UnityEngine;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    public event System.Action<List<Album>> OnAlbumsLoaded;

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        Debug.Log("NetworkManager initialized");
        LoadDummyAlbums();
    }

    void LoadDummyAlbums()
    {
        List<Album> albums = new List<Album>
        {
            new Album { Title = "Abbey Road", Artist = "The Beatles" },
            new Album { Title = "Dark Side of the Moon", Artist = "Pink Floyd" },
            new Album { Title = "Led Zeppelin IV", Artist = "Led Zeppelin" },
            new Album { Title = "Hotel California", Artist = "Eagles" },
            new Album { Title = "Rumours", Artist = "Fleetwood Mac" },
            new Album { Title = "The Wall", Artist = "Pink Floyd" },
            new Album { Title = "Born to Run", Artist = "Bruce Springsteen" },
            new Album { Title = "Purple", Artist = "Deep Purple" },
            new Album { Title = "Paranoid Android", Artist = "Radiohead" },
            new Album { Title = "OK Computer", Artist = "Radiohead" },
        };

        OnAlbumsLoaded?.Invoke(albums);
    }
}
