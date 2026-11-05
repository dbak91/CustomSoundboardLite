using System.Diagnostics;
using CustomSoundboard.Models;
using CustomSoundboard.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace CustomSoundboard;

public class MainPage : ContentPage
{
    // Landscape board: five tiles across suits the wide screens of older tablets.
    private const int Columns = 5;

    // Edit mode is shown with this orange frame and banner, and its menu button.
    private const string EditColor = "#FFB300";

    // Menu buttons. Each kind of choice has its own colour so it is easy to find.
    private const string AddPageColor = "#1565C0";
    private const string RemovePageColor = "#C62828";
    private const string CloseColor = "#EEEEEE";

    // Choices for one tile in edit mode.
    // Each tile opens a short chain of menus: first what to edit, then how.
    private const string EditAudioChoice = "Edit audio";
    private const string EditImageChoice = "Edit image";
    private const string EditLabelChoice = "Edit label";
    private const string ClearTileChoice = "Clear this tile";

    // Edit audio.
    private const string TextToAudioChoice = "Text to audio";
    private const string UploadAudioChoice = "Upload own audio";
    private const string RecordAudioChoice = "Record audio";

    // The plain, default voice: the device's own text-to-speech.
    private const string RobotVoiceName = "Plain robot voice";

    // Tiny robot voices: the AI voice run through a robot effect. No extra model files.
    private static readonly (string Name, string Kind, RoboticStyle Style)[] RoboticVoices =
    [
        ("Robot - classic hum", "robotclassic", RoboticStyle.Classic),
        ("Robot - Dalek", "robotdalek", RoboticStyle.Dalek),
        ("Robot - retro 8-bit", "robotretro", RoboticStyle.Retro),
        ("Robot - tin can", "robotmetallic", RoboticStyle.Metallic),
    ];

    // Edit image.
    private const string ChooseSymbolChoice = "Choose a symbol";
    private const string UploadImageChoice = "Upload image";
    private const string TakeImageChoice = "Take image";

    // Audio files the file picker offers.
    private static readonly FilePickerFileType AudioFileTypes = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.Android] = ["audio/*"],
    });

    // Page one: five across, two rows of five.
    private static readonly SoundTile[] PageOneTiles =
    [
        new(string.Empty, "I want", "#FFF59D", "i_want.wav", "mb_want"),
        new(string.Empty, "Drink", "#90CAF9", "drink.wav", "icon_drink"),
        new(string.Empty, "Food", "#FFE082", "food.wav", "mb_dinner"),
        new(string.Empty, "Toilet", "#F06292", "toilet.wav", "mb_toilet"),
        new(string.Empty, "Bedroom", "#9FA8DA", "bedroom.wav", "icon_bedroom"),
        new("👇", "Stay here", "#C5CAE9", "stay_here.wav"),
        new(string.Empty, "Help", "#E1BEE7", "help.wav", "icon_help"),
        new(string.Empty, "Music", "#D1C4E9", "music.wav", "icon_music"),
        new("➕", "More", "#81C784", "more.wav"),
        new("🤫", "Quiet", "#CE93D8", "quiet.wav"),
    ];

    // Page two starts with Cake (tile 11), then the tiles that moved off page one.
    private static readonly SoundTile[] PageTwoTiles =
    [
        new(string.Empty, "Cake", "#FFE0B2", "cake.wav", "icon_cake"),
        new(string.Empty, "Happy", "#F8BBD0", "happy.wav", "icon_girl"),
        new(string.Empty, "Sad", "#B2DFDB", "sad.wav", "icon_sad"),
        new(string.Empty, "Yes", "#4FC3F7", "yes.wav", "icon_yes"),
        new(string.Empty, "No", "#E57373", "no.wav", "icon_no"),
        new(string.Empty, "Shower", "#B3E5FC", "shower.wav", "icon_shower"),
        new(string.Empty, "Stairs", "#B0BEC5", "stairs.wav", "icon_stairs"),
        new(string.Empty, "Pizza", "#FF8A65", "pizza.wav", "icon_pizza"),
        new(string.Empty, "Outside", "#C5E1A5", "outside.wav", "icon_outside"),
        new(string.Empty, "Home", "#FFCCBC", "home.wav", "icon_home"),
    ];

    // Page three: everyday words.
    private static readonly SoundTile[] PageThreeTiles =
    [
        new("🚶", "Walk", "#C8E6C9", "walk.wav"),
        new("💃", "Dance", "#F8BBD0", "dance.wav"),
        new("🌭", "Sausages", "#FFF9C4", "sausages.wav"),
        new("🍛", "Curry", "#FFE0B2", "curry.wav"),
        new("📀", "DVD", "#C5CAE9", "dvd.wav"),
        new(string.Empty, "Crisps", "#FFF59D", "crisps.wav", "icon_crisps"),
    ];

    // Colours cycled across tiles on extra pages, each on a contrasting pastel tile.
    private static readonly string[] TileColors =
    [
        "#FFF9C4", "#FFE0B2", "#FFCCBC", "#F8BBD0", "#E1BEE7", "#D1C4E9",
        "#C5CAE9", "#BBDEFB", "#B3E5FC", "#B2EBF2", "#B2DFDB", "#C8E6C9",
    ];

    private readonly ISoundPlayer _player;
    private readonly IAudioRecorder _recorder;
    private readonly ISpeechSynthesizer _speech;
    private readonly ContentView _pageHost = new();
    private readonly Border _pageFrame;
    private readonly Grid _editRow;
    private readonly Button _menuButton;
    private readonly Grid _menuOverlay;
    private readonly VerticalStackLayout _menuItems;
    private readonly List<(Button Tab, Func<View> Build)> _tabs = new();
    private readonly List<Button> _builtInTabs = new();
    private readonly List<Button> _extraTabs = new();

    private const int BuiltInPageCount = 3;

    // The saved extra pages, as the board shows them.
    private UserPages _live;

    // The copy being edited. It is the same object as _live except in edit mode.
    private UserPages _pages;

    // Files replaced by the draft: removed when the draft is confirmed.
    private readonly List<string> _filesToDelete = new();

    // Files made by the draft: removed when the draft is cancelled.
    private readonly List<string> _filesToDiscard = new();

    private Button? _selectedTab;
    private bool _editMode;

    // The question the menu panel is showing, if any, so the back button can answer it.
    private TaskCompletionSource<string?>? _pendingChoice;

    public MainPage(
        ISoundPlayer player,
        IAudioRecorder recorder,
        ISpeechSynthesizer speech
    )
    {
        _player = player;
        _recorder = recorder;
        _speech = speech;
        _live = UserPageStore.Load();
        EnsureBuiltInPages();
        _pages = _live;

        // Full-screen board: no title bar, so every tile can be used.
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Colors.White;

        // One row of tabs. Labels are short so all of them fit; more tabs scroll sideways.
        var tabBar = new HorizontalStackLayout
        {
            Spacing = 10,
            Padding = new Thickness(10, 8),
        };

        AddTab(tabBar, "1", () => BuildUserBoard(0));
        AddTab(tabBar, "2", () => BuildUserBoard(1));
        AddTab(tabBar, "3", () => BuildUserBoard(2));

        // Extra pages sit straight after page 3. They show only once they exist.
        for (var page = 0; page < UserPages.MaxExtraPages; page++)
        {
            var pageIndex = page;
            _extraTabs.Add(AddTab(tabBar, $"{4 + page}", () => BuildUserBoard(BuiltInPageCount + pageIndex), isUserPage: true));
        }

        var tabScroll = new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Never,
            Content = tabBar,
        };

        // Small menu button in the top right corner.
        _menuButton = new Button
        {
            ImageSource = "icon_menu",
            CornerRadius = 12,
            WidthRequest = 48,
            Padding = new Thickness(0),
            BackgroundColor = Color.FromArgb("#EEEEEE"),
            TextColor = Colors.Black,
        };
        SemanticProperties.SetDescription(_menuButton, "Menu");
        _menuButton.Clicked += (_, _) => OpenMenu();

        var topRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            },
        };
        topRow.Add(tabScroll, 0, 0);
        var corner = new HorizontalStackLayout
        {
            Spacing = 8,
            Padding = new Thickness(0, 8, 8, 8),
            Children = { _menuButton },
        };
        topRow.Add(corner, 1, 0);

        // Shown only in edit mode: says the changes are a draft, with Cancel and Confirm.
        var editLabel = new Label
        {
            Text = "EDIT MODE: tap a tile to change it, drag one tile onto another to swap them. Changes go live when you tap Confirm",
            FontAttributes = FontAttributes.Bold,
            FontSize = 16,
            TextColor = Colors.Black,
            LineBreakMode = LineBreakMode.WordWrap,
            VerticalTextAlignment = TextAlignment.Center,
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            FontSize = 18,
            CornerRadius = 12,
            BackgroundColor = Color.FromArgb("#EEEEEE"),
            TextColor = Colors.Black,
        };
        cancelButton.Clicked += async (_, _) => await CancelEditAsync();

        var confirmButton = new Button
        {
            Text = "Confirm",
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            BackgroundColor = Color.FromArgb("#2E7D32"),
            TextColor = Colors.White,
        };
        confirmButton.Clicked += async (_, _) => await ConfirmEditAsync();

        _editRow = new Grid
        {
            ColumnSpacing = 12,
            Padding = new Thickness(12, 6),
            BackgroundColor = Color.FromArgb(EditColor),
            IsVisible = false,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
            },
        };
        _editRow.Add(editLabel, 0, 0);
        _editRow.Add(cancelButton, 1, 0);
        _editRow.Add(confirmButton, 2, 0);

        // The orange frame around the board shows that edit mode is on.
        _pageFrame = new Border
        {
            Content = _pageHost,
            Stroke = Colors.Transparent,
            StrokeThickness = 0,
        };

        // The menu panel, drawn over the whole screen while it is open.
        _menuItems = new VerticalStackLayout { Spacing = 14 };
        _menuOverlay = new Grid
        {
            IsVisible = false,
            BackgroundColor = Color.FromArgb("#B3000000"),
            Padding = new Thickness(24),
        };
        _menuOverlay.Add(new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 20 },
            Padding = new Thickness(20),
            MaximumWidthRequest = 420,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Content = new ScrollView { Content = _menuItems },
        });

        var layout = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
            },
        };
        layout.Add(topRow, 0, 0);
        layout.Add(_editRow, 0, 1);
        layout.Add(_pageFrame, 0, 2);

        // Added last so the menu sits on top of everything.
        layout.Add(_menuOverlay, 0, 0);
        Grid.SetRowSpan(_menuOverlay, 3);
        Content = layout;

        ApplyExtraPages();
        ShowPage(_tabs[0].Tab);
    }

    // Older saves contain only extra pages. On first run after this change, seed editable
    // copies of the three starter pages while leaving all existing extra pages untouched.
    private void EnsureBuiltInPages()
    {
        var changed = _live.BuiltInPages.Count != BuiltInPageCount;
        if (changed)
        {
            _live.BuiltInPages =
            [
                CreateDefaultPage(PageOneTiles),
                CreateDefaultPage(PageTwoTiles),
                CreateDefaultPage(PageThreeTiles),
            ];
        }

        // Upgrade the old starter emoji, including tiles moved within their page.
        // A custom picture or a cleared tile must keep the user's chosen appearance.
        foreach (var tile in _live.BuiltInPages.SelectMany(page => page))
        {
            if (tile.ImagePath is not null || tile.ImageAsset is not null)
            {
                continue;
            }

            var image = (tile.Emoji, tile.Label) switch
            {
                ("🛏️", "Bedroom") => "icon_bedroom",
                ("🍕", "Pizza") => "icon_pizza",
                ("🍰", "Cake") => "icon_cake",
                ("🆘", "Help") => "icon_help",
                _ => null,
            };
            if (image is null)
            {
                continue;
            }

            tile.ImageAsset = image;
            tile.Emoji = string.Empty;
            changed = true;
        }

        if (changed)
        {
            UserPageStore.Save(_live);
        }
    }

    private static UserTile[] CreateDefaultPage(IEnumerable<SoundTile> defaults) =>
        defaults
            .Select(tile => new UserTile
            {
                Emoji = tile.Emoji,
                Label = tile.Label,
                Color = tile.Color,
                ImageAsset = tile.Image,
                SoundAsset = tile.SoundFile,
            })
            .Concat(Enumerable.Repeat(0, UserPages.TilesPerPage).Select(_ => new UserTile()))
            .Take(UserPages.TilesPerPage)
            .ToArray();

    private static UserTile[] GetPage(UserPages state, int page) =>
        page < BuiltInPageCount ? state.BuiltInPages[page] : state.Pages[page - BuiltInPageCount];

    private static bool HasSound(UserTile tile) =>
        tile.SoundPath is not null || tile.SoundAsset is not null;

    private Button AddTab(HorizontalStackLayout tabBar, string title, Func<View> build, bool isUserPage = false)
    {
        var tab = new Button
        {
            Text = title,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            MinimumWidthRequest = 72,
        };

        SemanticProperties.SetDescription(tab, $"{title} page");
        tab.Clicked += (_, _) => ShowPage(tab);
        _tabs.Add((tab, build));
        if (!isUserPage)
        {
            _builtInTabs.Add(tab);
        }

        tabBar.Children.Add(tab);
        return tab;
    }

    private void ShowPage(Button selected)
    {
        _selectedTab = selected;
        foreach (var (tab, build) in _tabs)
        {
            var isSelected = tab == selected;
            tab.BackgroundColor = Color.FromArgb(isSelected ? "#C62828" : "#EEEEEE");
            tab.TextColor = isSelected ? Colors.White : Colors.Black;
            if (isSelected)
            {
                Debug.WriteLine($"CustomSoundboard: page '{tab.Text}' shown");
                _pageHost.Content = build();
            }
        }
    }

    // Shows the extra page tabs that exist, and moves off an extra page that was removed.
    private void ApplyExtraPages()
    {
        for (var index = 0; index < _extraTabs.Count; index++)
        {
            _extraTabs[index].IsVisible = index < _live.Pages.Count;
        }

        if (_selectedTab is { IsVisible: false })
        {
            ShowPage(_tabs[0].Tab);
        }
        else
        {
            // The page under the same tab may now hold different tiles, so it is redrawn.
            RebuildSelectedPage();
        }
    }

    private void OpenMenu()
    {
        _menuItems.Children.Clear();
        var pageCount = _live.Pages.Count;

        // Edit mode opens on the page being viewed; the page tabs stay available to reach the others.
        AddMenuButton("Edit mode", EditColor, Colors.Black, async () => StartEdit());
        AddMenuButton("Add page", AddPageColor, Colors.White, AddExtraPageAsync);

        for (var index = 0; index < pageCount; index++)
        {
            var pageIndex = index;
            AddMenuButton($"Remove page {4 + pageIndex}", RemovePageColor, Colors.White, () => RemoveExtraPageAsync(pageIndex));
        }

        AddMenuButton("Credits", CloseColor, Colors.Black, () => DisplayAlert("Credits", SymbolCatalog.Credits, "OK"));
        AddMenuButton("Close", CloseColor, Colors.Black, async () => { });
        _menuOverlay.IsVisible = true;
    }

    // Asks a question with the same big buttons as the menu, and returns the choice, or null for Back.
    private Task<string?> ChooseAsync(string title, string back, params string[] choices)
    {
        var result = new TaskCompletionSource<string?>();
        _pendingChoice = result;
        _menuItems.Children.Clear();
        _menuItems.Children.Add(new Label
        {
            Text = title,
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Black,
            HorizontalTextAlignment = TextAlignment.Center,
        });

        foreach (var choice in choices)
        {
            var color = choice switch
            {
                ClearTileChoice => RemovePageColor,
                _ => AddPageColor,
            };
            AddMenuButton(choice, color, Colors.White, async () => result.TrySetResult(choice));
        }

        AddMenuButton(back, CloseColor, Colors.Black, async () => result.TrySetResult(null));
        _menuOverlay.IsVisible = true;
        return result.Task;
    }

    // The tablet's back button closes the menu panel, the same as its last button.
    protected override bool OnBackButtonPressed()
    {
        if (!_menuOverlay.IsVisible)
        {
            return base.OnBackButtonPressed();
        }

        _menuOverlay.IsVisible = false;
        _pendingChoice?.TrySetResult(null);
        return true;
    }

    private void AddMenuButton(string text, string color, Color textColor, Func<Task> action, bool enabled = true)
    {
        var button = new Button
        {
            Text = text,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 12,
            MinimumHeightRequest = 56,
            BackgroundColor = Color.FromArgb(color),
            TextColor = textColor,
            IsEnabled = enabled,
            Opacity = enabled ? 1 : 0.4,
        };

        // The panel closes first, so the action can open its own alerts on a clear screen.
        button.Clicked += async (_, _) =>
        {
            _menuOverlay.IsVisible = false;
            await action();
        };
        _menuItems.Children.Add(button);
    }

    // Creates an empty page and opens it in edit mode, ready to be filled in.
    private async Task AddExtraPageAsync()
    {
        // There are only so many page tabs, so the last one cannot be passed.
        if (_live.Pages.Count >= UserPages.MaxExtraPages)
        {
            await DisplayAlert("Add page", $"There can be at most {UserPages.MaxExtraPages} extra pages.", "OK");
            return;
        }

        _live.Pages.Add(NewPage());
        UserPageStore.Save(_live);
        ApplyExtraPages();
        ShowPage(_extraTabs[_live.Pages.Count - 1]);
        StartEdit();
    }

    private static UserTile[] NewPage() =>
        Enumerable.Range(0, UserPages.TilesPerPage).Select(_ => new UserTile()).ToArray();

    // Removing a page takes effect at once, and deletes its pictures and sounds.
    private async Task RemoveExtraPageAsync(int page)
    {
        if (!await DisplayAlert("Remove page", $"Remove page {4 + page} and everything on it?", "Remove", "Keep"))
        {
            return;
        }

        foreach (var tile in _live.Pages[page])
        {
            UserPageStore.Delete(tile.ImagePath);
            UserPageStore.Delete(tile.SoundPath);
        }

        _live.Pages.RemoveAt(page);
        UserPageStore.Save(_live);
        ApplyExtraPages();
    }

    // Edit mode works on a copy, so Cancel can discard changes to any existing page.
    private void StartEdit()
    {
        _pages = UserPageStore.Copy(_live);
        _editMode = true;
        _menuButton.IsVisible = false;
        _editRow.IsVisible = true;
        _pageFrame.Stroke = Color.FromArgb(EditColor);
        _pageFrame.StrokeThickness = 8;

        // The page on screen is redrawn with editable tiles, not swapped for another one.
        RebuildSelectedPage();
    }

    private async Task ConfirmEditAsync()
    {
        if (!await DisplayAlert("Confirm changes", "Make these changes live on the board?", "Confirm", "Keep editing"))
        {
            return;
        }

        _live = _pages;
        UserPageStore.Save(_live);
        foreach (var path in _filesToDelete)
        {
            UserPageStore.Delete(path);
        }

        EndEdit();
    }

    private async Task CancelEditAsync()
    {
        if (!await DisplayAlert("Discard changes", "Throw away the changes you made?", "Discard", "Keep editing"))
        {
            return;
        }

        foreach (var path in _filesToDiscard)
        {
            UserPageStore.Delete(path);
        }

        EndEdit();
    }

    private void EndEdit()
    {
        _filesToDelete.Clear();
        _filesToDiscard.Clear();
        _pages = _live;
        _editMode = false;
        _menuButton.IsVisible = true;
        _editRow.IsVisible = false;
        _pageFrame.Stroke = Colors.Transparent;
        _pageFrame.StrokeThickness = 0;
        ApplyExtraPages();
    }

    private void RebuildSelectedPage()
    {
        if (_selectedTab is not null)
        {
            ShowPage(_selectedTab);
        }
    }

    private View BuildUserBoard(int page)
    {
        var tiles = new List<View>();
        for (var index = 0; index < UserPages.TilesPerPage; index++)
        {
            tiles.Add(CreateUserTile(page, index));
        }

        return Arrange(tiles, Columns);
    }

    private static View Arrange(IReadOnlyList<View> tiles, int columns)
    {
        var rowCount = (int)Math.Ceiling(tiles.Count / (double)columns);
        var grid = new Grid
        {
            Padding = 16,
            RowSpacing = 16,
            ColumnSpacing = 16,
        };

        for (var column = 0; column < columns; column++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        for (var row = 0; row < rowCount; row++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        }

        for (var index = 0; index < tiles.Count; index++)
        {
            grid.Add(tiles[index], index % columns, index / columns);
        }

        return grid;
    }

    private Border CreateUserTile(int page, int index)
    {
        var tile = GetPage(_pages, page)[index];
        var picturePath = tile.ImagePath is not null && File.Exists(tile.ImagePath) ? tile.ImagePath : null;

        View? content = null;
        if (_editMode)
        {
            content = CreateEditContent(tile, picturePath, () => PlaySoundAsync(tile));
        }
        else if (picturePath is not null)
        {
            content = new Image { Source = ImageSource.FromFile(picturePath), Aspect = Aspect.AspectFit, Margin = new Thickness(6) };
        }
        else if (tile.ImageAsset is not null)
        {
            content = new Image { Source = SymbolCatalog.ImageFor(tile.ImageAsset), Aspect = Aspect.AspectFit, Margin = new Thickness(6) };
        }
        else if (!string.IsNullOrWhiteSpace(tile.Emoji))
        {
            content = CreateEmoji(tile.Emoji);
        }
        else if (HasSound(tile))
        {
            // Sound but no picture yet: a speaker stands in for it.
            content = new Label
            {
                Text = "🔊",
                FontSize = 72,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
            };
        }

        var label = $"page {page + 1}, tile {index + 1}";
        var tileBorder = new Border
        {
            BackgroundColor = Color.FromArgb(tile.Color ?? TileColors[index % TileColors.Length]),
            StrokeShape = new RoundRectangle { CornerRadius = 28 },
            Stroke = Colors.Transparent,
            Padding = new Thickness(4),
            Content = content,
        };

        var accessibleLabel = string.IsNullOrWhiteSpace(tile.Label) ? label : tile.Label;
        SemanticProperties.SetDescription(tileBorder, _editMode ? $"Edit {accessibleLabel}" : $"Play {accessibleLabel}");
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await OnUserTileTappedAsync(tileBorder, page, index);
        tileBorder.GestureRecognizers.Add(tap);

        if (_editMode)
        {
            // Drag a tile onto another tile on the same page to swap the two.
            var drag = new DragGestureRecognizer();
            drag.DragStarting += (_, e) =>
            {
                e.Data.Text = $"{page}:{index}";
            };

            var drop = new DropGestureRecognizer { AllowDrop = true };
            drop.Drop += async (_, e) => SwapTiles(page, index, await e.Data.GetTextAsync());

            tileBorder.GestureRecognizers.Add(drag);
            tileBorder.GestureRecognizers.Add(drop);
        }

        return tileBorder;
    }

    // In edit mode a tile shows its picture (or speaker, or a plus when empty), with a pencil to show it can be changed.
    // A tile with a sound also gets a play button under the pencil, so it can be heard without opening the menu.
    private static View CreateEditContent(UserTile tile, string? picturePath, Func<Task> play)
    {
        var content = new Grid();
        if (picturePath is not null)
        {
            content.Add(new Image { Source = ImageSource.FromFile(picturePath), Aspect = Aspect.AspectFit, Margin = new Thickness(6) });
        }
        else if (tile.ImageAsset is not null)
        {
            content.Add(new Image { Source = SymbolCatalog.ImageFor(tile.ImageAsset), Aspect = Aspect.AspectFit, Margin = new Thickness(6) });
        }
        else if (!string.IsNullOrWhiteSpace(tile.Emoji))
        {
            content.Add(CreateEmoji(tile.Emoji));
        }
        else
        {
            content.Add(new Label
            {
                Text = HasSound(tile) ? "🔊" : "+",
                FontSize = HasSound(tile) ? 72 : 96,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#37474F"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            });
        }

        content.Add(new Image
        {
            Source = "icon_edit",
            WidthRequest = 40,
            HeightRequest = 40,
            Margin = new Thickness(4),
            BackgroundColor = Color.FromArgb("#CCFFFFFF"),
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
        });

        if (HasSound(tile))
        {
            // A button, not a tap on the tile, so pressing it plays the sound and does not open the menu.
            var playButton = new Button
            {
                Text = "🔊",
                FontSize = 18,
                WidthRequest = 40,
                HeightRequest = 40,
                CornerRadius = 20,
                Padding = new Thickness(0),
                Margin = new Thickness(4, 52, 4, 0),
                BackgroundColor = Color.FromArgb("#CCFFFFFF"),
                TextColor = Color.FromArgb("#2E7D32"),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.Start,
            };
            SemanticProperties.SetDescription(playButton, "Play this sound");
            playButton.Clicked += async (_, _) => await play();
            content.Add(playButton);
        }

        return content;
    }

    private static Label CreateEmoji(string emoji) => new()
    {
        Text = emoji,
        FontSize = 72,
        HorizontalOptions = LayoutOptions.Center,
        VerticalOptions = LayoutOptions.Center,
        HorizontalTextAlignment = TextAlignment.Center,
        VerticalTextAlignment = TextAlignment.Center,
    };

    // Swaps the tile being dropped on with the tile that was dragged. Both must be on the same page.
    private void SwapTiles(int page, int index, string? source)
    {
        var parts = source?.Split(':');
        if (parts is not { Length: 2 }
            || !int.TryParse(parts[0], out var fromPage)
            || !int.TryParse(parts[1], out var fromIndex)
            || fromPage != page
            || fromIndex == index)
        {
            return;
        }

        var tiles = GetPage(_pages, page);
        (tiles[fromIndex], tiles[index]) = (tiles[index], tiles[fromIndex]);
        Dispatcher.Dispatch(RebuildSelectedPage);
    }

    private static async Task PressAsync(VisualElement tileView)
    {
        // Brief press animation so the child can see which tile responded.
        await tileView.ScaleToAsync(0.92, 90);
        await tileView.ScaleToAsync(1, 90);
    }

    private async Task OnUserTileTappedAsync(VisualElement tileView, int page, int index)
    {
        if (_editMode)
        {
            await EditUserTileAsync(page, index);
            return;
        }

        var tile = GetPage(_pages, page)[index];
        var sound = tile.SoundPath ?? (tile.SoundAsset is null ? null : $"sounds/{tile.SoundAsset}");
        if (sound is null)
        {
            // Nothing recorded on this tile yet.
            return;
        }

        await PressAsync(tileView);
        Debug.WriteLine($"CustomSoundboard: tapped page {page + 1}, tile {index + 1} (sound: {sound})");

        try
        {
            await _player.PlayAsync(sound);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CustomSoundboard: could not play {sound}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private async Task PlaySoundAsync(UserTile tile)
    {
        var sound = tile.SoundPath ?? (tile.SoundAsset is null ? null : $"sounds/{tile.SoundAsset}");
        if (sound is null)
        {
            return;
        }

        try
        {
            await _player.PlayAsync(sound);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CustomSoundboard: could not play {sound}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    // Tapping a tile in edit mode: first choose what to change, then how. Each step is a short menu.
    private async Task EditUserTileAsync(int page, int index)
    {
        var title = $"Page {page + 1}, tile {index + 1}";
        var tile = GetPage(_pages, page)[index];

        var choice = await ChooseAsync(title, "Cancel", [EditAudioChoice, EditImageChoice, EditLabelChoice, ClearTileChoice]);

        var changed = false;
        try
        {
            if (choice == EditAudioChoice)
            {
                changed = await EditAudioAsync(page, index, title);
            }
            else if (choice == EditImageChoice)
            {
                changed = await EditImageAsync(page, index, title);
            }
            else if (choice == EditLabelChoice)
            {
                changed = await EditLabelAsync(page, index);
            }
            else if (choice == ClearTileChoice)
            {
                changed = ClearTile(page, index);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"CustomSoundboard: edit of {title} failed: {ex.GetType().Name}: {ex.Message}");
            await DisplayAlert("Sorry", ex.Message, "OK");
        }

        if (changed)
        {
            RebuildSelectedPage();
        }
    }

    // Audio comes from typed words or from a file (or a recording).
    private async Task<bool> EditAudioAsync(int page, int index, string title)
    {
        var choices = new List<string> { TextToAudioChoice, UploadAudioChoice };
        choices.Add(RecordAudioChoice);
        var choice = await ChooseAsync($"{title}: audio", "Back", choices.ToArray());
        if (choice == TextToAudioChoice)
        {
            return await TextToAudioAsync(page, index);
        }

        if (choice == UploadAudioChoice)
        {
            return await PickSoundAsync(page, index);
        }

        if (choice == RecordAudioChoice)
        {
            return await RecordSoundAsync(page, index);
        }
        return false;
    }

    // Type the words, pick a voice and hear them, all on one screen; nothing changes until Use this sound.
    private async Task<bool> TextToAudioAsync(int page, int index)
    {
        var voices = new List<SpeechVoice>();
        if (await _speech.IsAvailableAsync())
        {
            // The plain voice first: the device's own speech, no effect.
            voices.Add(new SpeechVoice(RobotVoiceName, "speech", (text, path) => _speech.SynthesizeAsync(text, path)));

            // Then lightweight robot voices: the device's own speech with a simple effect. No models.
            foreach (var (name, kind, style) in RoboticVoices)
            {
                voices.Add(new SpeechVoice(name, kind, async (text, path) =>
                {
                    await _speech.SynthesizeAsync(text, path);
                    await Task.Run(() => RoboticVoice.Apply(path, style));
                }));
            }
        }

        if (voices.Count == 0)
        {
            await DisplayAlert("No voice", "This device has no voice to turn words into sound.", "OK");
            return false;
        }

        var result = await SpeechEditorPage.EditAsync(Navigation, voices, _player, page, index);
        if (result is null)
        {
            return false;
        }

        var tile = GetPage(_pages, page)[index];
        if (string.IsNullOrWhiteSpace(tile.Label))
        {
            tile.Label = result.Text;
        }

        ReplaceSound(tile, result.Path);
        return true;
    }

    private async Task<bool> EditImageAsync(int page, int index, string title)
    {
        var choice = await ChooseAsync($"{title}: image", "Back", ChooseSymbolChoice, UploadImageChoice, TakeImageChoice);
        if (choice == ChooseSymbolChoice)
        {
            return await PickSymbolAsync(page, index);
        }

        if (choice == UploadImageChoice)
        {
            return await PickPictureAsync(page, index);
        }

        if (choice == TakeImageChoice)
        {
            return await TakePictureAsync(page, index);
        }

        return false;
    }

    private async Task<bool> EditLabelAsync(int page, int index)
    {
        var tile = GetPage(_pages, page)[index];
        var text = await DisplayPromptAsync("Edit label", "What should this tile be called?", "Save", "Cancel", initialValue: tile.Label ?? string.Empty);
        if (text is null)
        {
            return false;
        }

        tile.Label = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        return true;
    }

    // Uses a packaged symbol as the tile's picture. A blank label is filled in with the symbol's name.
    private async Task<bool> PickSymbolAsync(int page, int index)
    {
        var symbol = await SymbolPickerPage.PickAsync(Navigation);
        if (symbol is null)
        {
            return false;
        }

        var tile = GetPage(_pages, page)[index];
        if (tile.ImagePath is not null)
        {
            _filesToDelete.Add(tile.ImagePath);
        }

        tile.ImagePath = null;
        tile.ImageAsset = symbol.Asset;
        tile.Emoji = null;
        if (string.IsNullOrWhiteSpace(tile.Label))
        {
            tile.Label = symbol.Label;
        }

        return true;
    }

    private async Task<bool> PickPictureAsync(int page, int index)
    {
        var file = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions { Title = "Choose a picture" });
        if (file is null)
        {
            return false;
        }

        var path = await CopyIntoTileAsync(file, page, index, "picture", ".jpg");
        ReplaceImage(GetPage(_pages, page)[index], path);
        return true;
    }

    private async Task<bool> TakePictureAsync(int page, int index)
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            await DisplayAlert("No camera", "This tablet cannot take pictures.", "OK");
            return false;
        }

        if (await Permissions.RequestAsync<Permissions.Camera>() != PermissionStatus.Granted)
        {
            return false;
        }

        var file = await MediaPicker.Default.CapturePhotoAsync();
        if (file is null)
        {
            return false;
        }

        var path = await CopyIntoTileAsync(file, page, index, "picture", ".jpg");
        ReplaceImage(GetPage(_pages, page)[index], path);
        return true;
    }

    private async Task<bool> PickSoundAsync(int page, int index)
    {
        var file = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Choose a sound",
            FileTypes = AudioFileTypes,
        });
        if (file is null)
        {
            return false;
        }

        var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        var supported = extension is ".mp3" or ".wav" or ".m4a";
        const string supportedDescription = "Choose an MP3, WAV, or M4A audio file.";
        if (!supported)
        {
            await DisplayAlert("Unsupported audio", supportedDescription, "OK");
            return false;
        }

        var path = await CopyIntoTileAsync(file, page, index, "sound", extension);
        ReplaceSound(GetPage(_pages, page)[index], path);
        return true;
    }

    private async Task<bool> RecordSoundAsync(int page, int index)
    {
        if (await Permissions.RequestAsync<Permissions.Microphone>() != PermissionStatus.Granted)
        {
            return false;
        }

        var path = UserPageStore.NewTileFile(page, index, "sound", ".m4a");
        _recorder.Start(path);

        // The alert stays up while the recording runs; its Stop button ends it.
        await DisplayAlert("Recording", "Say the word, then tap Stop.", "Stop");
        _recorder.Stop();

        ReplaceSound(GetPage(_pages, page)[index], path);
        return true;
    }

    private bool ClearTile(int page, int index)
    {
        var tile = GetPage(_pages, page)[index];
        if (tile.ImagePath is not null)
        {
            _filesToDelete.Add(tile.ImagePath);
        }

        if (tile.SoundPath is not null)
        {
            _filesToDelete.Add(tile.SoundPath);
        }

        tile.ImagePath = null;
        tile.ImageAsset = null;
        tile.Emoji = null;
        tile.SoundPath = null;
        tile.SoundAsset = null;
        tile.Label = null;
        return true;
    }

    // The new picture is kept only if the draft is confirmed; the old one is removed then, not now.
    private void ReplaceImage(UserTile tile, string newPath)
    {
        if (tile.ImagePath is not null)
        {
            _filesToDelete.Add(tile.ImagePath);
        }

        _filesToDiscard.Add(newPath);
        tile.ImagePath = newPath;
        tile.ImageAsset = null;
        tile.Emoji = null;
    }

    private void ReplaceSound(UserTile tile, string newPath)
    {
        if (tile.SoundPath is not null)
        {
            _filesToDelete.Add(tile.SoundPath);
        }

        _filesToDiscard.Add(newPath);
        tile.SoundPath = newPath;
        tile.SoundAsset = null;
    }

    // Copies a picked or captured file into app storage under a new name, and returns its full path.
    private static async Task<string> CopyIntoTileAsync(FileResult file, int page, int index, string kind, string fallbackExtension)
    {
        var extension = System.IO.Path.GetExtension(file.FileName);
        var path = UserPageStore.NewTileFile(page, index, kind, string.IsNullOrEmpty(extension) ? fallbackExtension : extension);

        await using var source = await file.OpenReadAsync();
        await using var target = File.Create(path);
        await source.CopyToAsync(target);
        return path;
    }
}
