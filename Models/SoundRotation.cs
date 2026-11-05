namespace CustomSoundboard.Models;

/// <summary>
/// The recordings a tile takes turns playing. Each tap gets the next file in order, wrapping
/// back to the first after the last: two files alternate, three cycle, and one file repeats.
/// </summary>
public sealed class SoundRotation
{
    private readonly IReadOnlyList<string> _soundFiles;

    // Index of the file the next tap will play.
    private int _next;

    public SoundRotation(IReadOnlyList<string> soundFiles)
    {
        // A tile with nothing to play is a mistake in the tile list, so fail at startup.
        if (soundFiles.Count == 0)
        {
            throw new ArgumentException("A rotation needs at least one sound file.", nameof(soundFiles));
        }

        _soundFiles = soundFiles;
    }

    /// <summary>
    /// Returns the file the tap should play and moves the turn on to the following file.
    /// </summary>
    public string Next()
    {
        var file = _soundFiles[_next];
        _next = (_next + 1) % _soundFiles.Count;
        return file;
    }
}
