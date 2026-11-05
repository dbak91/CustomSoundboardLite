namespace CustomSoundboard.Services;

public interface IAudioRecorder
{
    /// <summary>
    /// Starts recording from the microphone into a file at the given path.
    /// </summary>
    void Start(string filePath);

    /// <summary>
    /// Stops the recording and finishes the file.
    /// </summary>
    void Stop();
}
