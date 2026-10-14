param([switch]$WindowSmoke)
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$results = Join-Path $PSScriptRoot 'artifacts\tests'
New-Item -ItemType Directory -Path $results -Force | Out-Null
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' -File | ForEach-Object { $_.FullName })
foreach ($test in Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'tests') -Filter '*.cs' -File | Sort-Object Name) {
    $executable = Join-Path $results ($test.BaseName + '.exe')
    $arguments = @('/nologo','/target:exe','/optimize+','/platform:x64',('/main:' + $test.BaseName),('/out:' + $executable),'/reference:System.dll','/reference:System.Drawing.dll','/reference:System.Windows.Forms.dll','/reference:System.Xml.dll') + $sources + @($test.FullName)
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw ('Test compile failed: ' + $test.BaseName) }
    $folder = Join-Path $results $test.BaseName
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    $testArguments = @()
    if ($test.BaseName -in @('BadgeTests','BehaviorTests','FeedbackTests','FullscreenTests')) { $testArguments = @($folder) }
    if ($test.BaseName -eq 'ExpressionTests') { $testArguments = @((Join-Path $PSScriptRoot 'assets\head.png'),(Join-Path $PSScriptRoot 'assets\focused.png'),(Join-Path $folder 'composed-focus.png')) }
    & $executable @testArguments 2>&1 | Tee-Object -FilePath (Join-Path $folder 'results.txt')
    if ($LASTEXITCODE -ne 0) { throw ('Test failed: ' + $test.BaseName) }
}
if ($WindowSmoke) {
    $executable = Join-Path $PSScriptRoot 'dist\DeniaPet.exe'
    foreach ($mode in @('--temporary-smoke-test','--temporary-live-test','--settings-smoke-test','--fullscreen-smoke-test','--badge-smoke-test','--interaction-smoke-test','--input-smoke-test','--performance-smoke-test')) {
        $process = Start-Process -FilePath $executable -ArgumentList $mode -WindowStyle Hidden -Wait -PassThru
        if ($process.ExitCode -ne 0) { throw ('Window check failed: ' + $mode) }
        Write-Output ('PASS ' + $mode)
    }
    foreach ($file in @('smoke-result.json','performance-smoke-result.json','temporary-live-test.json')) {
        $result = Get-Content -LiteralPath (Join-Path (Split-Path $executable) $file) -Raw | ConvertFrom-Json
        if ($result.ok -ne $true) { throw ('Window result was not OK: ' + $file) }
    }
}
Write-Output 'All requested tests passed.'
