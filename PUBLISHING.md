# Publishing Custom Soundboard Lite as an APK

## 1. Change the application ID (once, before the first release)

The application ID is `uk.co.dbak91.customsoundboardlite` (set in `CustomSoundboard.csproj`). Android treats two
apps with different IDs as separate apps, so change it before the tablet ever
installs a build.

## 2. Create the signing key (once, ever)

From the folder containing `CustomSoundboard.csproj`:

```powershell
.\scripts\New-SigningKey.ps1
```

It asks you to choose a password (or, with `-GeneratePassword`, makes a strong random one), then writes the key, a file holding its password, and the build
settings to `release-signing\` (excluded from Git). It refuses to run if a key already exists.

Back up the whole `release-signing\` folder somewhere safe, away from this PC. Every future update
must be signed with the same key. If the key is lost, tablets must uninstall the app first,
which deletes its tiles.

## 3. Build the signed APK

```powershell
.\scripts\Publish-Release.ps1
```

This builds the app, signs it with the release key, checks the signature, and writes:

```text
artifacts\release\<version>\CustomSoundboardLite-<version>.apk
```

## 4. Install on the tablet

- Copy the `.apk` to the tablet (USB, Google Drive, or email to yourself), open it, and allow
  "Install unknown apps" for that app when Android asks.
- Or with USB debugging enabled: `adb install -r <path-to-apk>`

## Updating later

Bump `ApplicationDisplayVersion` (for example `1.2`) and `ApplicationVersion` (increase by 1
every time), then repeat step 3 and install over the top.
