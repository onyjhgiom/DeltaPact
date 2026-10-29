param([string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $ProjectRoot).Path
$backup = Join-Path $root 'Validation\SideScrollerBackup_20261024_115759'
$baseline = @{}
Import-Csv (Join-Path $backup 'baseline.csv') | ForEach-Object { $baseline[$_.Path] = $_.SHA256 }
$files = @()
foreach ($directory in @('Assets','Packages','ProjectSettings','docs','Tools')) {
    $files += Get-ChildItem -LiteralPath (Join-Path $root $directory) -File -Recurse
}
foreach ($name in @('README.md','HOW_TO_PLAY.md','PLAY_MoriEchoes.cmd')) {
    $files += Get-Item -LiteralPath (Join-Path $root $name)
}
$changes = @()
foreach ($file in $files) {
    $relative = $file.FullName.Substring($root.Length + 1)
    if ($relative -in @('docs\SIDE_SCROLLER_FILE_CHANGES.csv','docs\SIDE_SCROLLER_FILE_CHANGES.md')) { continue }
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    if (!$baseline.ContainsKey($relative) -or $baseline[$relative] -ne $hash) {
        $status = if ($baseline.ContainsKey($relative)) {'Modified'} else {'Added'}
        if ($relative -eq 'docs\CONTRA_SIDE_SCROLLER_REDESIGN.md') { $status = 'ExternalChangePreserved' }
        $changes += [pscustomobject]@{Status=$status; Path=$relative; SHA256=$hash; Bytes=$file.Length}
    }
}
foreach ($relative in $baseline.Keys) {
    if (!(Test-Path -LiteralPath (Join-Path $root $relative))) { $changes += [pscustomobject]@{Status='Missing'; Path=$relative; SHA256=''; Bytes=0} }
}
$changes = @($changes | Sort-Object Path)
$changes | Export-Csv -LiteralPath (Join-Path $root 'docs\SIDE_SCROLLER_FILE_CHANGES.csv') -NoTypeInformation -Encoding UTF8
$content = @('# 横版改造文件变更清单', '', ('生成时间：' + (Get-Date).ToString('o')), '', ('对照备份：`' + $backup + '`'), '', '| 状态 | 文件 |', '|---|---|')
foreach ($change in $changes) { $content += '| ' + $change.Status + ' | `' + $change.Path + '` |' }
$content += @('', 'CSV 同时记录 SHA256 和字节数；本清单文件自身不计入散列。构建与运行截图保存在 Builds、TestResults，未删除任何旧构建。')
$content += 'ExternalChangePreserved：参考设计文档在本轮开发期间出现额外修改，已保留，未将其回滚或当作本轮代码修改。'
$content | Set-Content -LiteralPath (Join-Path $root 'docs\SIDE_SCROLLER_FILE_CHANGES.md') -Encoding UTF8
Write-Output ('Changes: ' + $changes.Count + '; Missing: ' + @($changes | Where-Object Status -EQ 'Missing').Count)
