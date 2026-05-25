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

# Target platforms
$rids = @("win-x64", "linux-x64", "osx-x64", "osx-arm64")

# Locate and clean dist directory
$distDir = Join-Path $PSScriptRoot "..\dist"
if (Test-Path $distDir) {
    Write-Host "Cleaning dist directory..."
    Remove-Item (Join-Path $distDir "*") -Recurse -Force
} else {
    $null = New-Item -ItemType Directory -Path $distDir
}

# Clean existing ZIP files for this version
$existingZips = Get-ChildItem (Join-Path $PSScriptRoot "..") -Filter "Ragnar_${version}_*.zip"
if ($existingZips) {
    Write-Host "Cleaning existing release ZIPs for version $version..."
    $existingZips | Remove-Item -Force
}

# Build and package each platform
foreach ($rid in $rids) {
    Write-Host "`n=== Building and Packaging for $rid ==="
    $outDir = Join-Path $distDir $rid
    
    # Run dotnet publish
    dotnet publish $csprojPath -c Release -r $rid -o $outDir -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:SelfContained=false --no-self-contained
    
    # Create ZIP package
    $zipName = "Ragnar_${version}_${rid}.zip"
    $zipPath = Join-Path $PSScriptRoot "..\$zipName"
    
    Write-Host "Creating zip package: $zipName..."
    Compress-Archive -Path (Join-Path $outDir "*") -DestinationPath $zipPath -Force
    Write-Host "Zip package for $rid created successfully."
}

# Check if gh CLI is installed and authenticated
$ghPath = Get-Command gh -ErrorAction SilentlyContinue
if ($ghPath) {
    Write-Host "`nGitHub CLI (gh) detected. Checking auth status..."
    $null = gh auth status 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Creating GitHub Release v$version and uploading zip packages..."
        
        # Gather all zip files for this version
        $zipFiles = Get-ChildItem (Join-Path $PSScriptRoot "..") -Filter "Ragnar_${version}_*.zip" | Select-Object -ExpandProperty FullName
        
        gh release create "v$version" $zipFiles --title "v$version" --generate-notes
        if ($LASTEXITCODE -eq 0) {
            Write-Host "GitHub Release v$version successfully created and assets uploaded."
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
