using Android.Media;
using Microsoft.Maui.Storage;

namespace CustomSoundboard.Services;

public class AndroidSoundPlayer : ISoundPlayer
{
    private MediaPlayer? _player;

    // Taps can arrive faster than a copy finishes, so only one tap plays at a time.
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task PlayAsync(string assetName)
    {
        await _gate.WaitAsync();
        try
        {
            // Stop the old sound before copying: the copy overwrites the cache file it may still be reading.
            StopCurrent();

            // A full path is a recording the user made or picked; anything else is a packaged sound.
            var localPath = Path.IsPathRooted(assetName) ? assetName : await CopyAssetToCacheAsync(assetName);

            var player = new MediaPlayer();
            player.SetDataSource(localPath);
            player.Prepare();
            player.Start();
            _player = player;
        }
        finally
        {
            _gate.Release();
        }
    }

    // A long recording (such as a full song) must stop when a new tile is pressed.
    private void StopCurrent()
    {
        if (_player is null)
        {
            return;
        }

        try
        {
            _player.Stop();
        }
        catch (Java.Lang.IllegalStateException)
        {
            // Already stopped or still preparing: releasing it below is enough.
        }

        _player.Release();
        _player = null;
    }

    // MediaPlayer needs a file path, so each packaged sound is copied to the cache.
    // Always overwrite: a stale copy from an earlier build would otherwise keep playing.
    private static async Task<string> CopyAssetToCacheAsync(string assetName)
    {
        var localPath = Path.Combine(FileSystem.CacheDirectory, Path.GetFileName(assetName));

        await using var source = await FileSystem.OpenAppPackageFileAsync(assetName);
        await using var target = File.Create(localPath);
        await source.CopyToAsync(target);
        return localPath;
    }
}
