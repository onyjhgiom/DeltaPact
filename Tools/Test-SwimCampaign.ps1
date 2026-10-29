param([Parameter(Mandatory = $true)][string]$Executable)
$ErrorActionPreference = 'Stop'
$gamePath = (Resolve-Path -LiteralPath $Executable).Path
$project = Split-Path -Parent $PSScriptRoot
$folder = Join-Path $project ('TestResults\Swim_' + (Get-Date -Format 'yyyyMMdd_HHmmss_fff'))
New-Item -ItemType Directory -Path $folder | Out-Null
$arguments = @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-force-d3d11','-moriSwimSmoke',('"{0}"' -f $folder),'-logFile',('"{0}\Player.log"' -f $folder))
Write-Output "Aquatic integration: $folder"
# This is the interactive game under visual test; a hidden/minimized Direct3D window can produce black screenshots.
$process = Start-Process -FilePath $gamePath -ArgumentList $arguments -WindowStyle Normal -PassThru
if (!$process.WaitForExit(260000)) { Stop-Process -Id $process.Id; throw 'Owned swim integration exceeded deadline.' }
$reportPath = Join-Path $folder 'swim-campaign-smoke.json'
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
Add-Type -AssemblyName System.Drawing
$screenshots = @(Get-ChildItem -LiteralPath $folder -Filter '*.png' -File)
if ($report.passed -and $screenshots.Count -lt 15) { throw 'Aquatic evidence screenshots missing.' }
foreach ($path in $screenshots) {
    $bitmap = [System.Drawing.Bitmap]::new($path.FullName)
    try { if ($bitmap.Width -lt 1280 -or $bitmap.Height -lt 720) { throw 'Screenshot size below requested resolution.' } }
    finally { $bitmap.Dispose() }
}
$summary = [ordered]@{Executable=$gamePath; Passed=$report.passed; Checks=$report.checks.Count; Errors=$report.errors; ExitCode=$process.ExitCode; Screenshots=$screenshots.Count; Report=$reportPath}
$summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $folder 'process-evidence.json') -Encoding UTF8
$summary | ConvertTo-Json -Depth 5
if (!$report.passed -or $process.ExitCode -ne 0) { throw "Aquatic integration failed: $folder" }
