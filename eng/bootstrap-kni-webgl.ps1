[CmdletBinding()]
param(
    [string]$Destination = (Join-Path $PSScriptRoot "..\.artifacts\kni")
)

$ErrorActionPreference = "Stop"
$versions = [xml](Get-Content (Join-Path $PSScriptRoot "Versions.props") -Raw)
$commit = [string]$versions.Project.PropertyGroup.KniCommit
$patch = Join-Path $PSScriptRoot "patches\kni-webgl-canvas-upload.patch"
$destinationPath = [System.IO.Path]::GetFullPath($Destination)

if (-not (Test-Path (Join-Path $destinationPath ".git"))) {
    New-Item -ItemType Directory -Force -Path (Split-Path $destinationPath) | Out-Null
    git clone --filter=blob:none --no-checkout https://github.com/kniEngine/kni.git $destinationPath
    if ($LASTEXITCODE -ne 0) { throw "Failed to clone KNI." }
}

$actualRemote = git -C $destinationPath remote get-url origin
if ($LASTEXITCODE -ne 0) { throw "Failed to inspect the KNI remote." }
if ($actualRemote -notmatch "kniEngine/kni") {
    throw "Destination is not a KNI checkout: $destinationPath"
}

git -C $destinationPath fetch --depth 1 origin $commit
if ($LASTEXITCODE -ne 0) { throw "Failed to fetch pinned KNI commit $commit." }
git -C $destinationPath checkout --detach $commit
if ($LASTEXITCODE -ne 0) { throw "Failed to check out pinned KNI commit $commit." }
git -C $destinationPath submodule update --init --depth 1 ThirdParty/StbImageSharp ThirdParty/StbImageWriteSharp
if ($LASTEXITCODE -ne 0) { throw "Failed to initialize KNI submodules." }

git -C $destinationPath apply --check $patch 2>$null
if ($LASTEXITCODE -eq 0) {
    git -C $destinationPath apply $patch
    if ($LASTEXITCODE -ne 0) { throw "Failed to apply the KNI WebGL patch." }
} else {
    git -C $destinationPath apply --reverse --check $patch
    if ($LASTEXITCODE -ne 0) {
        throw "KNI checkout is dirty or the WebGL patch no longer applies cleanly."
    }
}

Write-Host "Patched KNI ready at $destinationPath"
