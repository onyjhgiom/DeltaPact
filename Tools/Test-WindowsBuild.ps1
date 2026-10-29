param(
    [Parameter(Mandatory = $true)][string]$Executable,
    [int[]]$Widths = @(1280, 1600, 1920),
    [string]$ResultsRoot = (Join-Path (Split-Path -Parent $PSScriptRoot) 'TestResults')
)

$ErrorActionPreference = 'Stop'
$game = (Resolve-Path -LiteralPath $Executable).Path
if ([IO.Path]::GetFileName($game) -ne 'MoriEchoes.exe') { throw 'Select an actual MoriEchoes.exe Development build.' }
foreach ($width in $Widths) {
    if ($width -notin @(1280, 1600, 1920)) { throw 'Supported test widths are 1280, 1600 and 1920 (16:9).' }
}
$root = [IO.Path]::GetFullPath($ResultsRoot)
New-Item -ItemType Directory -Force -Path $root | Out-Null
$summary = @()
foreach ($width in $Widths) {
    $height = [int]($width * 9 / 16)
    $name = 'Visible_{0}x{1}_{2}' -f $width, $height, (Get-Date -Format 'yyyyMMdd_HHmmss_fff')
    $folder = Join-Path $root $name
    New-Item -ItemType Directory -Path $folder | Out-Null
    $log = Join-Path $folder 'Player.log'
    $arguments = @('-screen-fullscreen', '0', '-screen-width', "$width", '-screen-height', "$height", '-force-d3d11',
        '-moriSmoke', ('"{0}"' -f $folder), '-logFile', ('"{0}"' -f $log))
    Write-Output "Testing $width x $height in a visible game window. Results: $folder"
    $process = Start-Process -FilePath $game -ArgumentList $arguments -PassThru -WindowStyle Normal
    $deadline = [DateTime]::UtcNow.AddSeconds(120)
    while (!$process.WaitForExit(1000)) {
        if ([DateTime]::UtcNow -gt $deadline) {
            Stop-Process -Id $process.Id
            throw "Owned smoke-test process timed out. Inspect $log"
        }
    }
    $process.Refresh()
    $reportPath = Join-Path $folder 'runtime-smoke.json'
    if (!(Test-Path -LiteralPath $reportPath)) { throw "No test report generated. Verify this is the Development build with diagnostics. Log: $log" }
    $report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $entry = [pscustomobject]@{
        RequestedWidth = $width
        RequestedHeight = $height
        ActualWidth = $report.screenWidth
        ActualHeight = $report.screenHeight
        Passed = $report.passed
        ExitCode = $process.ExitCode
        Checks = $report.checks.Count
        Errors = $report.errors.Count
        Report = $reportPath
        Executable = $game
    }
    $summary += $entry
    $entry | ConvertTo-Json
    $summaryPath = Join-Path $root 'latest-windows-smoke-summary.json'
    $summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $summaryPath -Encoding UTF8
    if (!$report.passed -or $process.ExitCode -ne 0) { throw "Runtime test failed. Inspect $reportPath and $log" }
    if ($report.screenWidth -ne $width -or $report.screenHeight -ne $height) { throw "Requested resolution was not achieved; do not claim that size passed. Report: $reportPath" }
}
Write-Output 'Automated integration passed. Default ten-wave balance, human input feel, audio quality, long-run performance and other computers still require testing.'
