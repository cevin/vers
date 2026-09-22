$ErrorActionPreference = 'Stop'

if (-not [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
        [System.Runtime.InteropServices.OSPlatform]::Windows)) {
    throw 'windows.ps1 must be run on Windows.'
}

$architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
$rid = switch ($architecture.ToString()) {
    'X64' { 'win-x64' }
    'Arm64' { 'win-arm64' }
    default { throw "Unsupported Windows architecture: $architecture" }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$output = Join-Path $repoRoot 'artifacts/windows'
if (Test-Path $output) {
    Remove-Item -Recurse -Force $output
}

dotnet publish (Join-Path $repoRoot 'src/Vers.Gui/Vers.Gui.csproj') `
    --configuration Release `
    --runtime $rid `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishDir="$output/"

Get-ChildItem -Path $output -Filter '*.pdb' -File -Recurse | Remove-Item -Force

$executable = Join-Path $output 'ver.exe'
if (-not (Test-Path $executable)) {
    throw "Publish completed without producing $executable"
}

Write-Host "Created $executable"
