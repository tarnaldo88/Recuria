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

## Health checks
Liveness:
```
GET http://localhost:5132/health/live
```
Readiness (DB):
```
GET http://localhost:5132/health/ready
```

## JWT configuration (non-development)
In non-development environments, the API requires these settings:
```
Jwt:Issuer
Jwt:Audience
Jwt:SigningKey
```
If any are missing, the app will fail fast on startup.

Environment variable mapping (Windows):
```
Jwt__Issuer
Jwt__Audience
Jwt__SigningKey
```

User secrets (local dev):
```
dotnet user-secrets init --project Recuria.Api
dotnet user-secrets set "Jwt:Issuer" "Recuria" --project Recuria.Api
dotnet user-secrets set "Jwt:Audience" "Recuria.Api" --project Recuria.Api
dotnet user-secrets set "Jwt:SigningKey" "CHANGE_ME_DEV_KEY" --project Recuria.Api
```

## Database reliability (runbook)
Backup (SQL Server):
```sql
BACKUP DATABASE RecuriaDb TO DISK = 'C:\backups\RecuriaDb.bak' WITH INIT;
```

Restore (SQL Server):
```sql
RESTORE DATABASE RecuriaDb FROM DISK = 'C:\backups\RecuriaDb.bak' WITH REPLACE;
```

Rollback strategy:
- Prefer restoring a known-good backup in production.
- For dev/test, you can roll back via EF migrations:
```
dotnet ef database update <PreviousMigration> --project Recuria.Infrastructure --startup-project Recuria.Api
```

## Swagger (Development)
```
http://localhost:5132/swagger
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
