# NaturalApi Build and Package Script

param(
    [string]$Configuration = "Release",
    [string]$Version = "",
    [switch]$SkipTests = $false,
    [switch]$Pack = $false,
    [switch]$Push = $false,
    [string]$ApiKey = "",
    [string]$Source = "https://api.nuget.org/v3/index.json"
)

Write-Host "NaturalApi Build Script" -ForegroundColor Green
Write-Host "========================" -ForegroundColor Green

# Set version if provided
if ($Version) {
    Write-Host "Setting version to: $Version" -ForegroundColor Yellow
    dotnet build -p:VersionPrefix=$Version
}

# Clean and restore
Write-Host "Cleaning and restoring packages..." -ForegroundColor Yellow
dotnet clean
dotnet restore

# Build
Write-Host "Building in $Configuration mode..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Run tests (unless skipped)
if (-not $SkipTests) {
    Write-Host "Running tests..." -ForegroundColor Yellow
    dotnet test --configuration $Configuration --no-build --verbosity normal
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Tests failed!" -ForegroundColor Red
        exit 1
    }
}

# Pack (if requested)
if ($Pack) {
    Write-Host "Creating NuGet package..." -ForegroundColor Yellow
    dotnet pack --configuration $Configuration --no-build --output ./nupkgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Packaging failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Package created in ./nupkgs directory" -ForegroundColor Green
}

# Push (if requested)
if ($Push) {
    if (-not $ApiKey) {
        Write-Host "API key is required for pushing packages!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Pushing package to $Source..." -ForegroundColor Yellow
    $packageFile = Get-ChildItem -Path "./nupkgs" -Filter "*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    if ($packageFile) {
        dotnet nuget push $packageFile.FullName --api-key $ApiKey --source $Source
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Push failed!" -ForegroundColor Red
            exit 1
        }
        
        Write-Host "Package pushed successfully!" -ForegroundColor Green
    } else {
        Write-Host "No package found to push!" -ForegroundColor Red
        exit 1
    }
}

Write-Host "Build completed successfully!" -ForegroundColor Green
