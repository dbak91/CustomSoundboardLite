using Android.Speech.Tts;

namespace CustomSoundboard.Services;

// Uses the voice already on the device (no download), so the app stays small.
public class AndroidSpeechSynthesizer : ISpeechSynthesizer
{
    private Android.Speech.Tts.TextToSpeech? _engine;

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            await GetEngineAsync();
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    // Google's British voices are named like "en-gb-x-gba-local", which means nothing to a person.
    // These are the ones whose speaker is known; any other voice is just numbered.
    private static readonly Dictionary<string, string> KnownSpeakers = new()
    {
        ["gba"] = "woman",
        ["gbb"] = "man",
        ["gbc"] = "woman",
        ["gbd"] = "man",
        ["fis"] = "woman",
        ["rjs"] = "man",
    };

    public async Task<IReadOnlyList<BritishVoice>> GetBritishVoicesAsync()
    {
        var engine = await GetEngineAsync();
        var names = BritishOfflineVoices(engine)
            .Select(voice => voice.Name ?? string.Empty)
            .Where(name => name.Length > 0)
            .Order()
            .ToList();

        // "en-GB-language" is only the device's default British voice, which repeats one of the others.
        var distinct = names.Where(name => !name.EndsWith("-language", StringComparison.OrdinalIgnoreCase)).ToList();
        if (distinct.Count > 0)
        {
            names = distinct;
        }

        var counts = new Dictionary<string, int>();
        var voices = new List<BritishVoice>();
        foreach (var name in names)
        {
            var speaker = name.Split('-').Select(part => KnownSpeakers.GetValueOrDefault(part)).FirstOrDefault(kind => kind is not null);
            var kind = speaker ?? "voice";
            counts[kind] = counts.GetValueOrDefault(kind) + 1;
            voices.Add(new BritishVoice(name, $"British {kind} {counts[kind]}"));
        }

        return voices;
    }

    public async Task SynthesizeAsync(string text, string filePath)
    {
        var engine = await GetEngineAsync();
        engine.SetLanguage(new Java.Util.Locale("en", "GB"));
        await SpeakToFileAsync(engine, text, filePath);
    }

    public async Task SynthesizeAsync(string text, string voiceId, string filePath)
    {
        var engine = await GetEngineAsync();
        var voice = BritishOfflineVoices(engine).FirstOrDefault(candidate => candidate.Name == voiceId)
            ?? throw new InvalidOperationException("That voice is no longer installed on this device.");
        engine.SetVoice(voice);
        await SpeakToFileAsync(engine, text, filePath);
    }

    // British English voices that run on the device. Voices that need the internet are left out.
    private static List<Voice> BritishOfflineVoices(Android.Speech.Tts.TextToSpeech engine)
    {
        var voices = engine.Voices;
        if (voices is null)
        {
            return [];
        }

        return voices.OfType<Voice>()
            .Where(voice => voice.Locale?.Language == "en" && voice.Locale.Country == "GB" && !voice.IsNetworkConnectionRequired)
            .ToList();
    }

    private static async Task SpeakToFileAsync(Android.Speech.Tts.TextToSpeech engine, string text, string filePath)
    {
        var finished = new TaskCompletionSource<bool>();
        engine.SetOnUtteranceProgressListener(new SpeechListener(finished));

        var utteranceId = Guid.NewGuid().ToString();
        var result = engine.SynthesizeToFile(text, null, new Java.IO.File(filePath), utteranceId);
        if (result != OperationResult.Success)
        {
            throw new InvalidOperationException("The device could not make speech from these words.");
        }

        await finished.Task;
    }

    private async Task<Android.Speech.Tts.TextToSpeech> GetEngineAsync()
    {
        if (_engine is not null)
        {
            return _engine;
        }

        var ready = new TaskCompletionSource<bool>();
        var engine = new Android.Speech.Tts.TextToSpeech(Android.App.Application.Context, new InitListener(ready));
        if (!await ready.Task)
        {
            engine.Shutdown();
            throw new InvalidOperationException("This device does not have a working text-to-speech engine.");
        }

        // Prefer a British English voice, the same accent as the recordings.
        engine.SetLanguage(new Java.Util.Locale("en", "GB"));
        _engine = engine;
        return engine;
    }

    private sealed class InitListener(TaskCompletionSource<bool> ready) : Java.Lang.Object, Android.Speech.Tts.TextToSpeech.IOnInitListener
    {
        public void OnInit(OperationResult status) => ready.TrySetResult(status == OperationResult.Success);
    }

    private sealed class SpeechListener(TaskCompletionSource<bool> finished) : UtteranceProgressListener
    {
        public override void OnStart(string? utteranceId)
        {
        }

        public override void OnDone(string? utteranceId) => finished.TrySetResult(true);

        public override void OnError(string? utteranceId) => finished.TrySetException(new InvalidOperationException("The device could not finish the speech."));
    }
}
