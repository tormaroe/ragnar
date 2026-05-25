$ErrorActionPreference = "Stop"

# Extract version from Ragnar.csproj
$csprojPath = Join-Path $PSScriptRoot "..\src\Ragnar.csproj"
if (-not (Test-Path $csprojPath)) {
    Write-Error "Could not find src/Ragnar.csproj at expected path: $csprojPath"
    exit 1
}

[xml]$proj = Get-Content $csprojPath
$version = $proj.Project.PropertyGroup.Version
if (-not $version) {
    Write-Error "Could not find <Version> element in $csprojPath"
    exit 1
}

Write-Host "Detected version: $version"

# Locate dist directory
$distDir = Join-Path $PSScriptRoot "..\dist"
if (-not (Test-Path $distDir)) {
    Write-Error "Could not find dist directory at: $distDir. Please run 'just deploy' first."
    exit 1
}

# Zip path
$zipName = "Ragnar_$version.zip"
$zipPath = Join-Path $PSScriptRoot "..\$zipName"

# Remove existing zip
if (Test-Path $zipPath) {
    Write-Host "Removing existing zip: $zipName"
    Remove-Item $zipPath -Force
}

# Package dist output to zip
Write-Host "Creating zip package: $zipName..."
Compress-Archive -Path "$distDir\*" -DestinationPath $zipPath -Force
Write-Host "Zip package created successfully."

# Check if gh CLI is installed and authenticated
$ghPath = Get-Command gh -ErrorAction SilentlyContinue
if ($ghPath) {
    Write-Host "GitHub CLI (gh) detected. Checking auth status..."
    $null = gh auth status 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Creating GitHub Release v$version and uploading $zipName..."
        gh release create "v$version" $zipPath --title "v$version" --generate-notes
        if ($LASTEXITCODE -eq 0) {
            Write-Host "GitHub Release v$version successfully created and asset uploaded."
        } else {
            Write-Error "Failed to create GitHub Release."
            exit 1
        }
    } else {
        Write-Warning "GitHub CLI is not authenticated. Skipping release creation on GitHub."
        Write-Warning "Run 'gh auth login' to authenticate if you want to publish releases automatically."
    }
} else {
    Write-Warning "GitHub CLI (gh) was not found in PATH. Skipping release creation on GitHub."
}
