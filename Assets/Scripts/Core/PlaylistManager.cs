using UnityEngine;
using System.Collections.Generic;

public class PlaylistManager : MonoBehaviour
{
    public static PlaylistManager Instance { get; private set; }

    public event System.Action<List<Playlist>> OnPlaylistsLoaded;

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        Debug.Log("PlaylistManager initialized");
    }
}

public class Playlist
{
    public string Name { get; set; }
    public List<Track> Tracks { get; set; } = new List<Track>();
}

public class Track
{
    public string Title { get; set; }
    public string Artist { get; set; }
}
