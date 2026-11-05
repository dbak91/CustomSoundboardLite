namespace CustomSoundboard.Services;

public interface ISpeechSynthesizer
{
    /// <summary>
    /// True when Android has a working on-device text-to-speech engine.
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Speaks the text with the device's own voice and saves it as an audio file at the given path.
    /// </summary>
    Task SynthesizeAsync(string text, string filePath);

    /// <summary>
    /// The installed British English voices that work offline. Empty when none are installed.
    /// </summary>
    Task<IReadOnlyList<BritishVoice>> GetBritishVoicesAsync();

    /// <summary>
    /// Speaks the text with the voice that has the given id and saves it as an audio file at the given path.
    /// </summary>
    Task SynthesizeAsync(string text, string voiceId, string filePath);
}

/// <summary>
/// A voice the device offers. The id is the device's own name for it; the display name is what the user sees.
/// </summary>
public sealed record BritishVoice(string Id, string DisplayName);
