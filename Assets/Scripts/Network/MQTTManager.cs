using UnityEngine;
using System.Collections.Generic;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

public class MQTTManager : MonoBehaviour
{
    public static MQTTManager Instance { get; private set; }

    [SerializeField] private string _brokerAddress = "192.168.1.18";
    [SerializeField] private int _brokerPort = 1883;

    private MqttClient _client;
    private bool _isConnected;

    void Awake()
    {
        if (Instance != null) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        try
        {
            _client = new MqttClient(_brokerAddress, _brokerPort, false, null, null, MqttSslProtocols.None);
            _client.Connect(System.Guid.NewGuid().ToString());
            _isConnected = true;
            Debug.Log($"MQTT connected to {_brokerAddress}:{_brokerPort}");
        }
        catch (System.Exception e)
        {
            _isConnected = false;
            Debug.LogError($"MQTT connection failed: {e.Message}");
        }
    }

    public void PublishPlaylist(Playlist playlist, bool isNew = false)
    {
        try
        {
            if (!_isConnected || _client == null)
            {
                Debug.LogError("MQTT not connected, cannot publish");
                return;
            }

            string json = PlaylistToJson(playlist);
            string topic = "home/music/playlists/create";

            _client.Publish(topic, Encoding.UTF8.GetBytes(json), MqttMsgBase.QOS_LEVEL_1, true);
            Debug.Log($"[MQTT PUBLISHED] Topic: {topic}, Playlist: {playlist.Name}");
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
            if (!_isConnected || _client == null)
            {
                Debug.LogError("MQTT not connected, cannot publish");
                return;
            }

            string json = PlaylistListToJson(playlists);
            string topic = "home/music/playlists/list";

            _client.Publish(topic, Encoding.UTF8.GetBytes(json), MqttMsgBase.QOS_LEVEL_1, true);
            Debug.Log($"[MQTT PUBLISHED] Topic: {topic}");
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
                string trackJson = $"{{\"title\":\"{EscapeJson(track.Title)}\",\"artist\":\"{EscapeJson(track.Artist)}\",\"album\":\"{EscapeJson(track.Album)}\",\"uri\":\"{EscapeJson(track.Uri)}\"}}";
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

    void OnDestroy()
    {
        if (_client != null && _client.IsConnected)
        {
            _client.Disconnect();
        }
    }

}
