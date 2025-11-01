# Build and Push Docker Images Script
# Make sure you're logged in to Docker Hub first: docker login

param(
    [Parameter(Mandatory=$true)]
    [string]$DockerHubUsername,
    
    [string]$ApiTag = "latest",
    [string]$BlazorTag = "latest"
)

Write-Host "Building and pushing Docker images for NoteBookmark..." -ForegroundColor Green

# Build API image
Write-Host "Building API image..." -ForegroundColor Yellow
docker build -f ../src/NoteBookmark.Api/Dockerfile -t "$DockerHubUsername/notebookmark-api:$ApiTag" ..

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to build API image"
    exit 1
}

# Build Blazor App image
Write-Host "Building Blazor App image..." -ForegroundColor Yellow
docker build -f ../src/NoteBookmark.BlazorApp/Dockerfile -t "$DockerHubUsername/notebookmark-blazor:$BlazorTag" ..

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to build Blazor App image"
    exit 1
}

# Push API image
Write-Host "Pushing API image to Docker Hub..." -ForegroundColor Yellow
docker push "$DockerHubUsername/notebookmark-api:$ApiTag"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to push API image"
    exit 1
}

# Push Blazor App image
Write-Host "Pushing Blazor App image to Docker Hub..." -ForegroundColor Yellow
docker push "$DockerHubUsername/notebookmark-blazor:$BlazorTag"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to push Blazor App image"
    exit 1
}

Write-Host "Successfully built and pushed both images!" -ForegroundColor Green
Write-Host "API image: $DockerHubUsername/notebookmark-api:$ApiTag" -ForegroundColor Cyan
Write-Host "Blazor image: $DockerHubUsername/notebookmark-blazor:$BlazorTag" -ForegroundColor Cyan
