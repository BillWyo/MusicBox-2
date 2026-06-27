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
        _playlists = playlists;
        OnDataChanged?.Invoke();
    }

    public string GetTitle(int index)
    {
        if (index < 0 || index >= _playlists.Count) return "";
        return _playlists[index].Name;
    }

    public string GetSubtitle(int index)
    {
        if (index < 0 || index >= _playlists.Count) return "";
        return $"{_playlists[index].Tracks.Count} tracks";
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
