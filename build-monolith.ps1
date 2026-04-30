# Build and Deploy Monolith Script
# This script builds the frontend and copies it to the backend's wwwroot folder

param(
    [string]$Configuration = "Release"
)

Write-Host "=== Building Monolith (Frontend + Backend) ===" -ForegroundColor Green

# Step 1: Build Frontend
Write-Host "Step 1: Building Frontend..." -ForegroundColor Cyan
Push-Location .\Frontend
npm install
npm run build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Frontend build failed!" -ForegroundColor Red
    exit 1
}
Pop-Location
Write-Host "Frontend build successful!" -ForegroundColor Green

# Step 2: Copy Frontend build to wwwroot
Write-Host "Step 2: Copying frontend to backend wwwroot..." -ForegroundColor Cyan
$distPath = ".\Frontend\dist"
$wwwrootPath = ".\wwwroot"

# Remove existing wwwroot if it exists
if (Test-Path $wwwrootPath) {
    Remove-Item -Path $wwwrootPath -Recurse -Force
}

# Copy dist to wwwroot
Copy-Item -Path $distPath -Destination $wwwrootPath -Recurse
Write-Host "Frontend copied to wwwroot!" -ForegroundColor Green

# Step 3: Build Backend
Write-Host "Step 3: Building Backend..." -ForegroundColor Cyan
dotnet build --configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Host "Backend build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Backend build successful!" -ForegroundColor Green

# Step 4: Publish Backend
Write-Host "Step 4: Publishing Backend..." -ForegroundColor Cyan
dotnet publish --configuration $Configuration --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Backend publish failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Backend publish successful!" -ForegroundColor Green

Write-Host "=== Build Complete! ===" -ForegroundColor Green
Write-Host "Publish output is in ./bin/$Configuration/net10.0/publish/" -ForegroundColor Yellow
Write-Host "" -ForegroundColor Yellow
Write-Host "To deploy to Azure Web App:" -ForegroundColor Yellow
Write-Host "1. Use Azure App Service extension in VS Code, OR" -ForegroundColor Yellow
Write-Host "2. Run: az webapp deployment source config-zip --resource-group <resourceGroup> --name <appName> --src-path ./bin/$Configuration/net10.0/publish" -ForegroundColor Yellow
