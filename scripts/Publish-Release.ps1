<#
.SYNOPSIS
    Builds the signed 32-bit (ARM) and 64-bit (ARM64) release APKs of the Lite app.

.DESCRIPTION
    Each APK is built separately, signed with the release key from scripts\New-SigningKey.ps1, then checked, and
    copied to artifacts\release\<version>\ as CustomSoundboardLite-<version>-armeabi-v7a.apk (32-bit, e.g. Nexus 7)
    and CustomSoundboardLite-<version>-arm64-v8a.apk (64-bit).

    Before each release, raise ApplicationDisplayVersion and ApplicationVersion in CustomSoundboard.csproj,
    or the tablets will not accept the new APK as an update.
#>
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root "CustomSoundboard.csproj"
$framework = "net10.0-android36.0"
$signing = Join-Path $root "release-signing\CustomSoundboardLite.signing.props"

if (-not (Test-Path $signing)) {
    Write-Error "There is no release signing key yet. Run .\scripts\New-SigningKey.ps1 once first."
}

[xml]$csproj = Get-Content $project
$version = ($csproj.Project.PropertyGroup | Where-Object { $_.ApplicationDisplayVersion } | Select-Object -First 1).ApplicationDisplayVersion
$output = Join-Path $root "artifacts\release\$version"
New-Item -ItemType Directory -Force $output | Out-Null

# apksigner checks each APK is signed, and shows which key signed it.
$sdk = @($env:ANDROID_HOME, "C:\Android\Sdk", "${env:ProgramFiles(x86)}\Android\android-sdk", "$env:LOCALAPPDATA\Android\Sdk") |
    Where-Object { $_ -and (Test-Path "$_\build-tools") } | Select-Object -First 1
$apksigner = if ($sdk) {
    Get-ChildItem "$sdk\build-tools\*\apksigner.bat" | Sort-Object { [version]$_.Directory.Name } | Select-Object -Last 1
}

# Runtime identifier => ABI label used in the APK file name.
$targets = [ordered]@{
    "android-arm"   = "armeabi-v7a"   # 32-bit: Nexus 7 (2012) and other old tablets
    "android-arm64" = "arm64-v8a"     # 64-bit: newer tablets and phones
}

foreach ($rid in $targets.Keys) {
    $abi = $targets[$rid]
    Write-Host "Building $rid ($abi) $version..." -ForegroundColor Cyan
    & dotnet publish $project -c Release -f $framework -r $rid -p:AndroidPackageFormat=apk -nologo -v q
    if ($LASTEXITCODE -ne 0) {
        Write-Error "The $rid build failed."
    }

    $apk = Get-ChildItem (Join-Path $root "bin\Release\$framework\$rid\publish\*-Signed.apk") |
        Sort-Object LastWriteTime | Select-Object -Last 1
    $target = Join-Path $output "CustomSoundboardLite-$version-$abi.apk"
    Copy-Item $apk.FullName $target -Force

    if ($apksigner) {
        $report = & $apksigner.FullName verify --print-certs $target
        if ($LASTEXITCODE -ne 0) {
            Write-Error "$target is not correctly signed."
        }

        if ($report -match "CN=Android Debug") {
            Write-Error "$target was signed with the debug key, not the release key."
        }
    }

    $size = [math]::Round((Get-Item $target).Length / 1MB, 1)
    Write-Host "  $target ($size MB)" -ForegroundColor Green
}

if (-not $apksigner) {
    Write-Warning "apksigner was not found, so the signatures were not checked."
}
