using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;

public class PlaylistFileSaver
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

        string fileName = SanitizeFileName(playlist.Name) + ".wpl";
        string filePath = Path.Combine(playlistPath, fileName);

        try
        {
            XmlDocument doc = new XmlDocument();
            XmlDeclaration decl = doc.CreateXmlDeclaration("1.0", null, null);
            doc.AppendChild(decl);

            XmlElement smil = doc.CreateElement("smil");
            doc.AppendChild(smil);

            XmlElement head = doc.CreateElement("head");
            smil.AppendChild(head);

            XmlElement metaGenerator = doc.CreateElement("meta");
            metaGenerator.SetAttribute("name", "Generator");
            metaGenerator.SetAttribute("content", "MusicBox 2.0");
            head.AppendChild(metaGenerator);

            XmlElement metaItemCount = doc.CreateElement("meta");
            metaItemCount.SetAttribute("name", "ItemCount");
            metaItemCount.SetAttribute("content", playlist.Tracks.Count.ToString());
            head.AppendChild(metaItemCount);

            XmlElement title = doc.CreateElement("title");
            title.AppendChild(doc.CreateTextNode(playlist.Name));
            head.AppendChild(title);

            XmlElement body = doc.CreateElement("body");
            smil.AppendChild(body);

            XmlElement seq = doc.CreateElement("seq");
            body.AppendChild(seq);

            foreach (Track track in playlist.Tracks)
            {
                XmlElement media = doc.CreateElement("media");
                media.SetAttribute("src", track.Uri);
                seq.AppendChild(media);
            }

            doc.Save(filePath);
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
}
