param(
    [string]$GameRoot = "C:\Program Files (x86)\Steam\steamapps\common\Nuclear Option",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent

function Assert-DeployArtifact {
    param(
        [string]$Path,
        [long]$MinBytes,
        [string]$Label
    )
    if (-not (Test-Path $Path)) {
        Write-Error "Deploy verify failed: missing $Label at $Path"
    }
    $len = (Get-Item $Path).Length
    if ($len -lt $MinBytes) {
        Write-Error "Deploy verify failed: $Label too small ($len bytes, need >= $MinBytes) at $Path"
    }
    Write-Host "OK $Label : $Path ($len bytes)"
}

if (Get-Process -Name "NuclearOption" -ErrorAction SilentlyContinue) {
    Write-Error "Close Nuclear Option before deploy."
}

& (Join-Path $repoRoot "scripts\build-shader-bundle.ps1")
if ($LASTEXITCODE -ne 0) {
    Write-Warning "Shader bundle build failed or skipped; will try existing bundle on disk."
}

$solution = Join-Path $repoRoot "NOLidarWireframeContour.sln"
dotnet build $solution -c $Configuration --verbosity minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$pluginDll = Join-Path $repoRoot "BepInEx.LidarWireframeContour\bin\$Configuration\net48\BepInEx.LidarWireframeContour.dll"
$coreDll = Join-Path $repoRoot "NOLidarWireframeContour.Core\bin\$Configuration\net48\NOLidarWireframeContour.Core.dll"
$dataSrc = Join-Path $repoRoot "NOLidarWireframeContour_Data"
$bundleSrc = Join-Path $dataSrc "lidar_shaders"
$noloaderBundle = Join-Path $GameRoot "NOLoader\mods\LidarWireframeContour\NOLidarWireframeContour_Data\lidar_shaders"

if (-not (Test-Path $pluginDll)) {
    Write-Error "Build output missing: $pluginDll"
}
if (-not (Test-Path $coreDll)) {
    Write-Error "Build output missing: $coreDll"
}
if (-not (Test-Path $bundleSrc) -and (Test-Path $noloaderBundle)) {
    Write-Host "Using NOLoader mod bundle: $noloaderBundle"
    $bundleSrc = $noloaderBundle
}
if (-not (Test-Path $bundleSrc)) {
    Write-Error "Shader bundle missing: $bundleSrc - run build-shader-bundle.ps1 or deploy NOLoader mod first"
}

$pluginsDir = Join-Path $GameRoot "BepInEx\plugins"
$dataTarget = Join-Path $pluginsDir "NOLidarWireframeContour_Data"

New-Item -ItemType Directory -Force -Path $pluginsDir | Out-Null
New-Item -ItemType Directory -Force -Path $dataTarget | Out-Null

Copy-Item $pluginDll $pluginsDir -Force
Copy-Item $coreDll $pluginsDir -Force
Copy-Item $bundleSrc (Join-Path $dataTarget "lidar_shaders") -Force

$hashSrc = Join-Path $dataSrc "shader_source_hash.txt"
if (Test-Path $hashSrc) {
    Copy-Item $hashSrc $dataTarget -Force
}

$buildReadme = Join-Path $dataSrc "BUILD_SHADER_BUNDLE.md"
if (Test-Path $buildReadme) {
    Copy-Item $buildReadme $dataTarget -Force
}

Assert-DeployArtifact -Path (Join-Path $pluginsDir "BepInEx.LidarWireframeContour.dll") -MinBytes 8000 -Label "BepInEx plugin DLL"
Assert-DeployArtifact -Path (Join-Path $pluginsDir "NOLidarWireframeContour.Core.dll") -MinBytes 20000 -Label "Core DLL"
Assert-DeployArtifact -Path (Join-Path $dataTarget "lidar_shaders") -MinBytes 15000 -Label "shader bundle"

Write-Host "Lidar Wireframe Contour (BepInEx) deployed to $pluginsDir"
