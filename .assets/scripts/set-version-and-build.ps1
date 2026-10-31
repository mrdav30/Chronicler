$ErrorActionPreference = "Stop"

# Import shared functions
Set-Location (Split-Path $MyInvocation.MyCommand.Path)
. .\utilities.ps1

# Locate solution directory and switch to it
$solutionDir = Get-SolutionDirectory
Set-Location $solutionDir

# Ensure GitVersion environment variables are set
Ensure-GitVersion-Environment

$archiveOutputDir = Join-Path $solutionDir "artifacts\releases"
New-Item -ItemType Directory -Path $archiveOutputDir -Force | Out-Null
Write-Host "Archives will be written to: $archiveOutputDir"

# Build and package for each release configuration
$configurations = @("Release", "ReleaseLean")
$libraryProjects = @("Chronicler", "Chronicler.Diagnostics", "Chronicler.MemoryPackShim")

foreach ($config in $configurations) {
    # Build the project with the current configuration
    Build-Project -Configuration $config

    $configLabel = $config.ToLowerInvariant()

    foreach ($projectName in $libraryProjects) {
        $projectPath = Join-Path $solutionDir "src\$projectName\$projectName.csproj"
        $project = [xml](Get-Content -LiteralPath $projectPath -Raw)
        # Use declared targets so old build directories are not included.
        $targetFrameworks = $project.SelectSingleNode("/Project/PropertyGroup/TargetFrameworks").InnerText.Split(';')
        $releaseDir = Join-Path $solutionDir "src\$projectName\bin\$config"

        foreach ($frameworkName in $targetFrameworks) {
            $targetDir = Join-Path $releaseDir $frameworkName
            if (-not (Test-Path -LiteralPath $targetDir -PathType Container)) {
                throw "Framework output not found for $projectName ($config): $targetDir"
            }

            $zipFileName = "${projectName}-v$($Env:GitVersion_FullSemVer)-${frameworkName}-${configLabel}.zip"
            $zipPath = Join-Path $archiveOutputDir $zipFileName

            Write-Host "Creating archive: $zipPath"
            Compress-Archive -Path "$targetDir\*" -DestinationPath $zipPath -Force
            Write-Host "Archive created for $projectName / $frameworkName ($config)"
        }
    }
}
