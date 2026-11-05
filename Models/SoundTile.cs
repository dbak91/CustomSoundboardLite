namespace CustomSoundboard.Models;

/// <summary>
/// One big button on the board: a picture (emoji or image), a colour and the sound it plays.
/// </summary>
/// <param name="SoundFile">The first sound this tile plays.</param>
/// <param name="Image">Picture asset name, used instead of the emoji when set.</param>
/// <param name="MoreSoundFiles">Extra sounds that take turns with <paramref name="SoundFile"/> on each tap.
/// Leave null for a tile with one sound.</param>
public record SoundTile(string Emoji, string Label, string Color, string SoundFile, string? Image = null, string[]? MoreSoundFiles = null)
{
    // Every file this tile can play, in turn order. The rotation lives on the tile itself, so it
    // is kept when the user switches pages and the tile is rebuilt.
    public SoundRotation Sounds { get; } = new([SoundFile, .. MoreSoundFiles ?? []]);
}
