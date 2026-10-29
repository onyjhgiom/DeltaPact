param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$RoslynDirectory = '',
    [string]$NetstandardPath = ''
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$powerShellScripts = @(Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Tools') -Filter '*.ps1' -Recurse -File)
foreach ($script in $powerShellScripts) {
    $tokens = $null
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($script.FullName, [ref]$tokens, [ref]$parseErrors) | Out-Null
    if ($parseErrors.Count -gt 0) { throw ($parseErrors | Out-String) }
}
$installRoot = ''
if ([string]::IsNullOrWhiteSpace($RoslynDirectory)) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vswhere) {
        $installRoot = (& $vswhere -latest -products '*' -property installationPath | Select-Object -First 1)
    }
    if ([string]::IsNullOrWhiteSpace($installRoot)) { throw 'Visual Studio Roslyn not found. Supply -RoslynDirectory and -NetstandardPath.' }
    $RoslynDirectory = Join-Path $installRoot 'MSBuild\Current\Bin\Roslyn'
}
$compiler = Join-Path $RoslynDirectory 'csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw "Compiler missing: $compiler" }
if ([string]::IsNullOrWhiteSpace($NetstandardPath)) {
    if ([string]::IsNullOrWhiteSpace($installRoot)) {
        $installRoot = (Get-Item -LiteralPath $RoslynDirectory).Parent.Parent.Parent.Parent.FullName
    }
    $NetstandardPath = Join-Path $installRoot 'Common7\IDE\Remote Debugger\x64\netstandard.dll'
}
if (!(Test-Path -LiteralPath $NetstandardPath)) { throw 'Supply -NetstandardPath for your installed Roslyn toolchain.' }
$output = Join-Path $ProjectRoot 'Validation'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$tests = Join-Path $output 'RunModelTests.exe'
$syntax = Join-Path $output 'SourceValidator.exe'
$model = Join-Path $ProjectRoot 'Assets\Scripts\Core\RunModel.cs'
$testSource = Join-Path $ProjectRoot 'Tools\Tests\RunModelTests.cs'
$validatorSource = Join-Path $ProjectRoot 'Tools\SourceValidator.cs'
$compileOutput = & $compiler /nologo /langversion:9 /target:exe "/out:$tests" $model $testSource 2>&1
if ($LASTEXITCODE -ne 0) { throw ($compileOutput | Out-String) }
$testOutput = & $tests 2>&1
if ($LASTEXITCODE -ne 0) { throw ($testOutput | Out-String) }
$references = @('Microsoft.CodeAnalysis.dll', 'Microsoft.CodeAnalysis.CSharp.dll', 'System.Collections.Immutable.dll', 'System.Memory.dll')
$compilerArguments = @('/nologo', '/langversion:9', '/target:exe', "/out:$syntax", "/r:$NetstandardPath")
foreach ($reference in $references) { $compilerArguments += "/r:$(Join-Path $RoslynDirectory $reference)" }
$compilerArguments += $validatorSource
$compileOutput = & $compiler @compilerArguments 2>&1
if ($LASTEXITCODE -ne 0) { throw ($compileOutput | Out-String) }
$dependencies = $references + @('System.Reflection.Metadata.dll', 'System.Runtime.CompilerServices.Unsafe.dll', 'System.Buffers.dll', 'System.Numerics.Vectors.dll')
foreach ($dependency in $dependencies) { Copy-Item -LiteralPath (Join-Path $RoslynDirectory $dependency) -Destination $output -Force }
Copy-Item -LiteralPath (Join-Path $RoslynDirectory 'csc.exe.config') -Destination "$syntax.config" -Force
$syntaxOutput = & $syntax $ProjectRoot 2>&1
if ($LASTEXITCODE -ne 0) { throw ($syntaxOutput | Out-String) }
$importSafetyOutput = & (Join-Path $ProjectRoot 'Tools\Tests\ImportSafety.Tests.ps1') -SourceRoot $ProjectRoot
$sourceHashes = Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Assets') -Filter '*.cs' -Recurse -File | Sort-Object FullName | ForEach-Object {
    [ordered]@{ path = $_.FullName.Substring($ProjectRoot.Length + 1).Replace('\', '/'); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
}
$report = [ordered]@{
    generatedAt = (Get-Date).ToString('o')
    projectRoot = $ProjectRoot
    roslyn = $RoslynDirectory
    pureModelTest = ($testOutput | Out-String).Trim()
    syntaxCheck = ($syntaxOutput | Out-String).Trim()
    powerShellParse = "$($powerShellScripts.Count) PowerShell scripts; 0 parse errors"
    importSafety = ($importSafetyOutput | Out-String).Trim()
    unityCompile = 'NOT RUN by this portable tool; see actual Unity project Logs and docs/UNITY_INTEGRATION_REPORT.md'
    playMode = 'NOT RUN'
    windowsGameBuild = 'NOT RUN - test executable is NOT a game build'
    sourceHashes = @($sourceHashes)
}
$reportPath = Join-Path $output 'source-validation.json'
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
$testOutput
$syntaxOutput
$importSafetyOutput
"Report: $reportPath"
