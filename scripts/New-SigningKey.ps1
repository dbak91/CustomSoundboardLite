<#
.SYNOPSIS
    Creates the release signing key that signs the app. Run it once, ever.

.DESCRIPTION
    Writes four files to release-signing\ (which Git ignores):
      CustomSoundboardLite.keystore       the key itself
      store-password.txt              its password, read by the build
      key-password.txt                the same password again (see below)
      CustomSoundboardLite.signing.props  tells the build to sign with the key

    Every future update of Lite must be signed with this same key, so back up the whole
    release-signing\ folder somewhere safe (not only on this PC). If it is lost, tablets must uninstall
    the app, losing its tiles, before a new version will install.
#>
param(
    # The name recorded in the key's certificate.
    [string]$Name = "Custom Soundboard Lite",

    # Make a strong random password instead of asking for one. It is only saved in the password files.
    [switch]$GeneratePassword
)

$ErrorActionPreference = "Stop"
$folder = Join-Path (Split-Path $PSScriptRoot -Parent) "release-signing"
$keystore = Join-Path $folder "CustomSoundboardLite.keystore"

# Never replace an existing key: anything signed with it could no longer be updated.
if (Test-Path $keystore) {
    Write-Error "A signing key already exists at $keystore. Keep using it; do not make a new one."
}

if (-not (Get-Command keytool -ErrorAction SilentlyContinue)) {
    Write-Error "keytool was not found. Install a Java JDK (for example Eclipse Adoptium) and try again."
}

if ($GeneratePassword) {
    # Letters and digits only, so it is safe to pass to keytool on the command line.
    $alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789".ToCharArray()
    $bytes = New-Object byte[] 32
    [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $plain = -join ($bytes | ForEach-Object { $alphabet[$_ % $alphabet.Length] })
} else {
    $first = Read-Host "Choose a password for the signing key (at least 6 characters)" -AsSecureString
    $second = Read-Host "Type it again" -AsSecureString
    $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($first))
    $check = [Runtime.InteropServices.Marshal]::PtrToStringBSTR([Runtime.InteropServices.Marshal]::SecureStringToBSTR($second))
    if ($plain -ne $check) {
        Write-Error "The two passwords were different. Nothing was created."
    }
    if ($plain.Length -lt 6) {
        Write-Error "The password must be at least 6 characters. Nothing was created."
    }
}

New-Item -ItemType Directory -Force $folder | Out-Null

# A PKCS12 keystore uses one password for both the store and the key.
& keytool -genkeypair -keystore $keystore -storetype PKCS12 -alias customsoundboardlite -keyalg RSA -keysize 2048 `
    -validity 10000 -dname "CN=$Name" -storepass $plain -keypass $plain
if ($LASTEXITCODE -ne 0) {
    Write-Error "keytool could not create the key."
}

# The build reads the passwords from these files ("file:" below), so no password is written into a build file.
# Two files, because apksigner reads the next line each time it is given the same file.
[IO.File]::WriteAllText((Join-Path $folder "store-password.txt"), $plain)
[IO.File]::WriteAllText((Join-Path $folder "key-password.txt"), $plain)

$props = @'
<Project>
  <!-- Written by scripts\New-SigningKey.ps1. Signs release builds. -->
  <PropertyGroup Condition="'$(Configuration)' == 'Release'">
    <AndroidKeyStore>true</AndroidKeyStore>
    <AndroidSigningKeyStore>$(MSBuildThisFileDirectory)CustomSoundboardLite.keystore</AndroidSigningKeyStore>
    <AndroidSigningKeyAlias>customsoundboardlite</AndroidSigningKeyAlias>
    <AndroidSigningKeyPass>file:$(MSBuildThisFileDirectory)key-password.txt</AndroidSigningKeyPass>
    <AndroidSigningStorePass>file:$(MSBuildThisFileDirectory)store-password.txt</AndroidSigningStorePass>
  </PropertyGroup>
</Project>
'@
[IO.File]::WriteAllText((Join-Path $folder "CustomSoundboardLite.signing.props"), $props)

Write-Host ""
Write-Host "Signing key created in $folder" -ForegroundColor Green
Write-Host "Back up that whole folder now (for example to a USB stick and a cloud drive)." -ForegroundColor Yellow
Write-Host "Then build both apps with: .\scripts\Publish-Release.ps1"
