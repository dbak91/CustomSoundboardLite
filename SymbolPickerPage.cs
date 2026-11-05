using System.Collections.ObjectModel;
using CustomSoundboard.Models;
using Microsoft.Maui.Controls.Shapes;

namespace CustomSoundboard;

/// <summary>
/// Full-screen grid of the packaged symbols. Shows one pack at a time, in a grid that only builds the
/// pictures on screen, so it stays light on older tablets.
/// </summary>
public class SymbolPickerPage : ContentPage
{
    private const int GridColumns = 6;
    private const string SelectedColor = "#1565C0";
    private const string UnselectedColor = "#EEEEEE";

    // There are over 3000 symbols, so they are added a page at a time.
    private const int PageSize = 300;
    private const int LoadMoreThreshold = 30;

    private readonly TaskCompletionSource<Symbol?> _result = new();
    private readonly CollectionView _grid;
    private readonly ObservableCollection<Symbol> _shown = [];
    private readonly Entry _search;
    private readonly Dictionary<SymbolPack, Button> _packButtons = [];
    private List<Symbol> _matches = [];
    private SymbolPack _pack;
    private bool _finished;

    private SymbolPickerPage()
    {
        Title = "Choose a symbol";
        BackgroundColor = Colors.White;

        _grid = new CollectionView
        {
            SelectionMode = SelectionMode.None,
            ItemsLayout = new GridItemsLayout(GridColumns, ItemsLayoutOrientation.Vertical),
            ItemTemplate = new DataTemplate(CreateItem),
            ItemsSource = _shown,
            RemainingItemsThreshold = LoadMoreThreshold,
            EmptyView = new Label
            {
                Text = "No symbols match that word.",
                FontSize = 20,
                Margin = new Thickness(16),
                HorizontalTextAlignment = TextAlignment.Center,
            },
        };
        _grid.RemainingItemsThresholdReached += (_, _) => ShowNextPage();

        // Typing searches every pack by name; clearing the box goes back to the chosen pack.
        _search = new Entry
        {
            Placeholder = "Search, for example: drink",
            PlaceholderColor = Color.FromArgb("#757575"),
            TextColor = Colors.Black,
            FontSize = 20,
            ClearButtonVisibility = ClearButtonVisibility.WhileEditing,
            VerticalOptions = LayoutOptions.Center,
        };
        _search.TextChanged += (_, _) => ShowSymbols();

        var searchRow = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)],
        };
        searchRow.Add(new Label { Text = "🔍", FontSize = 24, VerticalOptions = LayoutOptions.Center }, 0, 0);
        searchRow.Add(_search, 1, 0);

        // An outlined box beside the buttons: on a landscape screen the top row is shared, not given a row of its own.
        var searchBox = new Border
        {
            Padding = new Thickness(12, 0),
            HeightRequest = 56,
            VerticalOptions = LayoutOptions.Center,
            BackgroundColor = Color.FromArgb("#F5F5F5"),
            Stroke = Color.FromArgb("#9E9E9E"),
            StrokeThickness = 2,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Content = searchRow,
        };

        var packs = SymbolCatalog.Available.Select(symbol => symbol.Pack).Distinct().ToList();
        var buttons = new HorizontalStackLayout { Spacing = 8 };
        buttons.Add(CreateButton("Cancel", "#C62828", Colors.White, () => Finish(null)));

        // With only one pack in the build there is nothing to switch between.
        if (packs.Count > 1)
        {
            foreach (var pack in packs)
            {
                var button = CreateButton(PackName(pack), UnselectedColor, Colors.Black, () => ShowPack(pack));
                _packButtons[pack] = button;
                buttons.Add(button);
            }
        }

        var header = new Grid
        {
            ColumnSpacing = 12,
            Margin = new Thickness(8),
            ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)],
        };
        header.Add(buttons, 0, 0);
        header.Add(searchBox, 1, 0);

        Content = new Grid
        {
            RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star)],
            Children = { header, _grid },
        };
        Grid.SetRow(_grid, 1);

        ShowPack(packs[0]);
    }

    /// <summary>
    /// Opens the picker and returns the chosen symbol, or null if the user cancelled.
    /// </summary>
    public static async Task<Symbol?> PickAsync(INavigation navigation)
    {
        var page = new SymbolPickerPage();
        await navigation.PushModalAsync(page);
        return await page._result.Task;
    }

    // The tablet's back button counts as Cancel.
    protected override bool OnBackButtonPressed()
    {
        _ = Finish(null);
        return true;
    }

    private static string PackName(SymbolPack pack) => pack switch
    {
        SymbolPack.Mulberry => "Pictures",
        _ => "Emoji",
    };

    private static Button CreateButton(string text, string color, Color textColor, Action action)
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

    // Choosing a pack clears the search, so the whole pack shows.
    private void ShowPack(SymbolPack pack)
    {
        _pack = pack;
        if (!string.IsNullOrEmpty(_search.Text))
        {
            _search.Text = string.Empty;
        }

        ShowSymbols();
    }

    private void ShowSymbols()
    {
        var search = _search.Text?.Trim();
        var searching = !string.IsNullOrEmpty(search);

        // While searching, no pack is highlighted: the results come from all of them.
        foreach (var (buttonPack, button) in _packButtons)
        {
            var selected = !searching && buttonPack == _pack;
            button.BackgroundColor = Color.FromArgb(selected ? SelectedColor : UnselectedColor);
            button.TextColor = selected ? Colors.White : Colors.Black;
        }

        _matches = SymbolCatalog.Available
            .Where(symbol => searching
                ? symbol.Label.Contains(search!, StringComparison.OrdinalIgnoreCase)
                : symbol.Pack == _pack)
            .ToList();

        _shown.Clear();
        ShowNextPage();
    }

    // Adds the next page of matches. This is called as the user nears the end of what is shown.
    private void ShowNextPage()
    {
        foreach (var symbol in _matches.Skip(_shown.Count).Take(PageSize))
        {
            _shown.Add(symbol);
        }
    }

    private View CreateItem()
    {
        var picture = new Image { Aspect = Aspect.AspectFit, HeightRequest = 80, WidthRequest = 80 };
        picture.SetBinding(Image.SourceProperty, nameof(Symbol.Image));

        var label = new Label
        {
            FontSize = 14,
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1,
        };
        label.SetBinding(Label.TextProperty, nameof(Symbol.Label));

        var cell = new VerticalStackLayout { Spacing = 2, Padding = new Thickness(4), Children = { picture, label } };
        cell.SetBinding(SemanticProperties.DescriptionProperty, nameof(Symbol.Label));

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            if (cell.BindingContext is Symbol symbol)
            {
                await Finish(symbol);
            }
        };
        cell.GestureRecognizers.Add(tap);
        return cell;
    }

    private async Task Finish(Symbol? symbol)
    {
        if (_finished)
        {
            return;
        }

        _finished = true;
        await Navigation.PopModalAsync();
        _result.TrySetResult(symbol);
    }
}
