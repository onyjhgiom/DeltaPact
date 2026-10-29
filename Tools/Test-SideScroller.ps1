param(
    [Parameter(Mandatory = $true)][string]$Executable,
    [int[]]$Widths = @(1280, 1920)
)
$ErrorActionPreference = 'Stop'
$game = (Resolve-Path -LiteralPath $Executable).Path
$project = Split-Path -Parent $PSScriptRoot
$summary = @()
foreach ($width in $Widths) {
    $height = [int]($width * 9 / 16)
    $folder = Join-Path $project ('TestResults\Side_{0}x{1}_{2}' -f $width, $height, (Get-Date -Format 'yyyyMMdd_HHmmss_fff'))
    New-Item -ItemType Directory -Path $folder | Out-Null
    $arguments = @('-screen-fullscreen','0','-screen-width',"$width",'-screen-height',"$height",'-force-d3d11','-moriStage1',('"{0}"' -f $folder),'-logFile',('"{0}\Player.log"' -f $folder))
    Write-Output "Visible stage integration: $folder"
    $process = Start-Process -FilePath $game -ArgumentList $arguments -WindowStyle Normal -PassThru
    if (!$process.WaitForExit(445000)) { Stop-Process -Id $process.Id; throw 'Owned test process exceeded its deadline.' }
    $report = Get-Content -LiteralPath (Join-Path $folder 'stage-smoke.json') -Raw | ConvertFrom-Json
    $entry = [pscustomobject]@{Executable=$game; Width=$report.screenWidth; Height=$report.screenHeight; Passed=$report.passed; Checks=$report.checks.Count; Errors=$report.errors.Count; ExitCode=$process.ExitCode; Report=(Join-Path $folder 'stage-smoke.json')}
    $summary += $entry
    $entry | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $folder 'process-evidence.json') -Encoding UTF8
    $summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $project 'TestResults\latest-side-summary.json') -Encoding UTF8
    $entry | ConvertTo-Json
    if (!$report.passed -or $process.ExitCode -ne 0) { throw "Side integration failed: $folder" }
    if ($report.screenWidth -ne $width -or $report.screenHeight -ne $height) { throw 'Actual resolution differs from requested resolution.' }
}
