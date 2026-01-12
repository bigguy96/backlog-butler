Write-Host "Setting Backlog Butler environment variables"

$org = Read-Host "Azure DevOps org URL (e.g. https://dev.azure.com/yourorg)"
$project = Read-Host "Azure DevOps project name"
$pat = Read-Host "Azure DevOps PAT" -AsSecureString

# Convert SecureString to plain text only in memory
$patPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($pat)
)

$env:ADO_ORG = $org
$env:ADO_PROJECT = $project
$env:ADO_PAT = $patPlain

Write-Host ""
Write-Host "Environment variables set for this PowerShell session."
Write-Host "You can now run:"
Write-Host "  dotnet run --project src/BacklogButler.Cli"