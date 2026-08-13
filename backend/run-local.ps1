<#
run-local.ps1
Builds the solution, applies EF migrations (optional), and runs the SEAL_Backend project
Usage:
  .\run-local.ps1            # run with migrations
  .\run-local.ps1 -SkipMigrations  # skip migrations
#>

param(
    [switch]$SkipMigrations
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $scriptDir

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet not found. Install .NET SDK (https://dotnet.microsoft.com) and restart terminal."
    exit 1
}

$env:ASPNETCORE_ENVIRONMENT = "Development"

Write-Host "Building solution..."
dotnet build SEAL_Backend.slnx -c Debug
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Fix compile errors before running."
    exit $LASTEXITCODE
}

if (-not $SkipMigrations) {
    Write-Host "Applying EF migrations (may require dotnet-ef tool)..."
    dotnet ef database update --project SEAL.Infrastructure --startup-project SEAL_Backend -v
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Applying migrations failed. Create DB or run migrations manually if needed."
    }
}

Write-Host "Starting SEAL_Backend..."
dotnet run --project SEAL_Backend/SEAL_Backend.csproj
