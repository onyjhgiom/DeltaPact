param(
    [Parameter(Mandatory = $true)][string]$ProjectPath,
    [string]$SourcePath = (Split-Path -Parent $PSScriptRoot),
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'
$source = (Resolve-Path -LiteralPath $SourcePath).Path.TrimEnd('\', '/')
$target = (Resolve-Path -LiteralPath $ProjectPath).Path.TrimEnd('\', '/')
if ($source.Equals($target, [StringComparison]::OrdinalIgnoreCase) -or $target.StartsWith($source + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Target must be a separate Unity project, not the source folder or one of its children.'
}
$versionFile = Join-Path $target 'ProjectSettings\ProjectVersion.txt'
$manifestFile = Join-Path $target 'Packages\manifest.json'
if (!(Test-Path -LiteralPath $versionFile) -or !(Test-Path -LiteralPath $manifestFile) -or !(Test-Path -LiteralPath (Join-Path $target 'Assets'))) {
    throw 'Target is not a valid Unity project (Assets, Packages/manifest.json and ProjectSettings/ProjectVersion.txt required).'
}
$version = Get-Content -LiteralPath $versionFile -Raw
if ($version -notmatch '(?m)^m_EditorVersion:\s*(6000\.[^\r\n]+)') { throw 'Only a Unity 6 (6000.x) target is supported. No automatic upgrades will be made.' }
"Target Unity: $($Matches[1])"
$manifest = Get-Content -LiteralPath $manifestFile -Raw | ConvertFrom-Json
if (!$manifest.dependencies.'com.unity.inputsystem') { Write-Warning 'Input System is not declared. Install it in Unity Package Manager; this script will NOT change the manifest.' }
if (!$manifest.dependencies.'com.unity.render-pipelines.universal') { Write-Warning 'URP is not declared. Use Universal 2D; this script will NOT change rendering settings.' }
if (!$manifest.dependencies.'com.unity.ugui') { Write-Warning 'Unity UI (UGUI) is not declared. Install Unity UI in Package Manager before importing the HUD source.' }
$relativeFiles = @()
$relativeDirectories = @()
foreach ($directory in @('Assets', 'docs', 'prompts', 'CodexPrompts', 'Tools')) {
    $folder = Join-Path $source $directory
    if (Test-Path -LiteralPath $folder) {
        $relativeFiles += Get-ChildItem -LiteralPath $folder -Recurse -File | ForEach-Object { $_.FullName.Substring($source.Length + 1) }
        $relativeDirectories += $directory
        $relativeDirectories += Get-ChildItem -LiteralPath $folder -Recurse -Directory | ForEach-Object { $_.FullName.Substring($source.Length + 1) }
    }
}
foreach ($name in @('START_HERE.md', 'README_NEXT_STEPS.md')) {
    if (Test-Path -LiteralPath (Join-Path $source $name)) { $relativeFiles += $name }
}
$plan = @()
$assetConflicts = @()
foreach ($relative in ($relativeDirectories | Sort-Object -Unique)) {
    $destination = Join-Path $target $relative
    if ((Test-Path -LiteralPath $destination) -and !(Test-Path -LiteralPath $destination -PathType Container)) {
        throw "A file occupies an expected directory; NO files have been copied: $destination"
    }
}
foreach ($relative in ($relativeFiles | Sort-Object -Unique)) {
    $from = Join-Path $source $relative
    $to = [IO.Path]::GetFullPath((Join-Path $target $relative))
    if (!$to.StartsWith($target + '\', [StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe target: $to" }
    $action = 'ADD'
    if (Test-Path -LiteralPath $to) {
        if ((Get-FileHash -LiteralPath $from).Hash -eq (Get-FileHash -LiteralPath $to).Hash) { $action = 'IDENTICAL' }
        else {
            $action = 'PRESERVE_CONFLICT'
            if ($relative.StartsWith('Assets\', [StringComparison]::OrdinalIgnoreCase)) { $assetConflicts += $to }
        }
    }
    $plan += [pscustomobject]@{ Action = $action; RelativePath = $relative; Source = $from; Destination = $to }
}
$plan | Select-Object Action, RelativePath | Format-Table -AutoSize
if ($assetConflicts.Count -gt 0) { throw ("Asset conflicts found; NO files have been copied. Resolve manually or use a separate clean project:`n" + ($assetConflicts -join "`n")) }
if (!$Apply) { 'DRY RUN ONLY. Add -Apply after checking this plan. No destination files changed.'; return }
$added = 0
foreach ($relative in ($relativeDirectories | Sort-Object -Unique)) {
    New-Item -ItemType Directory -Force -Path (Join-Path $target $relative) | Out-Null
}
foreach ($item in $plan) {
    if ($item.Action -ne 'ADD') { continue }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $item.Destination) | Out-Null
    [IO.File]::Copy($item.Source, $item.Destination, $false)
    $added++
}
"Added $added files. Existing files were never overwritten. ProjectSettings and Packages were not modified."
