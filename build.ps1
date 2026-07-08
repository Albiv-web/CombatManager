[CmdletBinding()]
param(
    [ValidateSet('Release')]
    [string]$Configuration = 'Release',

    [switch]$Install
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$modName = 'CombatManager'
$packageSource = Join-Path $root $modName
$project = Join-Path $packageSource 'Source\CombatManager.csproj'
$verification = Join-Path $root 'tools\CombatManager.Verification\CombatManager.Verification.csproj'
$manifestPath = Join-Path $packageSource 'plugin.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$version = [string]$manifest.version

if ($version -notmatch '^\d+\.\d+\.\d+$') {
    throw "plugin.json version '$version' is not a three-part release version."
}

if ([string]::IsNullOrWhiteSpace($env:FTD_DIR) -or -not (Test-Path -LiteralPath $env:FTD_DIR)) {
    $defaultFtdDir = 'C:\Program Files (x86)\Steam\steamapps\common\From The Depths'
    if (Test-Path -LiteralPath $defaultFtdDir) {
        $env:FTD_DIR = $defaultFtdDir
    }
    else {
        throw 'FTD_DIR must point to the From The Depths installation.'
    }
}

$artifacts = Join-Path $root 'artifacts'
$stagingRoot = Join-Path $artifacts 'staging'
$stagedPackage = Join-Path $stagingRoot $modName
$zip = Join-Path $artifacts "$modName-$version.zip"
$buildOutput = Join-Path $packageSource 'Source\bin\Release\netstandard2.1'
$buildDll = Join-Path $buildOutput "$modName.dll"
$packageDll = Join-Path $packageSource "$modName.dll"

dotnet build $project -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw 'Mod build failed.' }

dotnet restore $verification --nologo
if ($LASTEXITCODE -ne 0) { throw 'Verification restore failed.' }

dotnet format $project --verify-no-changes --no-restore --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Formatting verification failed.' }

dotnet format $verification --verify-no-changes --no-restore --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw 'Verification formatting failed.' }

dotnet run --project $verification -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Verification failed.' }

if (-not (Test-Path -LiteralPath $buildDll) -or -not (Test-Path -LiteralPath $packageDll)) {
    throw 'The Release assembly or packaged assembly is missing.'
}

$buildDllHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $buildDll).Hash
$packageDllHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $packageDll).Hash
if ($buildDllHash -ne $packageDllHash) {
    throw "The packaged $modName.dll is stale relative to the Release build."
}

if (Get-ChildItem -LiteralPath $buildOutput -Recurse -File -Filter '*.pdb') {
    throw 'Release output contains a PDB.'
}

$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($packageDll).Version
$versionParts = $version.Split('.')
if ($assemblyVersion.Major -ne [int]$versionParts[0] -or
    $assemblyVersion.Minor -ne [int]$versionParts[1] -or
    $assemblyVersion.Build -ne [int]$versionParts[2]) {
    throw "Assembly version $assemblyVersion does not match plugin.json version $version."
}

if (Test-Path -LiteralPath $stagingRoot) {
    $resolved = (Resolve-Path -LiteralPath $stagingRoot).Path
    $resolvedArtifacts = (Resolve-Path -LiteralPath $artifacts).Path
    if (-not $resolved.StartsWith($resolvedArtifacts, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove unexpected staging path: $resolved"
    }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}

New-Item -ItemType Directory -Path $stagedPackage -Force | Out-Null

$runtimeFiles = @(
    "$modName.dll",
    'plugin.json',
    'header.header',
    'releases',
    'README.md'
)

foreach ($relative in $runtimeFiles) {
    Copy-Item -LiteralPath (Join-Path $packageSource $relative) -Destination $stagedPackage
}

$allowedTopLevel = $runtimeFiles
$unexpectedTopLevel = Get-ChildItem -LiteralPath $stagedPackage -Force | Where-Object {
    $_.Name -notin $allowedTopLevel
}
if ($unexpectedTopLevel) {
    throw 'Unexpected top-level package entries: ' +
          (($unexpectedTopLevel | Select-Object -ExpandProperty Name) -join ', ')
}

$forbidden = Get-ChildItem -LiteralPath $stagedPackage -Recurse -Force | Where-Object {
    $_.Name -in @('Source', '.vs', 'bin', 'obj', 'ModAssemblySelector.dll', 'AssemblyFilePath.txt') -or
    $_.Extension -in @('.pdb', '.user', '.suo')
}
if ($forbidden) {
    throw 'Forbidden files entered the runtime package: ' +
          (($forbidden | Select-Object -ExpandProperty FullName) -join ', ')
}

$dlls = @(Get-ChildItem -LiteralPath $stagedPackage -Recurse -File -Filter '*.dll')
if ($dlls.Count -ne 1 -or $dlls.Name -notcontains "$modName.dll") {
    throw "Runtime package must contain only $modName.dll."
}

if (-not (Test-Path -LiteralPath $artifacts)) {
    New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
}
if (Test-Path -LiteralPath $zip) {
    Remove-Item -LiteralPath $zip -Force
}

Add-Type -AssemblyName System.IO.Compression
$zipStream = $null
$archive = $null
try {
    $zipStream = [IO.File]::Open($zip, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    $archive = New-Object IO.Compression.ZipArchive(
        $zipStream,
        [IO.Compression.ZipArchiveMode]::Create,
        $false)
    $fixedTimestamp = New-Object DateTimeOffset(1980, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
    $stagedRootPath = (Resolve-Path -LiteralPath $stagedPackage).Path.TrimEnd('\') + '\'
    $files = Get-ChildItem -LiteralPath $stagedPackage -Recurse -File | Sort-Object {
        $_.FullName.Substring($stagedRootPath.Length).Replace('\', '/')
    }
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($stagedRootPath.Length).Replace('\', '/')
        $entry = $archive.CreateEntry(
            "$modName/" + $relative,
            [IO.Compression.CompressionLevel]::Optimal)
        $entry.LastWriteTime = $fixedTimestamp
        $input = $null
        $output = $null
        try {
            $input = [IO.File]::OpenRead($file.FullName)
            $output = $entry.Open()
            $input.CopyTo($output)
        }
        finally {
            if ($output) { $output.Dispose() }
            if ($input) { $input.Dispose() }
        }
    }
}
catch {
    if ($archive) { $archive.Dispose(); $archive = $null }
    if ($zipStream) { $zipStream.Dispose(); $zipStream = $null }
    if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
    throw
}
finally {
    if ($archive) { $archive.Dispose() }
    if ($zipStream) { $zipStream.Dispose() }
}

if ($Install) {
    $modsDir = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'From The Depths\Mods'
    $installPath = Join-Path $modsDir $modName
    if (-not (Test-Path -LiteralPath $modsDir)) {
        New-Item -ItemType Directory -Path $modsDir -Force | Out-Null
    }
    if (Test-Path -LiteralPath $installPath) {
        $resolvedInstallPath = (Resolve-Path -LiteralPath $installPath).Path
        $resolvedModsDir = (Resolve-Path -LiteralPath $modsDir).Path
        if (-not $resolvedInstallPath.StartsWith($resolvedModsDir, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove unexpected install path: $resolvedInstallPath"
        }
        Remove-Item -LiteralPath $resolvedInstallPath -Recurse -Force
    }
    Copy-Item -LiteralPath $stagedPackage -Destination $modsDir -Recurse
    Write-Host "Installed $modName to $installPath"
}

$hash = Get-FileHash -Algorithm SHA256 -LiteralPath $zip
Write-Host "Created $zip"
Write-Host "SHA256 $($hash.Hash)"
