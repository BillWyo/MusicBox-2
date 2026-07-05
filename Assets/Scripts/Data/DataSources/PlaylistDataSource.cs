using UnityEngine;
using System.Collections.Generic;

public class PlaylistDataSource : MonoBehaviour, ITileDataSource
{
    private List<Playlist> _playlists = new List<Playlist>();

    public event System.Action OnDataChanged;

    public int Count => _playlists.Count;

    void Start()
    {
        if (PlaylistManager.Instance != null)
        {
            // Query current data (in case event already fired)
            _playlists = PlaylistManager.Instance.GetAllPlaylists();
            if (_playlists.Count > 0)
            {
                OnDataChanged?.Invoke();
            }
            // Still subscribe for future changes
            PlaylistManager.Instance.OnPlaylistsLoaded += OnPlaylistsLoaded;
        }
    }

    void OnPlaylistsLoaded(List<Playlist> playlists)
    {
        _playlists = new List<Playlist>();

        // Add blank card at index 0 (create new)
        Playlist blankCard = new Playlist
        {
            Name = "",
            Tracks = new List<Track>()
        };
        _playlists.Add(blankCard);

        // Add all loaded playlists
        _playlists.AddRange(playlists);

        OnDataChanged?.Invoke();
    }

    public string GetTitle(int index)
    {
        if (index < 0 || index >= _playlists.Count) return "";
        if (index == 0) return "+ NEW PLAYLIST";
        return _playlists[index].Name;
    }

    public string GetSubtitle(int index)
    {
        if (index < 0 || index >= _playlists.Count) return "";
        if (index == 0) return "Create new";
        return $"{_playlists[index].Tracks.Count} tracks";
    }

    public bool IsBlankCard(int index)
    {
        return index == 0;
    }

    public Playlist GetPlaylist(int index)
    {
        if (index < 0 || index >= _playlists.Count) return null;
        return _playlists[index];
    }

    public Sprite GetArt(int index)
    {
        return null;
    }

    void OnDestroy()
    {
        if (PlaylistManager.Instance != null)
            PlaylistManager.Instance.OnPlaylistsLoaded -= OnPlaylistsLoaded;
    }
}
