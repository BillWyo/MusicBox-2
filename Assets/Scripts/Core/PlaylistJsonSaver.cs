using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class PlaylistJsonSaver
{
    public static bool SavePlaylist(Playlist playlist, string playlistPath)
    {
        if (string.IsNullOrEmpty(playlist.Name) || playlist.Name.Trim().Length == 0)
        {
            Debug.LogWarning("Cannot save playlist with empty name");
            return false;
        }

        if (!Directory.Exists(playlistPath))
        {
            try
            {
                Directory.CreateDirectory(playlistPath);
                Debug.Log($"Created playlist directory: {playlistPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create playlist directory: {e.Message}");
                return false;
            }
        }

        string fileName = SanitizeFileName(playlist.Name) + ".json";
        string filePath = Path.Combine(playlistPath, fileName);

        try
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"name\": \"{EscapeJsonString(playlist.Name)}\",");
            sb.AppendLine("  \"tracks\": [");

            for (int i = 0; i < playlist.Tracks.Count; i++)
            {
                Track track = playlist.Tracks[i];
                sb.Append("    {\n");
                sb.Append($"      \"title\": \"{EscapeJsonString(track.Title)}\",\n");
                sb.Append($"      \"artist\": \"{EscapeJsonString(track.Artist ?? "")}\",\n");
                sb.Append($"      \"album\": \"{EscapeJsonString(track.Album ?? "")}\",\n");
                sb.Append($"      \"uri\": \"{EscapeJsonString(track.Uri ?? "")}\"\n");
                sb.Append("    }");
                if (i < playlist.Tracks.Count - 1) sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
            Debug.Log($"Playlist saved: {filePath} ({playlist.Tracks.Count} tracks)");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save playlist: {e.Message}");
            return false;
        }
    }

    static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }

    static string EscapeJsonString(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\t", "\\t");
    }
}
