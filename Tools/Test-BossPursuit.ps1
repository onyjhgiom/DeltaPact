param([Parameter(Mandatory=$true)][string]$Executable)
$ErrorActionPreference='Stop'
$gamePath=(Resolve-Path -LiteralPath $Executable).Path
$project=Split-Path -Parent $PSScriptRoot
$folder=Join-Path $project ('TestResults/BossPursuit_'+(Get-Date -Format 'yyyyMMdd_HHmmss_fff'))
New-Item -ItemType Directory -Path $folder | Out-Null
$arguments=@('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','-force-d3d11','-moriBossPursuitSmoke',('"{0}"' -f $folder),'-logFile',('"{0}/Player.log"' -f $folder))
Write-Output "Boss pursuit runtime: $folder"
$process=Start-Process -FilePath $gamePath -ArgumentList $arguments -WindowStyle Normal -PassThru
if(!$process.WaitForExit(320000)){Stop-Process -Id $process.Id;throw 'Owned boss pursuit test exceeded its deadline.'}
$reportPath=Join-Path $folder 'boss-pursuit-smoke.json'
$report=Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
$summary=[ordered]@{Executable=$gamePath;Passed=$report.passed;Checks=$report.checks.Count;Errors=$report.errors;ExitCode=$process.ExitCode;Screenshots=$report.screenshots.Count;Report=$reportPath}
$summary|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $folder 'process-evidence.json') -Encoding UTF8
$summary|ConvertTo-Json -Depth 5
if(!$report.passed -or $process.ExitCode -ne 0){throw "Boss pursuit failed: $folder"}
