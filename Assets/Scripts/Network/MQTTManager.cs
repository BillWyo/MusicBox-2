using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class MQTTManager : MonoBehaviour
{
    public static MQTTManager Instance { get; private set; }

    [SerializeField] private string _brokerAddress = "192.168.1.18";
    [SerializeField] private int _brokerPort = 1883;

    private bool _isConnected;

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        // TODO: Implement MQTT connection when M2Mqtt library is available
        _isConnected = true;
        Debug.Log($"MQTT Manager ready (stub mode - library not yet integrated)");
    }

    public void PublishPlaylist(Playlist playlist, bool isNew = false)
    {
        try
        {
            // Publish playlist data as JSON to Node-RED
            string json = PlaylistToJson(playlist);
            string topic = "home/music/playlists/create";

            // TODO: Replace with actual M2Mqtt publish when library available
            Debug.Log($"[MQTT PUBLISH] Topic: {topic}");
            Debug.Log($"[MQTT PUBLISH] Payload: {json}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MQTT publish failed: {e.Message}");
        }
    }

    public void PublishPlaylistList(List<Playlist> playlists)
    {
        try
        {
            string json = PlaylistListToJson(playlists);
            Debug.Log($"[MQTT PUBLISH] Topic: home/music/playlists/list");
            Debug.Log($"[MQTT PUBLISH] Payload: {json}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MQTT publish failed: {e.Message}");
        }
    }

    string PlaylistToJson(Playlist playlist)
    {
        // Simple JSON serialization
        string tracksJson = "[]";
        if (playlist.Tracks.Count > 0)
        {
            List<string> trackObjs = new List<string>();
            foreach (Track track in playlist.Tracks)
            {
                string trackJson = $"{{\"title\":\"{EscapeJson(track.Title)}\",\"artist\":\"{EscapeJson(track.Artist)}\",\"uri\":\"{EscapeJson(track.Uri)}\"}}";
                trackObjs.Add(trackJson);
            }
            tracksJson = "[" + string.Join(",", trackObjs) + "]";
        }

        return $"{{\"name\":\"{EscapeJson(playlist.Name)}\",\"tracks\":{tracksJson}}}";
    }

    string PlaylistListToJson(List<Playlist> playlists)
    {
        List<string> playlistObjs = new List<string>();
        foreach (Playlist p in playlists)
        {
            playlistObjs.Add($"{{\"name\":\"{EscapeJson(p.Name)}\",\"trackCount\":{p.Tracks.Count}}}");
        }
        return "[" + string.Join(",", playlistObjs) + "]";
    }

    string EscapeJson(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        return str.Replace("\"", "\\\"").Replace("\\", "\\\\").Replace("\n", "\\n");
    }

}
