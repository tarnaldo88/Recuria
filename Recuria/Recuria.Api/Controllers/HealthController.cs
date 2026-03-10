using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Recuria.Infrastructure.Persistence;
using System.Diagnostics;

namespace Recuria.Api.Controllers
{
    /// <summary>
    /// Comprehensive health and monitoring endpoint.
    /// </summary>
    [ApiController]
    [Route("api/health")]
    public sealed class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly RecuriaDbContext _db;
        private readonly HealthCheckService _healthCheckService;
        private readonly IWebHostEnvironment _env;

        public HealthController(
            ILogger<HealthController> logger,
            RecuriaDbContext db,
            HealthCheckService healthCheckService,
            IWebHostEnvironment env)
        {
            _logger = logger;
            _db = db;
            _healthCheckService = healthCheckService;
            _env = env;
        }

        /// <summary>
        /// Simple health check.
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogDebug("Health check requested.");
            return Ok(new { status = "ok", timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Detailed system status with all components.
        /// </summary>
        [HttpGet("detailed")]
        public async Task<IActionResult> GetDetailed()
        {
            var stopwatch = Stopwatch.StartNew();
            
            var report = await _healthCheckService.CheckHealthAsync();
            stopwatch.Stop();

            var result = new
            {
                Status = report.Status.ToString(),
                TotalDuration = stopwatch.ElapsedMilliseconds,
                Timestamp = DateTime.UtcNow,
                Environment = _env.EnvironmentName,
                Version = GetType().Assembly.GetName().Version?.ToString() ?? "unknown",
                Entries = report.Entries.Select(e => new
                {
                    Name = e.Key,
                    Status = e.Value.Status.ToString(),
                    Duration = e.Value.Duration.TotalMilliseconds,
                    Description = e.Value.Description,
                    Exception = e.Value.Exception?.Message
                }).ToList()
            };

            return Ok(result);
        }

        /// <summary>
        /// Database performance metrics.
        /// </summary>
        [HttpGet("database")]
        public async Task<IActionResult> GetDatabaseMetrics()
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Test database connectivity and get metrics
                var canConnect = await _db.Database.CanConnectAsync();
                var pendingMigrations = await _db.Database.GetPendingMigrationsAsync();
                var appliedMigrations = await _db.Database.GetAppliedMigrationsAsync();
                
                // Get table statistics
                var tableStats = new List<object>();
                try
                {
                    var connection = _db.Database.GetDbConnection();
                    await connection.OpenAsync();
                    
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT 
                            t.name AS TableName,
                            p.rows AS RowCount,
                            SUM(a.total_pages) * 8 AS TotalSizeKB
                        FROM sys.tables t
                        INNER JOIN sys.partitions p ON t.object_id = p.object_id
                        INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
                        WHERE t.is_ms_shipped = 0 AND p.index_id IN (0,1)
                        GROUP BY t.name, p.rows
                        ORDER BY TotalSizeKB DESC";
                    
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        tableStats.Add(new
                        {
                            TableName = reader.GetString(0),
                            RowCount = reader.GetInt64(1),
                            SizeKB = reader.GetInt64(2)
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve detailed table statistics");
                }

                stopwatch.Stop();

                return Ok(new
                {
                    Status = canConnect ? "Healthy" : "Unhealthy",
                    ResponseTime = stopwatch.ElapsedMilliseconds,
                    PendingMigrations = pendingMigrations.Count(),
                    AppliedMigrations = appliedMigrations.Count(),
                    TableStatistics = tableStats,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return StatusCode(500, new { Status = "Unhealthy", Error = ex.Message });
            }
        }

        /// <summary>
        /// API performance metrics.
        /// </summary>
        [HttpGet("performance")]
        public IActionResult GetPerformanceMetrics()
        {
            var currentProcess = Process.GetCurrentProcess();
            
            var metrics = new
            {
                Timestamp = DateTime.UtcNow,
                Process = new
                {
                    Id = currentProcess.Id,
                    ProcessName = currentProcess.ProcessName,
                    WorkingSet = currentProcess.WorkingSet64 / (1024 * 1024), // MB
                    PrivateMemory = currentProcess.PrivateMemorySize64 / (1024 * 1024), // MB
                    VirtualMemory = currentProcess.VirtualMemorySize64 / (1024 * 1024), // MB
                    StartTime = currentProcess.StartTime,
                    TotalProcessorTime = currentProcess.TotalProcessorTime.TotalSeconds,
                    ThreadCount = currentProcess.Threads.Count,
                    HandleCount = currentProcess.HandleCount
                },
                GC = new
                {
                    TotalMemory = GC.GetTotalMemory(false) / (1024 * 1024), // MB
                    Gen0Collections = GC.CollectionCount(0),
                    Gen1Collections = GC.CollectionCount(1),
                    Gen2Collections = GC.CollectionCount(2)
                },
                Environment = new
                {
                    MachineName = Environment.MachineName,
                    ProcessorCount = Environment.ProcessorCount,
                    TickCount = Environment.TickCount64,
                    Version = Environment.Version.ToString()
                }
            };

            return Ok(metrics);
        }
    }
}