using UnityEngine;

public interface IListDataSource
{
    int Count { get; }
    string GetTitle(int index);
    string GetSubtitle(int index);
    Sprite GetArt(int index);
    event System.Action OnDataChanged;
}
