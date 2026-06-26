using System.Collections.Generic;

public class Album
{
    public string Title { get; set; }
    public string Artist { get; set; }
    public string ArtUrl { get; set; }
    public List<Track> Tracks { get; set; } = new List<Track>();
}
