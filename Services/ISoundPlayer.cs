namespace CustomSoundboard.Services;

public interface ISoundPlayer
{
    /// <summary>
    /// Plays a sound packaged under Resources/Sounds, stopping whatever is currently playing.
    /// </summary>
    Task PlayAsync(string assetName);
}
