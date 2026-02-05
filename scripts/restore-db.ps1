$server = $env:RECURIA_SQL_SERVER
$database = $env:RECURIA_SQL_DATABASE
$backupFile = $env:RECURIA_SQL_BACKUP_FILE

if ([string]::IsNullOrWhiteSpace($server) -or
    [string]::IsNullOrWhiteSpace($database) -or
    [string]::IsNullOrWhiteSpace($backupFile)) {
  throw "Set RECURIA_SQL_SERVER, RECURIA_SQL_DATABASE, and RECURIA_SQL_BACKUP_FILE."
}

$query = "RESTORE DATABASE [$database] FROM DISK = N'$backupFile' WITH REPLACE;"

sqlcmd -S $server -Q $query
Write-Host "Restored $database from $backupFile"
