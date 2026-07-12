using UnityEngine;
using System.Collections.Generic;

public class EditablePlaylistDataSource : MonoBehaviour, IListDataSource
{
    private List<Track> _tracks = new List<Track>();
    private Playlist _playlist;

    public event System.Action OnDataChanged;

    public int Count => _tracks.Count;

    public void SetPlaylist(Playlist playlist)
    {
        _playlist = playlist;
        if (playlist != null)
            _tracks = new List<Track>(playlist.Tracks);
        else
            _tracks.Clear();
        OnDataChanged?.Invoke();
    }

    public void AddTrack(Track track)
    {
        if (!_tracks.Contains(track))
        {
            _tracks.Add(track);
            OnDataChanged?.Invoke();
        }
    }

    public void RemoveTrack(Track track)
    {
        if (_tracks.Remove(track))
        {
            Debug.Log($"Track removed: {track.Title}, remaining: {_tracks.Count}");
            OnDataChanged?.Invoke();
        }
        else
        {
            Debug.Log($"Track not found in playlist: {track.Title}");
        }
    }

    public void Clear()
    {
        _tracks.Clear();
        _playlist = null;
        OnDataChanged?.Invoke();
    }

    public Track GetTrack(int index)
    {
        if (index < 0 || index >= _tracks.Count) return null;
        return _tracks[index];
    }

    public string GetTitle(int index)
    {
        if (index < 0 || index >= _tracks.Count) return "";
        return _tracks[index].Title;
    }

    public string GetSubtitle(int index)
    {
        if (index < 0 || index >= _tracks.Count) return "";
        return _tracks[index].Artist;
    }

    public Sprite GetArt(int index)
    {
        return null;
    }

    public void SyncToPlaylist()
    {
        if (_playlist != null)
        {
            _playlist.Tracks = new List<Track>(_tracks);
            Debug.Log($"Synced {_tracks.Count} tracks to playlist '{_playlist.Name}'");
        }
    }
}
