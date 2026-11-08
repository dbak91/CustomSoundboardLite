# Custom Soundboard Lite

The lightweight edition of [Custom Soundboard](https://github.com/dbak91/CustomSoundboard), made for old 32-bit tablets such as the 2012 Nexus 7 (Android 5.0). It has no AI voice models: robot voices use the device's own text-to-speech, and 1,000 pictures are included. It has its own package ID (`uk.co.dbak91.customsoundboardlite`) and signing key, so it installs alongside the Full app and never replaces it.

A big-button sound board for an Android tablet. Each tile is a large picture on a
colour, and tapping it plays a spoken word. Built with .NET MAUI.

## Tiles

The default tiles use the speech clips in `Resources/Sounds` and the icons in
`Resources/Images`. Pages one to three are the built-in pages. Extra pages (4 to 6) can be
added from the menu, and each tile on them can take its own picture and sound.

To change the built-in tiles, edit `PageOneTiles`, `PageTwoTiles` and `PageThreeTiles` in
`MainPage.cs`. Each tile is a `new SoundTile(emoji, label, colour, soundFile, imageName)`
entry, and its sound file goes in `Resources/Sounds`.

The app runs in landscape on a grid, sized for wide-screen tablets. Page one has five
columns across two rows.

## Download

Get the latest APK from the [Releases page](https://github.com/dbak91/CustomSoundboardLite/releases/latest). Each release has two separate files, so pick the one that matches your tablet:

| File | For |
| --- | --- |
| `CustomSoundboardLite-<version>-armeabi-v7a.apk` | 32-bit devices, including the Nexus 7 (2012) |
| `CustomSoundboardLite-<version>-arm64-v8a.apk` | 64-bit devices |

Not sure which one you have? If the device is older than about 2015, choose the 32-bit file. Otherwise try the 64-bit file first. Install by opening the APK and allowing "Install unknown apps" when Android asks.

## Requirements

- .NET 10 SDK with the MAUI Android workload
- Android SDK 36
- Visual Studio 2026 or the .NET CLI
- Android 5.0 (API 21) or later

## Build

```powershell
dotnet build CustomSoundboard.csproj
```

## Publish

See [PUBLISHING.md](PUBLISHING.md).

## Licence

Custom Soundboard is free software, released under the GNU General Public License v3.0 (see `LICENSE`).
The pictures (Mulberry Symbols, OpenMoji) are CC BY-SA 4.0; see `Resources/Packs/ATTRIBUTION.md` and the in-app Credits.
Voices are robot voices made from the device's own text-to-speech; no voice models are bundled.
