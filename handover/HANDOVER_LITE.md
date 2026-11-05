# Handover prompt: bring the Full app's menu and edit workflow to JAMESSOUNDS (Lite)

Paste everything below the line into the assistant working in the JAMESSOUNDS repository.

---

You are working in the JAMESSOUNDS repository, a fork of Custom Soundboard (a .NET MAUI Android app). JAMESSOUNDS is the **Lite**, legacy-device edition for the owner's younger brother, who has non-verbal autism and Down syndrome. He uses an old Android tablet, so the app must stay light, simple and predictable. Test on the old-device emulator only: AVD `Nexus_7_2012_API_21` (never the Pixel 7 emulator, which is for the Full edition). Find the emulator by AVD name with `adb emu avd name`, never by serial.

The **Full** edition lives in the sibling repository `C:\Users\PC\source\repos\CustomSoundboard`. Read its code and copy from it. This brief says what to bring across and what to leave out.

## Goal

Give Lite the same hamburger menu and edit-mode workflow as Full, with three differences:

1. **No AI voices.** Do not add sherpa-onnx, ONNX Runtime, Piper models, `NeuralVoice`, or `INeuralVoice`. No new native libraries.
2. **Voices come only from the device's own text-to-speech engine**, English (British) only. See "Voices".
3. **A much smaller symbol set**: 500 common pictures and 500 common emoji. See "Symbols".

Keep the existing built-in board (3 pages of default tiles) working as it does now.

## What to read in the Full repository first

- `MainPage.cs`: the board, the hamburger menu (`OpenMenu`, `ChooseAsync`, `AddMenuButton`), edit mode (`StartEdit`, `ConfirmEditAsync`, `CancelEditAsync`, `EndEdit`), per-tile editing (`EditUserTileAsync`, `EditAudioAsync`, `EditImageAsync`, `EditLabelAsync`, `PickSymbolAsync`, `ClearTile`, `ReplaceImage`, `ReplaceSound`) and `TextToAudioAsync`.
- `SpeechEditorPage.cs`: the text-to-audio screen.
- `SymbolPickerPage.cs` and `Models/SymbolCatalog.cs`: the symbol picker and the catalog.
- `Services/UserPageStore.cs`, `Services/ISpeechSynthesizer.cs`, `Platforms/Android/AndroidSpeechSynthesizer.cs`, `Platforms/Android/AndroidAudioRecorder.cs`.
- `APP_CONTEXT.md`: why the app exists and its principles (simple, accessible, dignified, free).

## The workflow to reproduce

**Hamburger menu.** A hamburger button at the top right of the board opens a centred panel with these buttons, in order: **Edit mode**, **Add page**, **Picture credits**, **Close**. The panel also answers the back button. `ChooseAsync` is the shared "pick one of these" panel that the other questions below reuse.

**Pages.** There are 3 built-in pages (tabs 1, 2, 3). **Add page** adds extra user pages as more tabs, which scroll sideways. Built-in pages can have their tiles edited too, but the built-in defaults are restored by the existing logic (`EnsureBuiltInPages`), so keep that behaviour.

**Edit mode.** Choosing Edit mode shows a coloured banner under the tabs: "EDIT MODE: tap a tile to change it. Changes go live when you confirm.", with **Cancel** and **Confirm** buttons. Every tile shows a small pencil badge and a speaker badge. Nothing is saved until Confirm.

- Work on a **draft copy** of the pages (`_pages`), separate from the live pages (`_live`). Outside edit mode they are the same object.
- Keep two lists of files: `_filesToDelete` (files the draft replaced; delete them on Confirm) and `_filesToDiscard` (files the draft created; delete them on Cancel). This keeps the device free of orphaned files.
- Cancel discards the draft and removes `_filesToDiscard`. Confirm saves the draft and removes `_filesToDelete`.

**Tapping a tile in edit mode** opens a panel titled "Page N, tile M" with: **Edit audio**, **Edit image**, **Edit label**, **Clear this tile**, then **Back**.

- **Edit audio** opens "…: audio" with **Text to audio**, **Upload own audio** (MP3, WAV or M4A), **Record audio**, then **Back**.
- **Edit image** opens "…: image" with **Choose a symbol**, **Upload image**, **Take image**, then **Back**.
- **Edit label** asks "What should this tile be called?" with a Save/Cancel prompt.
- **Clear this tile** empties it.

**Text to audio** is one screen (`SpeechEditorPage`): heading "What should this tile say?", a row of voice buttons (horizontally scrollable), one text entry, and three buttons: **Cancel**, **Hear it**, **Use this sound**.
- Do **not** focus the text entry automatically. The keyboard must open only when the user taps the field. (Full had a bug where it opened by itself and shoved the page around; do not repeat it.)
- Done on the keyboard plays the words, same as Hear it.
- The sound is made once per words and voice, then reused; nothing changes on the tile until **Use this sound**.
- Pressing Hear it or Use this sound with an empty field shows "Type some words first." and focusing the field then is fine, because the user asked for it.
- The result is saved as a WAV file through `UserPageStore.NewTileFile`.

**Symbol picker** (`SymbolPickerPage`): full-screen grid of 6 columns, a Cancel button, two pack buttons (**Pictures**, **Emoji**) and a search box on one row. The grid builds pictures a page at a time so it stays light.
- **Search must search both packs at once.** Typing "hello" must show "Hello" from Pictures and "Hello" from Emoji together. While searching, neither pack button is highlighted. Clearing the box returns to the chosen pack.
- Symbols are small WebP files loaded by name from the app package (`symbols/<name>.webp`), not MauiImages, so they are not packed at five screen sizes.

## Voices (Lite)

- Offer **Robot** (the device's default engine, `Locale en-GB` preferred) and the device's installed **English (British)** voices that run offline (`IsNetworkConnectionRequired == false`), named by what a person would understand: "English (British) female" and "English (British) male". The Full app's `AndroidSpeechSynthesizer` already lists British offline voices and names them from Google's codes (`gba`, `gbc` are women; `gbb`, `gbd` are men); reuse and tidy that.
- If the device has no British voice installed, show only Robot. If it has none at all, show "This device has no voice to turn words into sound." and do not open the editor.
- Do not download anything and do not bundle any voice. Old tablets may have only the basic engine, and that is fine.
- Remember that Android's TTS on very old versions (API 21) lacks some `Voice` APIs; guard calls and fall back to `SetLanguage`.

## Symbols (Lite)

Lists are ready in the Full repository under `handover/`:

- `handover/lite-pictures.csv`: 500 common Mulberry pictures (`asset,label`). These are the common everyday words (food, drink, toilet, feelings, actions, places, people, body, health, clothes, toys, animals) with the alphabet removed.
- `handover/lite-emoji.csv`: 500 common OpenMoji emoji (`asset,label`): 93 communication-focused ones already in Full, plus faces, hands, animals, food, drink, vehicles and everyday objects.

Steps:

1. Copy only the files named in those two lists from the Full repository's `Resources/Packs/Full/<asset>.webp` into the Lite repository. Do not copy the other ~4,200.
2. Replace Lite's `SymbolCatalog.cs` list with exactly those 1,000 entries (keep the `Symbol` record, `Credits`, `Available`, `ImageFor`).
3. Update `Resources/Packs/manifest.csv` to match and keep `ATTRIBUTION.md` and the in-app **Picture credits** text. Both packs are CC BY-SA 4.0 and require attribution (Mulberry Symbols by Steve Lee; OpenMoji).
4. Check every default tile on the three built-in pages still has its picture (`mb_want`, `icon_*` etc.). Keep any picture a default tile uses, even if it is not in the lists.
5. Keep images at 384px WebP for tiles, or shrink to 256px if the old tablet is slow. The Lite tablet has little memory, so test scrolling the picker on `Nexus_7_2012_API_21`.

## Things Lite must not break

- Minimum Android API stays at 21. Remove anything that needs newer APIs, or guard it.
- Keep the APK small and the app start fast. No AI packages, no large assets.
- Keep the default 3 pages and their sounds as they are in Lite. Do not replace Lite's recordings with the Full edition's regenerated AI sounds unless asked.
- Large touch targets and high contrast; no automatic keyboard; no surprise page movement.
- The signing files and release scripts (`release-signing/`, `scripts/Publish-Release.ps1`) are for the owner's Play Store publishing. Do not change or commit keys.

## How to work

- Read the Full code first, then port the workflow rather than rewriting it, so behaviour matches.
- Build with `dotnet build` and install to the Nexus 7 emulator (AVD `Nexus_7_2012_API_21`). Open the hamburger menu, enter edit mode, edit a tile's audio with Robot and a British voice, pick a symbol from both packs, search "hello" and confirm both packs appear, Confirm and Cancel an edit, and check no orphan files are left in app storage.
- Report what you ran and saw, and what you could not check.
- Do not commit unless asked.
