param(
    [Parameter(Mandatory = $true)][string]$Executable,
    [int]$Width = 1280,
    [ValidateSet('Art','Arsenal','Encounter')][string]$Mode = 'Art'
)
$ErrorActionPreference = 'Stop'
$gamePath = (Resolve-Path -LiteralPath $Executable).Path
$project = Split-Path -Parent $PSScriptRoot
$height = [int]($Width * 9 / 16)
$prefix = if ($Mode -eq 'Encounter') { 'Encounter' } elseif ($Mode -eq 'Arsenal') { 'Arsenal' } else { 'ProductionArt' }
$switch = if ($Mode -eq 'Encounter') { '-moriEncounterSmoke' } elseif ($Mode -eq 'Arsenal') { '-moriArsenalSmoke' } else { '-moriArtSmoke' }
$folder = Join-Path $project ('TestResults\{0}_{1}x{2}_{3}' -f $prefix, $Width, $height, (Get-Date -Format 'yyyyMMdd_HHmmss_fff'))
New-Item -ItemType Directory -Path $folder | Out-Null
$arguments = @('-screen-fullscreen','0','-screen-width',"$Width",'-screen-height',"$height",'-force-d3d11',$switch,('"{0}"' -f $folder),'-logFile',('"{0}\Player.log"' -f $folder))
Write-Output "Art integration: $folder"
$process = Start-Process -FilePath $gamePath -ArgumentList $arguments -WindowStyle Normal -PassThru
if (!$process.WaitForExit(325000)) { Stop-Process -Id $process.Id; throw 'Owned art test exceeded its deadline.' }
$report = Get-Content -LiteralPath (Join-Path $folder 'stage-smoke.json') -Raw | ConvertFrom-Json
Add-Type -AssemblyName System.Drawing
foreach ($path in $report.screenshots) {
    $bitmap = [System.Drawing.Bitmap]::new($path)
    try {
        $colors = [System.Collections.Generic.HashSet[int]]::new()
        for ($row = 1; $row -lt 12; $row++) {
            for ($column = 1; $column -lt 20; $column++) {
                $pixel = $bitmap.GetPixel([int]($bitmap.Width * $column / 20), [int]($bitmap.Height * $row / 12))
                [void]$colors.Add($pixel.ToArgb())
            }
        }
        if ($colors.Count -lt 8) { throw "Blank screenshot; visual validation failed: $path" }
    } finally { $bitmap.Dispose() }
}
$summary = [ordered]@{ Executable=$gamePath; Width=$report.screenWidth; Height=$report.screenHeight; Passed=$report.passed; Checks=$report.checks.Count; Errors=$report.errors; ExitCode=$process.ExitCode; Report=(Join-Path $folder 'stage-smoke.json') }
$summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $folder 'process-evidence.json') -Encoding UTF8
$summary | ConvertTo-Json -Depth 5
if (!$report.passed -or $process.ExitCode -ne 0) { throw "Art integration failed: $folder" }
if ($report.screenWidth -ne $Width -or $report.screenHeight -ne $height) { throw 'Actual resolution differs from requested resolution.' }
