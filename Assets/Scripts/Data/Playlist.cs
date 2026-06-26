using System.Collections.Generic;

public class Playlist
{
    public string Name { get; set; }
    public List<Track> Tracks { get; set; } = new List<Track>();
}
