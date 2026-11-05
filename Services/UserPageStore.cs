using System.Diagnostics;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace CustomSoundboard.Services;

/// <summary>
/// One tile the user filled in: a picture and a sound, both copied into app storage.
/// </summary>
public sealed class UserTile
{
    // Starter tiles use packaged emoji/images and sounds. A user-selected file replaces
    // the corresponding packaged value but is kept in the same editable tile model.
    public string? Emoji { get; set; }

    public string? Label { get; set; }

    public string? Color { get; set; }

    public string? ImageAsset { get; set; }

    public string? ImagePath { get; set; }

    public string? SoundAsset { get; set; }

    public string? SoundPath { get; set; }
}

/// <summary>
/// The user's extra pages, in board order, with the ten tiles on each.
/// </summary>
public sealed class UserPages
{
    public const int TilesPerPage = 10;

    // The three starter pages are saved too, which lets edit mode change them.
    public List<UserTile[]> BuiltInPages { get; set; } = [];

    // The menu allows at most this many extra pages.
    public const int MaxExtraPages = 5;

    public List<UserTile[]> Pages { get; set; } = [];
}

/// <summary>
/// Loads and saves the extra pages as JSON in app data, with the picture and sound files beside it.
/// </summary>
public static class UserPageStore
{
    private static readonly string StatePath = Path.Combine(FileSystem.AppDataDirectory, "user_pages.json");

    public static readonly string TilesDirectory = Path.Combine(FileSystem.AppDataDirectory, "tiles");

    public static UserPages Load()
    {
        UserPages? state = null;
        try
        {
            if (File.Exists(StatePath))
            {
                var json = File.ReadAllText(StatePath);
                state = JsonSerializer.Deserialize<UserPages>(json);

                // Saves from the first version also kept a count of switched-on pages; keep only those.
                using var document = JsonDocument.Parse(json);
                if (state is not null && document.RootElement.TryGetProperty("ExtraPages", out var count))
                {
                    state.Pages = state.Pages.Take(count.GetInt32()).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            // A damaged file must not stop the board opening: start with no extra pages.
            Debug.WriteLine($"CustomSoundboard: could not read {StatePath}: {ex.Message}");
        }

        state ??= new UserPages();
        state.Pages ??= [];
        state.BuiltInPages ??= [];

        // Every page always has ten tiles, even if the saved file is short or missing some.
        state.Pages = state.Pages
            .Take(UserPages.MaxExtraPages)
            .Select(NormalizePage)
            .ToList();

        state.BuiltInPages = state.BuiltInPages
            .Take(3)
            .Select(NormalizePage)
            .ToList();

        return state;
    }

    private static UserTile[] NormalizePage(UserTile[]? page) =>
        Enumerable.Range(0, UserPages.TilesPerPage)
            .Select(index => page is not null && index < page.Length ? page[index] ?? new UserTile() : new UserTile())
            .ToArray();

    public static void Save(UserPages state)
    {
        File.WriteAllText(StatePath, JsonSerializer.Serialize(state));
    }

    /// <summary>
    /// A separate deep copy, so edits can be made to it and thrown away without touching the live pages.
    /// </summary>
    public static UserPages Copy(UserPages state) =>
        JsonSerializer.Deserialize<UserPages>(JsonSerializer.Serialize(state))!;

    /// <summary>
    /// A new, unique file name for a tile's picture or sound. A new name each time stops old copies being shown or played.
    /// </summary>
    public static string NewTileFile(int page, int tile, string kind, string extension)
    {
        Directory.CreateDirectory(TilesDirectory);
        return Path.Combine(TilesDirectory, $"p{page}_t{tile}_{kind}_{DateTime.UtcNow.Ticks}{extension}");
    }

    public static void Delete(string? path)
    {
        if (path is not null && File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
