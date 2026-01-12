# backlog-butler
Keeps the backlog tidy without making a mess

## Setup (recommended)

Backlog Butler reads Azure DevOps credentials from environment variables.

### PowerShell (Windows)
```powershell
.\scripts\set-env.ps1
dotnet run --project src/BacklogButler.Cli