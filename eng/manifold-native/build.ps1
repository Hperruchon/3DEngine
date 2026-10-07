# Builds the native Manifold C API (manifoldc) on Windows from a Manifold source folder, and
# stages the libraries. The workflow build-manifold-native.yml and a local build call this one
# script, so the recipe of the pipeline and the recipe of a computer cannot differ (TASK-0049).
#
# Usage: eng/manifold-native/build.ps1 -Source <Manifold source folder> -Staging <staging folder>
#
# MANIFOLD_CROSS_SECTION must stay ON: MANIFOLD_CBIND is a cmake_dependent_option forced OFF when
# cross-section is OFF (v3.5.2), which would silently drop manifoldc. MANIFOLD_PAR=OFF is the
# serial (no-TBB) backend per ADR-0014 section 3. -A x64 selects the platform of the Visual Studio
# generator. Windows finds a dependency in the folder of the library, so no search path is set.
#
# The script runs in Windows PowerShell 5.1 and in PowerShell 7.
param(
    [Parameter(Mandatory = $true)][string]$Source,
    [Parameter(Mandatory = $true)][string]$Staging
)

$ErrorActionPreference = 'Stop'

# CMake on the path, as on a runner, or the copy inside Visual Studio, as on this computer.
$cmake = $null
$onPath = Get-Command cmake -ErrorAction SilentlyContinue
if ($onPath) {
    $cmake = $onPath.Source
}
else {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path $vswhere) {
        $found = & $vswhere -latest -prerelease -products * `
            -find 'Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe'
        if ($found) { $cmake = @($found)[0] }
    }
}
if (-not $cmake) { throw 'CMake was not found on the path or in Visual Studio.' }
Write-Host "cmake: $cmake"

$sourceDir = (Resolve-Path $Source).Path
$build = Join-Path $sourceDir 'build'
New-Item -ItemType Directory -Force -Path $Staging | Out-Null
$stagingDir = (Resolve-Path $Staging).Path

function Invoke-Checked {
    param([string]$Program, [string[]]$Arguments)
    & $Program @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Program failed with exit code $LASTEXITCODE" }
}

Write-Host '--- configure (Windows) ---'
Invoke-Checked $cmake @('-S', $sourceDir, '-B', $build, '-A', 'x64',
    '-DBUILD_SHARED_LIBS=ON',
    '-DMANIFOLD_CROSS_SECTION=ON',
    '-DMANIFOLD_CBIND=ON',
    '-DMANIFOLD_PAR=OFF',
    '-DMANIFOLD_TEST=OFF',
    '-DMANIFOLD_DOWNLOADS=ON')

Write-Host '--- build ---'
Invoke-Checked $cmake @('--build', $build, '--config', 'Release', '--parallel')

Write-Host '--- stage ---'
# A shared build emits manifoldc + core manifold (+ Clipper2 from cross-section): ship all.
$libraries = Get-ChildItem -Path $build -Recurse -File |
    Where-Object { $_.Name -ieq 'manifoldc.dll' -or $_.Name -ieq 'manifold.dll' -or $_.Name -like 'clipper2*.dll' }
foreach ($library in $libraries) {
    Copy-Item -Path $library.FullName -Destination $stagingDir -Force
    Write-Host "staged $($library.Name)"
}
if (-not (Test-Path (Join-Path $stagingDir 'manifoldc.dll'))) {
    Write-Host '::error title=Native build (Windows)::manifoldc was not built'
    exit 1
}
