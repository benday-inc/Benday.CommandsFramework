# Copies the highest version nupkg for each package to the local NuGet feed folder.

$scriptDir = $PSScriptRoot

if ($IsWindows -or $env:OS -match "Windows") {
    $destDir = "C:\LocalNuGet"
} else {
    $destDir = Join-Path $HOME "LocalNuGet"
}

if (-not (Test-Path $destDir)) {
    New-Item -ItemType Directory -Path $destDir | Out-Null
    Write-Host "Created directory: $destDir"
}

# Splits a nupkg base name into package id + sortable version parts.
# NuGet names files "{id}.{version}", so the version starts at the first
# dot-segment that begins with a digit.
function Get-PackageInfo {
    param([System.IO.FileInfo] $File)

    if ($File.BaseName -notmatch '^(.+?)\.(\d+(?:\.\d+)+.*)$') {
        return $null
    }

    $packageName = $Matches[1]
    $version     = $Matches[2]

    # Strip build metadata (1.2.3+abc0def), then split off the prerelease tag.
    $version = ($version -split '\+', 2)[0]
    $parts   = $version -split '-', 2
    $core    = $parts[0]
    $pre     = if ($parts.Count -gt 1) { $parts[1] } else { '' }

    $parsed = [System.Version]"0.0.0"
    if (-not [System.Version]::TryParse($core, [ref] $parsed)) {
        $parsed = [System.Version]"0.0.0"
    }

    # Zero-pad digit runs so "alpha10" sorts after "alpha2" as a plain string.
    $preKey = [regex]::Replace($pre, '\d+', { param($m) $m.Value.PadLeft(10, '0') })

    [PSCustomObject]@{
        PackageName = $packageName
        Version     = $parsed
        # A release beats any prerelease of the same version (semver).
        IsRelease   = [int](-not $pre)
        PreKey      = $preKey
        File        = $File
    }
}

# Find all nupkg files (exclude obj folders and snupkg)
$allPackages = Get-ChildItem -Path $scriptDir -Recurse -Filter "*.nupkg" |
    Where-Object { $_.FullName -notmatch '[/\\]obj[/\\]' -and $_.Extension -eq '.nupkg' }

$grouped = $allPackages |
    ForEach-Object { Get-PackageInfo $_ } |
    Where-Object { $null -ne $_ } |
    Group-Object PackageName

foreach ($group in $grouped) {
    $latest = $group.Group |
        Sort-Object -Property Version, IsRelease, PreKey -Descending |
        Select-Object -First 1

    $dest = Join-Path $destDir $latest.File.Name
    Copy-Item -Path $latest.File.FullName -Destination $dest -Force
    Write-Host "Copied $($latest.File.Name) -> $destDir"
}

Write-Host ""
Write-Host "Done. $($grouped.Count) package(s) copied to $destDir"
