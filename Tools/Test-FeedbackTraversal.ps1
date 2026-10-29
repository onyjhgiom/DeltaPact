param([Parameter(Mandatory = $true)][string]$Executable, [switch]$ClimbOnly)
$ErrorActionPreference = 'Stop'
$gamePath = (Resolve-Path -LiteralPath $Executable).Path
$project = Split-Path -Parent $PSScriptRoot
$folder = Join-Path $project ('TestResults\FeedbackTraversal_' + (Get-Date -Format 'yyyyMMdd_HHmmss_fff'))
New-Item -ItemType Directory -Path $folder | Out-Null
$arguments = @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-force-d3d11','-moriFeedbackSmoke',('"{0}"' -f $folder),'-logFile',('"{0}\Player.log"' -f $folder))
if ($ClimbOnly) { $arguments += '-moriClimbOnly' }
$process = Start-Process -FilePath $gamePath -ArgumentList $arguments -WindowStyle Normal -PassThru
Write-Output "Feedback and physical traversal test PID=$($process.Id): $folder"
if (!$process.WaitForExit(555000)) { Stop-Process -Id $process.Id; throw 'Owned feedback/traversal test exceeded its deadline.' }
$reportPath = Join-Path $folder 'feedback-traversal.json'
$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
$screenshots = @(Get-ChildItem -LiteralPath $folder -Filter '*.png' -File)
Add-Type -AssemblyName System.Drawing
foreach ($shot in $screenshots) {
    $bitmap = [System.Drawing.Bitmap]::new($shot.FullName)
    try {
        $colors = [System.Collections.Generic.HashSet[int]]::new()
        for ($row = 1; $row -lt 12; $row++) {
            for ($column = 1; $column -lt 20; $column++) {
                [void]$colors.Add($bitmap.GetPixel([int]($bitmap.Width * $column / 20), [int]($bitmap.Height * $row / 12)).ToArgb())
            }
        }
        if ($colors.Count -lt 8) { throw "Blank screenshot: $($shot.FullName)" }
    } finally { $bitmap.Dispose() }
}
$summary = [ordered]@{Executable=$gamePath; ClimbOnly=[bool]$ClimbOnly; Passed=$report.passed; Checks=$report.checks.Count; Errors=$report.errors; ExitCode=$process.ExitCode; Screenshots=$screenshots.Count; Report=$reportPath}
$summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $folder 'process-evidence.json') -Encoding UTF8
$summary | ConvertTo-Json -Depth 5
if (!$report.passed -or $process.ExitCode -ne 0) { throw "Feedback/traversal integration failed: $folder" }
