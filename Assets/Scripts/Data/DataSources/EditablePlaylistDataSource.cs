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
            OnDataChanged?.Invoke();
        }
    }

    public void Clear()
    {
        _tracks.Clear();
        _playlist = null;
        OnDataChanged?.Invoke();
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
}
