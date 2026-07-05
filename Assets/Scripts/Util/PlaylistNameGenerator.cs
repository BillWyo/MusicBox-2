using UnityEngine;

public static class PlaylistNameGenerator
{
    private static string[] _birdNames = new string[]
    {
        "Albatross", "Blackbird", "Cardinal", "Dove", "Eagle", "Falcon", "Goldfinch",
        "Hawk", "Ibis", "Jay", "Kestrel", "Lark", "Magpie", "Nightingale", "Osprey",
        "Parrot", "Quail", "Raven", "Sparrow", "Thrush", "Uguisu", "Vulture", "Warbler",
        "Xantus", "Yellowthroat", "Zebra Finch"
    };

    public static string GenerateName()
    {
        string bird = _birdNames[Random.Range(0, _birdNames.Length)];
        return $"{bird}'s Mix";
    }
}
