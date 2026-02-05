$server = $env:RECURIA_SQL_SERVER
$database = $env:RECURIA_SQL_DATABASE
$backupPath = $env:RECURIA_SQL_BACKUP_PATH

if ([string]::IsNullOrWhiteSpace($server) -or
    [string]::IsNullOrWhiteSpace($database) -or
    [string]::IsNullOrWhiteSpace($backupPath)) {
  throw "Set RECURIA_SQL_SERVER, RECURIA_SQL_DATABASE, and RECURIA_SQL_BACKUP_PATH."
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$file = Join-Path $backupPath "$database-$timestamp.bak"

$query = "BACKUP DATABASE [$database] TO DISK = N'$file' WITH INIT, COMPRESSION;"

sqlcmd -S $server -Q $query
Write-Host "Backup written to $file"
