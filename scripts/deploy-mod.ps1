param(

    [string]$GameRoot = "C:\Program Files (x86)\Steam\steamapps\common\Nuclear Option",

    [string]$Configuration = "Release",

    [string]$PatchToolConfiguration = "DEV_SDK",

    [switch]$SkipPatchTool

)



$ErrorActionPreference = "Stop"

$repoRoot = Split-Path $PSScriptRoot -Parent

$noloaderRoot = Join-Path (Split-Path $repoRoot -Parent) "NOLoader_Engine"



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



function Copy-LidarModPayload {

    param(

        [string]$SourceDeploy,

        [string]$TargetGame

    )

    New-Item -ItemType Directory -Force -Path $TargetGame | Out-Null

    Copy-Item (Join-Path $SourceDeploy "NOLoader.LidarWireframeContour.dll") $TargetGame -Force

    Copy-Item (Join-Path $SourceDeploy "NOLidarWireframeContour.Core.dll") $TargetGame -Force

    Copy-Item (Join-Path $SourceDeploy "NOLoader.ModConfig.dll") $TargetGame -Force

    Copy-Item (Join-Path $SourceDeploy "mod.json") $TargetGame -Force

    Copy-Item (Join-Path $SourceDeploy "mod_config.ini") $TargetGame -Force



    $dataTarget = Join-Path $TargetGame "NOLidarWireframeContour_Data"

    New-Item -ItemType Directory -Force -Path $dataTarget | Out-Null

    $bundle = Join-Path $SourceDeploy "NOLidarWireframeContour_Data\lidar_shaders"

    if (-not (Test-Path $bundle)) {

        Write-Error "Deploy payload missing bundle: $bundle"

    }

    Copy-Item $bundle (Join-Path $dataTarget "lidar_shaders") -Force

    $hashSrc = Join-Path $SourceDeploy "NOLidarWireframeContour_Data\shader_source_hash.txt"
    if (Test-Path $hashSrc) {
        Copy-Item $hashSrc $dataTarget -Force
    }

    $readme = Join-Path $SourceDeploy "NOLidarWireframeContour_Data\BUILD_SHADER_BUNDLE.md"

    if (Test-Path $readme) {

        Copy-Item $readme $dataTarget -Force

    }

    $staleShaders = Join-Path $dataTarget "Shaders"

    if (Test-Path $staleShaders) {

        Remove-Item $staleShaders -Recurse -Force

    }

}



if (-not (Test-Path $noloaderRoot)) {

    Write-Error "NOLoader_Engine not found at $noloaderRoot"

}



if (Get-Process -Name "NuclearOption" -ErrorAction SilentlyContinue) {

    Write-Error "Close Nuclear Option before deploy (PatchTool needs Managed DLLs unlocked)."

}



& (Join-Path $repoRoot "scripts\build-shader-bundle.ps1")

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }



$proj = Join-Path $repoRoot "NOLoader.LidarWireframeContour\NOLoader.LidarWireframeContour.csproj"

$modConfigProj = Join-Path $noloaderRoot "DEV.SDK\shared\NOLoader.ModConfig\NOLoader.ModConfig.csproj"

$dll = Join-Path $repoRoot "NOLoader.LidarWireframeContour\bin\$Configuration\net48\NOLoader.LidarWireframeContour.dll"

$modConfigDll = Join-Path $noloaderRoot "DEV.SDK\shared\NOLoader.ModConfig\bin\Release\net48\NOLoader.ModConfig.dll"

$coreDll = Join-Path $repoRoot "NOLidarWireframeContour.Core\bin\$Configuration\net48\NOLidarWireframeContour.Core.dll"

$deployMods = Join-Path $repoRoot "deploy\NOLoader\mods\LidarWireframeContour"

$gameMods = Join-Path $GameRoot "NOLoader\mods\LidarWireframeContour"

$dataSrc = Join-Path $repoRoot "NOLidarWireframeContour_Data"

$bundleSrc = Join-Path $dataSrc "lidar_shaders"



dotnet build $modConfigProj -c Release --verbosity minimal

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }



dotnet build $proj -c $Configuration --verbosity minimal

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }



if (-not (Test-Path $dll)) {

    Write-Error "Build output missing: $dll"

}



New-Item -ItemType Directory -Force -Path $deployMods | Out-Null

Copy-Item $dll $deployMods -Force

Copy-Item $coreDll $deployMods -Force

Copy-Item $modConfigDll $deployMods -Force

Copy-Item (Join-Path $repoRoot "NOLoader.LidarWireframeContour\mod.json") $deployMods -Force

Copy-Item (Join-Path $repoRoot "NOLoader.LidarWireframeContour\mod_config.ini") $deployMods -Force



$dataDeploy = Join-Path $deployMods "NOLidarWireframeContour_Data"

New-Item -ItemType Directory -Force -Path $dataDeploy | Out-Null

Copy-Item $bundleSrc (Join-Path $dataDeploy "lidar_shaders") -Force

$hashSrc = Join-Path $dataSrc "shader_source_hash.txt"
if (Test-Path $hashSrc) {
    Copy-Item $hashSrc $dataDeploy -Force
}

$buildReadme = Join-Path $dataSrc "BUILD_SHADER_BUNDLE.md"

if (Test-Path $buildReadme) {

    Copy-Item $buildReadme $dataDeploy -Force

}



Copy-LidarModPayload -SourceDeploy $deployMods -TargetGame $gameMods



if (-not $SkipPatchTool) {

    Write-Host "Applying mod IL patches via PatchTool ($PatchToolConfiguration)..."

    dotnet run --project (Join-Path $noloaderRoot "src\NOLoader.PatchTool\NOLoader.PatchTool.csproj") -c $PatchToolConfiguration -- $GameRoot

    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Copy-LidarModPayload -SourceDeploy $deployMods -TargetGame $gameMods

}



Assert-DeployArtifact -Path (Join-Path $gameMods "NOLoader.LidarWireframeContour.dll") -MinBytes 4000 -Label "mod DLL"

Assert-DeployArtifact -Path (Join-Path $gameMods "NOLidarWireframeContour.Core.dll") -MinBytes 20000 -Label "Core DLL"

Assert-DeployArtifact -Path (Join-Path $gameMods "NOLoader.ModConfig.dll") -MinBytes 1000 -Label "ModConfig DLL"

Assert-DeployArtifact -Path (Join-Path $gameMods "NOLidarWireframeContour_Data\lidar_shaders") -MinBytes 15000 -Label "shader bundle"



Write-Host "Lidar Wireframe Contour deployed to $gameMods"

