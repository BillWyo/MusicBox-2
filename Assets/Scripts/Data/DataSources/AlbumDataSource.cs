using UnityEngine;
using System.Collections.Generic;

public class AlbumDataSource : MonoBehaviour, ITileDataSource
{
    private List<Album> _albums = new List<Album>();

    public event System.Action OnDataChanged;

    public int Count => _albums.Count;

    void Start()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnAlbumsLoaded += OnAlbumsLoaded;
    }

    void OnAlbumsLoaded(List<Album> albums)
    {
        _albums = albums;
        OnDataChanged?.Invoke();
    }

    public string GetTitle(int index)
    {
        if (index < 0 || index >= _albums.Count) return "";
        return _albums[index].Title;
    }

    public string GetSubtitle(int index)
    {
        if (index < 0 || index >= _albums.Count) return "";
        return _albums[index].Artist;
    }

    public Sprite GetArt(int index)
    {
        return null;
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnAlbumsLoaded -= OnAlbumsLoaded;
    }
}
