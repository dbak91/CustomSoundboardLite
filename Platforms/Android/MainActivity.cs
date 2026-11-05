using Android.App;
using Android.Content.PM;
using Android.OS;

// Taking pictures needs this; Android asks the user on first use.
[assembly: UsesPermission(Android.Manifest.Permission.Camera)]
// Recording sound is used for the tile audio. Speech uses the device's own voices, offline.
[assembly: UsesPermission(Android.Manifest.Permission.RecordAudio)]

namespace CustomSoundboard;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    ConfigurationChanges = ConfigChanges.ScreenSize
        | ConfigChanges.Orientation
        | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
