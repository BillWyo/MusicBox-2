using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class MusicLibraryScanner
{
    public static List<Album> ScanMusicLibrary(string libraryPath)
    {
        List<Album> albums = new List<Album>();

        if (!Directory.Exists(libraryPath))
        {
            Debug.LogError($"Music library path not found: {libraryPath}");
            return albums;
        }

        DirectoryInfo libraryDir = new DirectoryInfo(libraryPath);

        foreach (DirectoryInfo artistDir in libraryDir.GetDirectories())
        {
            foreach (DirectoryInfo albumDir in artistDir.GetDirectories())
            {
                Album album = ScanAlbumFolder(artistDir.Name, albumDir);
                if (album != null)
                {
                    albums.Add(album);
                }
            }
        }

        Debug.Log($"MusicLibraryScanner: Loaded {albums.Count} albums from {libraryPath}");
        return albums;
    }

    static Album ScanAlbumFolder(string artistName, DirectoryInfo albumDir)
    {
        // Get album tracks
        FileInfo[] flacFiles = albumDir.GetFiles("*.flac");
        if (flacFiles.Length == 0)
            return null;

        // Check for art
        FileInfo artFile = albumDir.GetFiles("Folder.jpg").FirstOrDefault();

        List<Track> tracks = new List<Track>();
        foreach (FileInfo flacFile in flacFiles.OrderBy(f => f.Name))
        {
            tracks.Add(new Track
            {
                Title = Path.GetFileNameWithoutExtension(flacFile.Name),
                Artist = artistName,
                Album = albumDir.Name,
                Uri = flacFile.FullName
            });
        }

        Album album = new Album
        {
            Title = albumDir.Name,
            Artist = artistName,
            ArtPath = artFile?.FullName,
            HasArt = artFile != null,
            Tracks = tracks
        };

        return album;
    }
}
