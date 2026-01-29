# Recuria API Runbook (Local Dev)

## Prereqs
- .NET SDK 8.x
- SQL Server or LocalDB

## Configure
Set the connection string in `Recuria.Api/appsettings.Development.json`:
```
ConnectionStrings:DefaultConnection
```

## Database
Apply migrations (from repo root):
```powershell
dotnet ef database update --project Recuria.Infrastructure --startup-project Recuria.Api
```

## Run the API
```powershell
dotnet run --project Recuria.Api
```

Health check:
```
GET http://localhost:5132/api/health
```

## Dev bootstrap (Development only)
Creates an owner user + organization and returns a JWT with `org_id` + `role`.
```powershell
Invoke-RestMethod -Method Post -Uri http://localhost:5132/api/dev/bootstrap `
  -ContentType "application/json" `
  -Body (@{ organizationName = "Acme Inc"; ownerEmail = "owner@recuria.local"; ownerName = "Owner One" } | ConvertTo-Json)
```

## API examples
See `Recuria.Api/Recuria.Api.http` for the full flow:
create org → add users → subscriptions → invoices.

## Tests
```powershell
dotnet test Recuria.Tests/Recuria.Tests.csproj
```
