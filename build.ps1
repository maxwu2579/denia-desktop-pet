param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist'))
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.x C# compiler was not found.' }
$destination = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' -File | Sort-Object Name | ForEach-Object { $_.FullName })
$arguments = @('/nologo','/target:winexe','/optimize+','/platform:x64',('/out:' + (Join-Path $destination 'DeniaPet.exe')),'/reference:System.dll','/reference:System.Drawing.dll','/reference:System.Windows.Forms.dll','/reference:System.Xml.dll') + $sources
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw "Build failed: $LASTEXITCODE" }
$assetDirectory = Join-Path $destination 'assets'
New-Item -ItemType Directory -Path $assetDirectory -Force | Out-Null
Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'assets') -File | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $assetDirectory -Force }
foreach ($file in @('README.md','ASSETS.md')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination $destination -Force }
Write-Output ('Built: ' + (Join-Path $destination 'DeniaPet.exe'))
