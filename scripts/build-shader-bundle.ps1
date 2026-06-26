# Builds lidar_shaders via Unity 2022.3.
$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path $PSScriptRoot -Parent
$ProjectPath = Join-Path $RepoRoot "UnityBundleBuilder"
$ShaderSrc = Join-Path $RepoRoot "NOLidarWireframeContour_Data\Shaders"
$ShaderDst = Join-Path $ProjectPath "Assets\LidarWireframe"

if (Test-Path $ShaderSrc) {
    New-Item -ItemType Directory -Force -Path $ShaderDst | Out-Null
    Copy-Item -Force (Join-Path $ShaderSrc "*.shader") $ShaderDst
    Write-Host "Synced shaders: $ShaderSrc -> $ShaderDst"
} else {
    Write-Error "Shader source folder missing: $ShaderSrc"
}

$UnityExe = "C:\Program Files\Unity\Hub\Editor\2022.3.6f1\Editor\Unity.exe"
if (-not (Test-Path $UnityExe)) {
    $UnityExe = "C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe"
}
if (-not (Test-Path $UnityExe)) {
    Write-Error "Unity 2022.3 not found. Install Unity Hub 2022.3.x LTS."
}
$LogFile = Join-Path $ProjectPath "build-bundle.log"
$LogTail = Join-Path $ProjectPath "build-bundle-tail.log"
Write-Host "Building lidar shader bundle with: $UnityExe"
$unityArgs = @(
    "-batchmode",
    "-nographics",
    "-projectPath", $ProjectPath,
    "-executeMethod", "BuildLidarBundle.BuildBatch",
    "-quit",
    "-logFile", $LogFile
)
& $UnityExe @unityArgs
$exitCode = $LASTEXITCODE
Write-Host "Unity exit code: $exitCode"

$Bundle = Join-Path $RepoRoot "NOLidarWireframeContour_Data\lidar_shaders"
$BundleBuilt = Join-Path $ProjectPath "BuiltBundles\lidar_shaders"

for ($i = 0; $i -lt 30 -and -not (Test-Path $BundleBuilt); $i++) {
    Start-Sleep -Milliseconds 200
}

if (Test-Path $BundleBuilt) {
    $destDir = Split-Path $Bundle -Parent
    New-Item -ItemType Directory -Force -Path $destDir | Out-Null
    Copy-Item -Force $BundleBuilt $Bundle
}

if (Test-Path $Bundle) {
    $compositeShader = Join-Path $ShaderSrc "LidarWireframeContour.shader"
    if (Test-Path $compositeShader) {
        $hashBytes = [System.Security.Cryptography.SHA256]::Create().ComputeHash([System.IO.File]::ReadAllBytes($compositeShader))
        $hashHex = [BitConverter]::ToString($hashBytes).Replace("-", "").Substring(0, 8).ToLowerInvariant()
        $hashFile = Join-Path $RepoRoot "NOLidarWireframeContour_Data\shader_source_hash.txt"
        Set-Content -Path $hashFile -Value $hashHex -NoNewline -Encoding ASCII
        Write-Host "Shader source hash: $hashHex"
    }
    if (Test-Path $LogFile) {
        $shaderErrors = Select-String -Path $LogFile -Pattern "Shader error in 'Hidden/ACT/Lidar'"
        if ($shaderErrors) {
            $shaderErrors | Select-Object -Last 3
            Write-Error "Shader compile failed - see $LogFile"
        }
    }
    Write-Host "OK: $Bundle ($((Get-Item $Bundle).Length) bytes)"
    if (Test-Path $LogFile) {
        Select-String -Path $LogFile -Pattern "\[LidarWireframe\] OK:" | Select-Object -Last 1
    }
    exit 0
}
if (Test-Path $LogFile) {
    Get-Content $LogFile -Tail 80 | Tee-Object -FilePath $LogTail
}
Write-Error "Bundle not found after build (exit=$exitCode): $Bundle (see $LogFile)"
