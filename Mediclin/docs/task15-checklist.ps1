$ErrorActionPreference = "Stop"

Write-Host "== MediClin Task 15 Checklist ==" -ForegroundColor Cyan

Write-Host "`n1) Build solution"
dotnet build ".\Mediclin.UI\Mediclin.UI.csproj"

Write-Host "`n2) Run unit tests"
dotnet test ".\Mediclin.Tests\Mediclin.Tests.csproj"

Write-Host "`n3) Run app"
Write-Host "   - Verify DB connection message is NOT shown."
Write-Host "   - If shown, update Mediclin.UI\appsettings.json with correct MySQL password."
Write-Host "   - Then rerun this script."
dotnet run --project ".\Mediclin.UI\Mediclin.UI.csproj"
