param([string]$SourceRoot = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)))

$ErrorActionPreference = 'Stop'
$SourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
$importer = Join-Path $SourceRoot 'Tools\Import-IntoUnityProject.ps1'
$fixture = Join-Path ([IO.Path]::GetTempPath()) ('MoriEchoes_ImportSafety_' + [Guid]::NewGuid().ToString('N'))
$checks = 0

function Assert-Check([bool]$Condition, [string]$Message) {
    if (!$Condition) { throw "FAIL: $Message" }
    $script:checks++
}

function Write-FixtureFile([string]$Path, [string]$Content) {
    [IO.Directory]::CreateDirectory((Split-Path -Parent $Path)) | Out-Null
    [IO.File]::WriteAllText($Path, $Content)
}

function Snapshot([string]$Path) {
    return ((Get-ChildItem -LiteralPath $Path -File -Recurse | Sort-Object FullName | ForEach-Object {
        $_.FullName.Substring($Path.Length) + ':' + (Get-FileHash -LiteralPath $_.FullName).Hash
    }) -join "`n")
}

try {
    $source = Join-Path $fixture 'Source'
    $target = Join-Path $fixture 'Target'
    $invalid = Join-Path $fixture 'NotAProject'
    [IO.Directory]::CreateDirectory($invalid) | Out-Null
    [IO.Directory]::CreateDirectory((Join-Path $source 'Assets\Art\EmptyArtFolder')) | Out-Null
    Write-FixtureFile (Join-Path $source 'Assets\Fixture.txt') 'source asset fixture only'
    Write-FixtureFile (Join-Path $source 'docs\Fixture.md') 'source document fixture only'
    Write-FixtureFile (Join-Path $source 'START_HERE.md') 'fixture readme'
    Write-FixtureFile (Join-Path $target 'ProjectSettings\ProjectVersion.txt') 'm_EditorVersion: 6000.0.0f1'
    Write-FixtureFile (Join-Path $target 'Packages\manifest.json') '{"dependencies":{"com.unity.inputsystem":"fixture","com.unity.render-pipelines.universal":"fixture","com.unity.ugui":"fixture"}}'
    [IO.Directory]::CreateDirectory((Join-Path $target 'Assets')) | Out-Null
    Write-FixtureFile (Join-Path $target 'docs\Fixture.md') 'protected target document'
    $manifestHash = (Get-FileHash -LiteralPath (Join-Path $target 'Packages\manifest.json')).Hash
    $versionHash = (Get-FileHash -LiteralPath (Join-Path $target 'ProjectSettings\ProjectVersion.txt')).Hash

    $rejected = $false
    try { & $importer -SourcePath $source -ProjectPath $invalid | Out-Null } catch { $rejected = $true }
    Assert-Check $rejected 'Missing project markers must be rejected.'

    $before = Snapshot $target
    & $importer -SourcePath $source -ProjectPath $target | Out-Null
    Assert-Check ((Snapshot $target) -eq $before) 'Dry run must not write files.'
    Assert-Check (!(Test-Path -LiteralPath (Join-Path $target 'Assets\Art'))) 'Dry run must not create directories.'

    & $importer -SourcePath $source -ProjectPath $target -Apply | Out-Null
    Assert-Check ((Get-Content -LiteralPath (Join-Path $target 'Assets\Fixture.txt') -Raw) -eq 'source asset fixture only') 'New asset must be copied.'
    Assert-Check ((Get-Content -LiteralPath (Join-Path $target 'docs\Fixture.md') -Raw) -eq 'protected target document') 'Conflicting document must be preserved.'
    Assert-Check (Test-Path -LiteralPath (Join-Path $target 'Assets\Art\EmptyArtFolder') -PathType Container) 'Empty art skeleton must be created.'
    Assert-Check ((Get-FileHash -LiteralPath (Join-Path $target 'Packages\manifest.json')).Hash -eq $manifestHash) 'Manifest must remain unchanged.'
    Assert-Check ((Get-FileHash -LiteralPath (Join-Path $target 'ProjectSettings\ProjectVersion.txt')).Hash -eq $versionHash) 'Editor version must remain unchanged.'

    $before = Snapshot $target
    & $importer -SourcePath $source -ProjectPath $target -Apply | Out-Null
    Assert-Check ((Snapshot $target) -eq $before) 'Repeated import must be idempotent.'

    Write-FixtureFile (Join-Path $source 'Assets\Fixture.txt') 'conflicting changed source'
    Write-FixtureFile (Join-Path $source 'Assets\ShouldNotCopy.txt') 'must not copy on conflict'
    $before = Snapshot $target
    $rejected = $false
    try { & $importer -SourcePath $source -ProjectPath $target -Apply | Out-Null } catch { $rejected = $true }
    Assert-Check $rejected 'Conflicting asset must reject the entire import.'
    Assert-Check ((Snapshot $target) -eq $before) 'Asset conflict must not produce partial file writes.'
    Assert-Check (!(Test-Path -LiteralPath (Join-Path $target 'Assets\ShouldNotCopy.txt'))) 'Other files must remain uncopied when any asset conflicts.'

    $rejected = $false
    try { & $importer -SourcePath $source -ProjectPath $source -Apply | Out-Null } catch { $rejected = $true }
    Assert-Check $rejected 'Self import must be rejected.'
    Write-Output "PASS: $checks import safety checks using isolated synthetic fixtures; NOT a Unity import or compilation."
}
finally {
    $temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
    $resolvedFixture = [IO.Path]::GetFullPath($fixture)
    if (!$resolvedFixture.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase) -or
        !(Split-Path -Leaf $resolvedFixture).StartsWith('MoriEchoes_ImportSafety_', [StringComparison]::Ordinal)) {
        throw 'Unsafe temporary cleanup path; cleanup refused.'
    }
    if (Test-Path -LiteralPath $resolvedFixture) { Remove-Item -LiteralPath $resolvedFixture -Recurse -Force }
}
