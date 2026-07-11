using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.IO;

public class MQTTManager : MonoBehaviour
{
    public static MQTTManager Instance { get; private set; }

    private string _playlistPath = "\\\\HIS-BASE\\Music - FlacPlaylists";

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        Debug.Log("MQTTManager ready (writes playlists to network drive)");
    }

    public void PublishPlaylist(Playlist playlist, bool isNew = false)
    {
        try
        {
            PlaylistJsonSaver.SavePlaylist(playlist, _playlistPath);
            Debug.Log($"[SAVED] Playlist '{playlist.Name}' written to {_playlistPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save playlist: {e.Message}");
        }
    }

    public void PublishPlaylistList(List<Playlist> playlists)
    {
        try
        {
            // List is implicitly published via individual playlist saves
            Debug.Log($"Playlist list will be saved as individual files");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save playlist list: {e.Message}");
        }
    }

}
