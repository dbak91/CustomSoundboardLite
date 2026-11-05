using Android.Media;

namespace CustomSoundboard.Services;

public class AndroidAudioRecorder : IAudioRecorder
{
    private MediaRecorder? _recorder;

    public void Start(string filePath)
    {
        var recorder = new MediaRecorder();
        recorder.SetAudioSource(AudioSource.Mic);
        recorder.SetOutputFormat(OutputFormat.Mpeg4);
        recorder.SetAudioEncoder(AudioEncoder.Aac);
        recorder.SetOutputFile(filePath);
        recorder.Prepare();
        recorder.Start();
        _recorder = recorder;
    }

    public void Stop()
    {
        if (_recorder is null)
        {
            return;
        }

        try
        {
            _recorder.Stop();
        }
        catch (Java.Lang.RuntimeException)
        {
            // Nothing was captured (stopped too soon): releasing it below is enough.
        }

        _recorder.Release();
        _recorder = null;
    }
}
