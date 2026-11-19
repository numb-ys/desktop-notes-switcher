#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$Iscc = (Join-Path ([Environment]::GetFolderPath('ProgramFiles')) 'Inno Setup 7\ISCC.exe')
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true

function Read-UiStrings([string]$File) {
    $strings = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
    $document = [xml](Get-Content -LiteralPath (Join-Path $PSScriptRoot "..\Resources\$File") -Raw -Encoding utf8)
    foreach ($node in $document.root.data) {
        if ([string]::IsNullOrWhiteSpace($node.value)) {
            throw "Missing translation for '$($node.name)' in $File."
        }
        $strings.Add($node.name, $node.value)
    }
    return $strings
}

function Get-Placeholders([string]$Text) {
    [regex]::Matches($Text, '\{(\d+)(?:[,:][^{}]*)?\}') |
        ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
}

$english = Read-UiStrings 'Strings.resx'
$chinese = Read-UiStrings 'Strings.zh-CN.resx'
foreach ($key in $english.Keys) {
    if (-not $chinese.ContainsKey($key)) { throw "Missing Chinese translation for '$key'." }
    if (((Get-Placeholders $english[$key]) -join ',') -ne ((Get-Placeholders $chinese[$key]) -join ',')) {
        throw "English and Chinese placeholders differ for '$key'."
    }
}
foreach ($key in $chinese.Keys) {
    if (-not $english.ContainsKey($key)) { throw "Missing English translation for '$key'." }
}

$compiler = (Get-Command -Name $Iscc -CommandType Application).Source
$project = Join-Path $PSScriptRoot '..\DesktopNotesSwitcher.csproj'
$version = (dotnet msbuild $project -nologo -getProperty:Version).Trim()
$output = (New-Item -ItemType Directory -Path (Join-Path $PSScriptRoot '..\artifacts') -Force).FullName
$staging = Join-Path $output ("installer-payload-" + [Guid]::NewGuid().ToString('N'))
$payload = (New-Item -ItemType Directory -Path $staging).FullName

try {
    dotnet publish $project -c Release -r win-x64 --self-contained true -o $payload

    $deps = Get-Content -LiteralPath (Join-Path $payload 'DesktopNotesSwitcher.deps.json') -Raw | ConvertFrom-Json -AsHashtable
    $packageRoot = (dotnet nuget locals global-packages --list) -replace '^.*?:\s+', ''
    $runtime = ($deps.libraries.Keys | Where-Object { $_ -like 'runtimepack.Microsoft.NETCore.App.Runtime.win-x64/*' }) -replace '^runtimepack\.', ''
    $desktop = ($deps.libraries.Keys | Where-Object { $_ -like 'runtimepack.Microsoft.WindowsDesktop.App.Runtime.win-x64/*' }) -replace '^runtimepack\.', ''
    if (-not $runtime -or -not $desktop) { throw 'Cannot locate the bundled .NET runtime packages.' }

    $licenses = (New-Item -ItemType Directory -Path (Join-Path $payload 'licenses')).FullName
    $runtimePath = Join-Path $packageRoot $runtime.ToLowerInvariant()
    $desktopPath = Join-Path $packageRoot $desktop.ToLowerInvariant()
    Copy-Item -LiteralPath (Join-Path $runtimePath 'LICENSE.TXT') -Destination (Join-Path $licenses '.NET-Runtime-LICENSE.txt')
    Copy-Item -LiteralPath (Join-Path $runtimePath 'THIRD-PARTY-NOTICES.TXT') -Destination (Join-Path $licenses '.NET-Runtime-ThirdPartyNotices.txt')
    Copy-Item -LiteralPath (Join-Path $desktopPath 'LICENSE') -Destination (Join-Path $licenses 'WindowsDesktop-LICENSE.txt')

    & $compiler /Q "/DAppVersion=$version" "/DPayload=$payload" "/O$output" (Join-Path $PSScriptRoot 'DesktopNotesSwitcher.iss')
    Get-Item -LiteralPath (Join-Path $output "DesktopNotesSwitcher-v$version-Setup-win-x64.exe")
}
finally {
    $resolved = (Resolve-Path -LiteralPath $staging).Path
    if ($resolved -eq $staging -and $resolved.StartsWith($output + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        Remove-Item -LiteralPath $resolved -Recurse
    }
}
