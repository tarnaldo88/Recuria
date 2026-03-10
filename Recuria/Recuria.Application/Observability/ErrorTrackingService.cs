using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Recuria.Application.Observability
{
    public interface IErrorTrackingService
    {
        void TrackError(Exception exception, string context, Dictionary<string, object>? metadata = null);
        void TrackWarning(string message, string context, Dictionary<string, object>? metadata = null);
        Task<IReadOnlyList<ErrorRecord>> GetRecentErrorsAsync(int count = 50);
        Task<IReadOnlyList<ErrorSummary>> GetErrorSummaryAsync(TimeSpan period);
        void ClearErrors();
    }

    public class ErrorTrackingService : IErrorTrackingService
    {
        private readonly ILogger<ErrorTrackingService> _logger;
        private readonly ConcurrentQueue<ErrorRecord> _errors = new();
        private readonly int _maxErrors = 1000;

        public ErrorTrackingService(ILogger<ErrorTrackingService> logger)
        {
            _logger = logger;
        }

        public void TrackError(Exception exception, string context, Dictionary<string, object>? metadata = null)
        {
            var record = new ErrorRecord
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Type = "Error",
                Context = context,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                Metadata = metadata ?? new Dictionary<string, object>()
            };

            _errors.Enqueue(record);

            // Trim old errors
            while (_errors.Count > _maxErrors)
            {
                _errors.TryDequeue(out _);
            }

            _logger.LogError(exception, "Error tracked in {Context}: {Message}", context, exception.Message);
        }

        public void TrackWarning(string message, string context, Dictionary<string, object>? metadata = null)
        {
            var record = new ErrorRecord
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Type = "Warning",
                Context = context,
                Message = message,
                Metadata = metadata ?? new Dictionary<string, object>()
            };

            _errors.Enqueue(record);

            while (_errors.Count > _maxErrors)
            {
                _errors.TryDequeue(out _);
            }

            _logger.LogWarning("Warning tracked in {Context}: {Message}", context, message);
        }

        public Task<IReadOnlyList<ErrorRecord>> GetRecentErrorsAsync(int count = 50)
        {
            var recent = _errors
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToList();

            return Task.FromResult<IReadOnlyList<ErrorRecord>>(recent);
        }

        public Task<IReadOnlyList<ErrorSummary>> GetErrorSummaryAsync(TimeSpan period)
        {
            var cutoff = DateTime.UtcNow - period;
            
            var summary = _errors
                .Where(e => e.Timestamp >= cutoff)
                .GroupBy(e => new { e.Context, e.Type })
                .Select(g => new ErrorSummary
                {
                    Context = g.Key.Context,
                    Type = g.Key.Type,
                    Count = g.Count(),
                    LastOccurrence = g.Max(e => e.Timestamp),
                    FirstOccurrence = g.Min(e => e.Timestamp)
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            return Task.FromResult<IReadOnlyList<ErrorSummary>>(summary);
        }

        public void ClearErrors()
        {
            while (_errors.TryDequeue(out _)) { }
        }
    }

    public class ErrorRecord
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ErrorSummary
    {
        public string Context { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
        public DateTime LastOccurrence { get; set; }
        public DateTime FirstOccurrence { get; set; }
    }
}