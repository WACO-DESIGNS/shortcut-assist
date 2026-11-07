$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' -File |
    ForEach-Object { $_.FullName })
$output = Join-Path $PSScriptRoot 'Shortcut Assist.exe'
$icon = Join-Path $PSScriptRoot 'assets/app.ico'
$mark = Join-Path $PSScriptRoot 'assets/app-mark.png'
$options = @(
    '/nologo', '/target:winexe', '/platform:x64', '/optimize+', '/codepage:65001', '/utf8output',
    "/out:$output", "/win32icon:$icon",
    "/resource:$icon,FusionAssist.AppIcon.ico", "/resource:$mark,FusionAssist.BrandMark.png",
    '/reference:System.Windows.Forms.dll', '/reference:System.Drawing.dll',
    '/reference:System.Web.Extensions.dll', '/reference:System.Core.dll'
)

& $compiler @options @sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Write-Output "Built $output"
