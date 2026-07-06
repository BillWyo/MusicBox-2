using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;

public class PlaylistFileScanner
{
    public static List<Playlist> ScanPlaylistDirectory(string playlistPath, List<Album> albums)
    {
        List<Playlist> playlists = new List<Playlist>();

        if (!Directory.Exists(playlistPath))
        {
            Debug.LogError($"Playlist directory not found: {playlistPath}");
            return playlists;
        }

        DirectoryInfo playlistDir = new DirectoryInfo(playlistPath);
        FileInfo[] wplFiles = playlistDir.GetFiles("*.wpl");

        foreach (FileInfo wplFile in wplFiles)
        {
            Playlist playlist = ParseWplFile(wplFile.FullName, albums);
            if (playlist != null)
            {
                playlists.Add(playlist);
                Debug.Log($"Loaded playlist: {playlist.Name} with {playlist.Tracks.Count} tracks");
            }
        }

        Debug.Log($"PlaylistFileScanner: Loaded {playlists.Count} playlists from {playlistPath}");
        return playlists;
    }

    static Playlist ParseWplFile(string filePath, List<Album> albums)
    {
        try
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            // Get playlist title
            XmlNodeList titleNodes = doc.GetElementsByTagName("title");
            string playlistName = "Untitled";
            if (titleNodes.Count > 0 && titleNodes[0].FirstChild != null)
            {
                playlistName = titleNodes[0].FirstChild.Value;
            }

            // Get track paths
            List<Track> tracks = new List<Track>();
            XmlNodeList mediaNodes = doc.GetElementsByTagName("media");

            foreach (XmlNode mediaNode in mediaNodes)
            {
                XmlAttribute srcAttr = mediaNode.Attributes?["src"];
                if (srcAttr != null)
                {
                    string trackPath = srcAttr.Value;
                    Track track = FindTrackByPath(trackPath, albums);
                    if (track != null)
                    {
                        tracks.Add(track);
                    }
                    else
                    {
                        Debug.LogWarning($"Track not found in library: {trackPath}");
                    }
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
            Debug.LogError($"Error parsing playlist file {filePath}: {e.Message}");
            return null;
        }
    }

    static Track FindTrackByPath(string trackPath, List<Album> albums)
    {
        foreach (Album album in albums)
        {
            foreach (Track track in album.Tracks)
            {
                if (track.Uri == trackPath)
                    return track;
            }
        }
        return null;
    }
}
