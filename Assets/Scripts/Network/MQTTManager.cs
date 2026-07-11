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
            if (playlist == null)
            {
                Debug.LogError("MQTTManager.PublishPlaylist: playlist is null");
                return;
            }

            bool success = PlaylistJsonSaver.SavePlaylist(playlist, _playlistPath);
            if (success)
            {
                Debug.Log($"[SAVED] Playlist '{playlist.Name}' ({playlist.Tracks.Count} tracks) to {_playlistPath}");
            }
            else
            {
                Debug.LogError($"[FAILED] PlaylistJsonSaver returned false for '{playlist.Name}'");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ERROR] Failed to save playlist: {e.Message}\n{e.StackTrace}");
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
