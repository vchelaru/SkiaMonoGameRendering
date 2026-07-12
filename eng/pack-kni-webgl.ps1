[CmdletBinding()]
param(
    [string]$Output = (Join-Path $PSScriptRoot "..\.artifacts\packages")
)

$ErrorActionPreference = "Stop"
& (Join-Path $PSScriptRoot "bootstrap-kni-webgl.ps1")

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.artifacts\kni"))
$outputPath = [System.IO.Path]::GetFullPath($Output)
$versions = [xml](Get-Content (Join-Path $PSScriptRoot "Versions.props") -Raw)
$version = [string]$versions.Project.PropertyGroup.KniPatchedPackageVersion
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

$projects = @(
    "src\Xna.Framework\Xna.Framework.csproj",
    "src\Xna.Framework.Content\Xna.Framework.Content.csproj",
    "src\Xna.Framework.Graphics\Xna.Framework.Graphics.csproj",
    "src\Xna.Framework.Audio\Xna.Framework.Audio.csproj",
    "src\Xna.Framework.Media\Xna.Framework.Media.csproj",
    "src\Xna.Framework.Input\Xna.Framework.Input.csproj",
    "src\Xna.Framework.Game\Xna.Framework.Game.csproj",
    "src\Xna.Framework.Devices\Xna.Framework.Devices.csproj",
    "src\Xna.Framework.Storage\Xna.Framework.Storage.csproj",
    "src\Xna.Framework.XR\Xna.Framework.XR.csproj",
    "Platforms\Kni.Platform.Blazor.GL.csproj"
)

foreach ($project in $projects) {
    dotnet pack (Join-Path $root $project) -c Release -o $outputPath -p:PackageVersion=$version
    if ($LASTEXITCODE -ne 0) { throw "Failed to pack KNI project '$project'." }
}

dotnet pack (Join-Path $PSScriptRoot "..\src\SkiaMonoGameRendering.Kni.WebGL\SkiaMonoGameRendering.Kni.WebGL.csproj") `
    -c Release -o $outputPath -p:UseKniPackages=true -p:RestoreSources="$outputPath;https://api.nuget.org/v3/index.json"
if ($LASTEXITCODE -ne 0) { throw "Failed to pack SkiaMonoGameRendering.Kni.WebGL." }

Write-Host "Packages written to $outputPath"
