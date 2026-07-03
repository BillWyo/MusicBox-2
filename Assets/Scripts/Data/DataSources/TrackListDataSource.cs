using UnityEngine;
using System.Collections.Generic;

public class TrackListDataSource : MonoBehaviour, IListDataSource
{
    private List<Track> _tracks = new List<Track>();

    public event System.Action OnDataChanged;

    public int Count => _tracks.Count;

    public void SetAlbum(Album album)
    {
        SetTracks(album?.Tracks ?? new List<Track>());
    }

    public void SetTracks(List<Track> tracks)
    {
        _tracks = tracks ?? new List<Track>();
        Debug.Log($"TrackListDataSource.SetTracks: loaded {_tracks.Count} tracks");
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
        return "";
    }

    public Sprite GetArt(int index)
    {
        return null;
    }
}
