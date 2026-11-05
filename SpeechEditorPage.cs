using System.Diagnostics;
using CustomSoundboard.Services;

namespace CustomSoundboard;

/// <summary>
/// A voice the editor can speak with. Kind goes into the saved file's name.
/// </summary>
public sealed record SpeechVoice(string Name, string Kind, Func<string, string, Task> Synthesize);

/// <summary>
/// The words that were kept, and the sound file made from them.
/// </summary>
public sealed record SpeechResult(string Text, string Path);

/// <summary>
/// One screen for turning typed words into a tile's sound. Hear it plays the words and the screen stays
/// as it is, so the words can be changed and heard again before Use this sound keeps them.
/// </summary>
public class SpeechEditorPage : ContentPage
{
    private const string SelectedColor = "#1565C0";

    private readonly TaskCompletionSource<SpeechResult?> _result = new();
    private readonly ISoundPlayer _player;
    private readonly int _page;
    private readonly int _index;
    private readonly Entry _words;
    private readonly Label _status;
    private readonly Button _hearButton;
    private readonly Button _useButton;
    private SpeechVoice _voice;

    // The last sound made, and the words and voice it was made from. It is remade only when either changes.
    private string? _path;
    private string? _pathText;
    private SpeechVoice? _pathVoice;

    private bool _busy;
    private bool _finished;

    private SpeechEditorPage(IReadOnlyList<SpeechVoice> voices, ISoundPlayer player, int page, int index)
    {
        _player = player;
        _page = page;
        _index = index;
        _voice = voices[0];
        BackgroundColor = Colors.White;

        var heading = new Label
        {
            Text = "What should this tile say?",
            FontSize = 26,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Black,
        };

        _words = new Entry
        {
            Placeholder = "for example: Help",
            FontSize = 28,
            TextColor = Colors.Black,
            ReturnType = ReturnType.Done,
        };

        // Done on the keyboard plays the words, the same as Hear it.
        _words.Completed += async (_, _) => await HearAsync();

        _status = new Label { FontSize = 18, TextColor = Color.FromArgb("#37474F"), IsVisible = false };

        _hearButton = CreateButton("🔊 Hear it", SelectedColor, Colors.White, HearAsync);
        _useButton = CreateButton("Use this sound", "#2E7D32", Colors.White, UseAsync);
        var cancelButton = CreateButton("Cancel", "#C62828", Colors.White, () => Finish(null));

        var actions = new HorizontalStackLayout { Spacing = 12, Children = { cancelButton, _hearButton, _useButton } };

        var layout = new VerticalStackLayout { Spacing = 16, Padding = new Thickness(24), Children = { heading } };

        // With only one voice there is nothing to pick between. The dropdown lists voices in the order given:
        // the human-sounding ones first, then the robot ones.
        if (voices.Count > 1)
        {
            var picker = new Picker
            {
                Title = "Choose a voice",
                FontSize = 22,
                TextColor = Colors.Black,
                TitleColor = Color.FromArgb("#37474F"),
                ItemsSource = voices.Select(voice => voice.Name).ToList(),
                SelectedIndex = 0,
            };
            picker.SelectedIndexChanged += (_, _) =>
            {
                if (picker.SelectedIndex >= 0)
                {
                    _voice = voices[picker.SelectedIndex];
                }
            };

            layout.Add(new Label { Text = "Voice", FontSize = 18, TextColor = Color.FromArgb("#37474F") });
            layout.Add(picker);
        }

        layout.Add(_words);
        layout.Add(_status);
        layout.Add(actions);
        Content = new ScrollView { Content = layout };
    }

    /// <summary>
    /// Opens the editor and returns the words and sound kept, or null if the user cancelled.
    /// A sound that is not kept is deleted before this returns.
    /// </summary>
    public static async Task<SpeechResult?> EditAsync(INavigation navigation, IReadOnlyList<SpeechVoice> voices, ISoundPlayer player, int page, int index)
    {
        var editor = new SpeechEditorPage(voices, player, page, index);
        await navigation.PushModalAsync(editor);
        return await editor._result.Task;
    }

    // The tablet's back button counts as Cancel.
    protected override bool OnBackButtonPressed()
    {
        _ = Finish(null);
        return true;
    }

    private static Button CreateButton(string text, string color, Color textColor, Action action)
    {
        var button = new Button
        {
            Text = text,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            MinimumHeightRequest = 56,
            Padding = new Thickness(20, 0),
            BackgroundColor = Color.FromArgb(color),
            TextColor = textColor,
        };
        button.Clicked += (_, _) => action();
        return button;
    }

    private static Button CreateButton(string text, string color, Color textColor, Func<Task> action)
    {
        var button = CreateButton(text, color, textColor, () => { });
        button.Clicked += async (_, _) => await action();
        return button;
    }

    private async Task HearAsync()
    {
        var path = await MakeSoundAsync();
        if (path is not null)
        {
            await _player.PlayAsync(path);
        }
    }

    private async Task UseAsync()
    {
        var path = await MakeSoundAsync();
        if (path is not null)
        {
            await Finish(new SpeechResult(_pathText!, path));
        }
    }

    // Returns the sound for the words on screen, making it first if the words or voice changed.
    private async Task<string?> MakeSoundAsync()
    {
        var text = _words.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            ShowStatus("Type some words first.");
            _words.Focus();
            return null;
        }

        if (_path is not null && text == _pathText && _voice == _pathVoice)
        {
            return _path;
        }

        if (_busy)
        {
            return null;
        }

        SetBusy(true);
        var path = UserPageStore.NewTileFile(_page, _index, _voice.Kind, ".wav");
        try
        {
            await _voice.Synthesize(text, path);
            UserPageStore.Delete(_path);
            _path = path;
            _pathText = text;
            _pathVoice = _voice;
            ShowStatus(null);
            return path;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CustomSoundboard: could not speak '{text}': {ex.GetType().Name}: {ex.Message}");
            UserPageStore.Delete(path);
            ShowStatus($"Sorry, that did not work: {ex.Message}");
            return null;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        _hearButton.IsEnabled = !busy;
        _useButton.IsEnabled = !busy;
        _hearButton.Text = busy ? "Making the sound…" : "🔊 Hear it";
    }

    private void ShowStatus(string? message)
    {
        _status.Text = message;
        _status.IsVisible = message is not null;
    }

    private async Task Finish(SpeechResult? result)
    {
        if (_finished)
        {
            return;
        }

        _finished = true;
        if (result is null)
        {
            UserPageStore.Delete(_path);
        }

        await Navigation.PopModalAsync();
        _result.TrySetResult(result);
    }
}
