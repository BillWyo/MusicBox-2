using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class PlaylistJsonLoader
{
    public static List<Playlist> LoadPlaylistsFromDirectory(string playlistPath, List<Album> albums)
    {
        List<Playlist> playlists = new List<Playlist>();

        if (!Directory.Exists(playlistPath))
        {
            Debug.LogError($"Playlist directory not found: {playlistPath}");
            return playlists;
        }

        DirectoryInfo playlistDir = new DirectoryInfo(playlistPath);
        FileInfo[] jsonFiles = playlistDir.GetFiles("*.json");

        foreach (FileInfo jsonFile in jsonFiles)
        {
            Playlist playlist = LoadPlaylistFromJson(jsonFile.FullName, albums);
            if (playlist != null && playlist.Tracks.Count > 0)
            {
                playlists.Add(playlist);
                Debug.Log($"Loaded playlist: {playlist.Name} with {playlist.Tracks.Count} tracks");
            }
            else if (playlist != null)
            {
                Debug.LogWarning($"Playlist {playlist.Name} has 0 matching tracks");
            }
        }

        Debug.Log($"PlaylistJsonLoader: Loaded {playlists.Count} playlists from {playlistPath}");
        return playlists;
    }

    static Playlist LoadPlaylistFromJson(string filePath, List<Album> albums)
    {
        try
        {
            string jsonText = File.ReadAllText(filePath);

            // Parse JSON manually (simple parser for our format)
            string playlistName = ExtractJsonString(jsonText, "name");
            if (string.IsNullOrEmpty(playlistName)) playlistName = "Untitled";

            List<Track> tracks = new List<Track>();

            // Extract tracks array
            int tracksStart = jsonText.IndexOf("\"tracks\"");
            if (tracksStart >= 0)
            {
                int arrayStart = jsonText.IndexOf('[', tracksStart);
                int arrayEnd = FindMatchingBracket(jsonText, arrayStart);
                string tracksJson = jsonText.Substring(arrayStart, arrayEnd - arrayStart + 1);

                // Parse each track object
                int pos = 0;
                int objStart = tracksJson.IndexOf('{', pos);
                while (objStart >= 0)
                {
                    int objEnd = FindMatchingBracket(tracksJson, objStart);
                    string trackJson = tracksJson.Substring(objStart, objEnd - objStart + 1);

                    string title = ExtractJsonString(trackJson, "title");
                    string artist = ExtractJsonString(trackJson, "album");
                    string uri = ExtractJsonString(trackJson, "uri");

                    // Match track to library by title
                    Track matchedTrack = FindTrackInLibrary(title, artist, albums);
                    if (matchedTrack != null)
                    {
                        // Use the HTTP URI from playlist for playback
                        matchedTrack.Uri = uri;
                        tracks.Add(matchedTrack);
                    }
                    else
                    {
                        Debug.LogWarning($"Track not found in library: {title}");
                    }

                    pos = objEnd + 1;
                    objStart = tracksJson.IndexOf('{', pos);
                }
            }

            Playlist playlist = new Playlist
            {
                Name = playlistName,
                Tracks = tracks
            };

            return playlist;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading playlist from {filePath}: {e.Message}");
            return null;
        }
    }

    static string ExtractJsonString(string json, string key)
    {
        string pattern = $"\"{key}\"\\s*:\\s*\"([^\"\\\\]*(?:\\\\.[^\"\\\\]*)*)\"";
        Match match = Regex.Match(json, pattern);
        if (match.Success)
        {
            string value = match.Groups[1].Value;
            // Unescape JSON string
            value = value.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n").Replace("\\t", "\t");
            return value;
        }
        return "";
    }

    static int FindMatchingBracket(string text, int openPos)
    {
        int count = 1;
        for (int i = openPos + 1; i < text.Length; i++)
        {
            if (text[i] == '{') count++;
            else if (text[i] == '}') count--;
            if (count == 0) return i;
        }
        return text.Length - 1;
    }

    static Track FindTrackInLibrary(string trackTitle, string artistOrAlbum, List<Album> albums)
    {
        // Unescape HTML entities
        string unescapedTitle = UnescapeHtmlEntities(trackTitle);
        Debug.Log($"Matching track: '{trackTitle}' -> '{unescapedTitle}'");

        foreach (Album album in albums)
        {
            foreach (Track track in album.Tracks)
            {
                // Match by title (case-insensitive)
                if (track.Title.Equals(unescapedTitle, System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"  ✓ Found in album '{album.Title}': {track.Title}");
                    return track;
                }
            }
        }
        Debug.Log($"  ✗ Not found");
        return null;
    }

    static string UnescapeHtmlEntities(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return System.Net.WebUtility.HtmlDecode(text);
    }
}
