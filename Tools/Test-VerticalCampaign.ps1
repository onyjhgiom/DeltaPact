param([Parameter(Mandatory = $true)][string]$Executable)
$ErrorActionPreference = 'Stop'
$gamePath = (Resolve-Path -LiteralPath $Executable).Path
$project = Split-Path -Parent $PSScriptRoot
$folder = Join-Path $project ('TestResults\Vertical_' + (Get-Date -Format 'yyyyMMdd_HHmmss_fff'))
New-Item -ItemType Directory -Path $folder | Out-Null
$arguments = @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-force-d3d11','-moriVerticalSmoke',('"{0}"' -f $folder),'-logFile',('"{0}\Player.log"' -f $folder))
Write-Output "Visible vertical integration: $folder"
$process = Start-Process -FilePath $gamePath -ArgumentList $arguments -WindowStyle Normal -PassThru
if (!$process.WaitForExit(160000)) { Stop-Process -Id $process.Id; throw 'Owned vertical test exceeded its deadline.' }
$reportPath = Join-Path $folder 'vertical-campaign-smoke.json'
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
Add-Type -AssemblyName System.Drawing
$screenshots = @(Get-ChildItem -LiteralPath $folder -Filter '*.png' -File)
if ($screenshots.Count -lt 12) { throw 'Missing shaft or boss-form screenshots.' }
foreach ($path in $screenshots) {
    $bitmap = [System.Drawing.Bitmap]::new($path.FullName)
    try {
        if ($bitmap.Width -ne 1280 -or $bitmap.Height -ne 720) { throw 'Unexpected screenshot resolution.' }
        $colors = [System.Collections.Generic.HashSet[int]]::new()
        for ($row = 1; $row -lt 12; $row++) {
            for ($column = 1; $column -lt 20; $column++) {
                [void]$colors.Add($bitmap.GetPixel([int]($bitmap.Width*$column/20),[int]($bitmap.Height*$row/12)).ToArgb())
            }
        }
        if ($colors.Count -lt 8) { throw "Blank screenshot: $($path.FullName)" }
    } finally { $bitmap.Dispose() }
}
$summary = [ordered]@{Executable=$gamePath; Passed=$report.passed; Checks=$report.checks.Count; Errors=$report.errors; ExitCode=$process.ExitCode; Screenshots=$screenshots.Count; Report=$reportPath}
$summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $folder 'process-evidence.json') -Encoding UTF8
$summary | ConvertTo-Json -Depth 5
if (!$report.passed -or $process.ExitCode -ne 0) { throw "Vertical integration failed: $folder" }
