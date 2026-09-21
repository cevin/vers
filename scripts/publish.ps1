param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('win-x64', 'win-arm64', 'osx-x64', 'osx-arm64', 'linux-x64', 'linux-arm64')]
    [string] $RuntimeIdentifier
)

$ErrorActionPreference = 'Stop'
$output = Join-Path $PSScriptRoot "../artifacts/$RuntimeIdentifier"
if (Test-Path $output) {
    Remove-Item -Recurse -Force $output
}

dotnet publish (Join-Path $PSScriptRoot '../src/Vers.Gui/Vers.Gui.csproj') `
    --configuration Release `
    --runtime $RuntimeIdentifier `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishDir=$output

Get-ChildItem -Path $output -Filter '*.pdb' -File -Recurse | Remove-Item -Force
