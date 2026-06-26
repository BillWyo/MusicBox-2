using UnityEngine;

public interface ITileDataSource
{
    int Count { get; }
    string GetTitle(int index);
    string GetSubtitle(int index);
    Sprite GetArt(int index);
}
