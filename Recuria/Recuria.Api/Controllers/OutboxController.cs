using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Recuria.Infrastructure.Outbox;
using Recuria.Infrastructure.Persistence;
using Recuria.Application.Interface.Idempotency;
using Recuria.Application.Contracts.Common;
using Recuria.Api.Logging;

namespace Recuria.Api.Controllers
{
    /// <summary>
    /// Operational outbox endpoints (admin or owner only).
    /// </summary>
    [ApiController]
    [Authorize(Policy = "AdminOrOwner")]
    [Route("api/outbox")]
    public sealed class OutboxController : ControllerBase
    {
        private readonly RecuriaDbContext _db;
        private readonly IAuditLogger _audit;
        private readonly IApiIdempotencyStore _idempotencyStore;

        public OutboxController(RecuriaDbContext db, IAuditLogger audit, IApiIdempotencyStore idempotencyStore  )
        {
            _db = db;
            _audit = audit;
            _idempotencyStore = idempotencyStore;

        }

        public sealed record DeadLetteredOutboxItem(
            Guid Id,
            DateTime OccurredOnUtc,
            DateTime DeadLetteredOnUtc,
            string Type,
            string? Error,
            int RetryCount);

        /// <summary>
        /// List dead-lettered outbox messages (paged/sorted/filtered).
        /// </summary>
        [HttpGet("dead-lettered")]
        public async Task<ActionResult<PagedResult<DeadLetteredOutboxItem>>> GetDeadLettered(
            [FromQuery] TableQuery query,
            CancellationToken ct = default)
        {
            var safe = new TableQuery
            {
                Page = Math.Max(1, query.Page),
                PageSize = Math.Clamp(query.PageSize, 5, 200),
                Search = query.Search,
                SortBy = query.SortBy,
                SortDir = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc"
            };

            var q = _db.OutBoxMessages
                .AsNoTracking()
                .Where(m => m.DeadLetteredOnUtc != null);

            if (!string.IsNullOrWhiteSpace(safe.Search))
            {
                var s = safe.Search.Trim();
                q = q.Where(m =>
                    m.Type.Contains(s) ||
                    (m.Error ?? string.Empty).Contains(s));
            }

            q = (safe.SortBy?.ToLowerInvariant(), safe.SortDir) switch
            {
                ("type", "desc") => q.OrderByDescending(m => m.Type),
                ("type", _) => q.OrderBy(m => m.Type),

                ("retrycount", "desc") => q.OrderByDescending(m => m.RetryCount),
                ("retrycount", _) => q.OrderBy(m => m.RetryCount),

                ("deadletteredonutc", "asc") => q.OrderBy(m => m.DeadLetteredOnUtc),
                _ => q.OrderByDescending(m => m.DeadLetteredOnUtc)
            };

            var total = await q.CountAsync(ct);

            var items = await q
                .Skip((safe.Page - 1) * safe.PageSize)
                .Take(safe.PageSize)
                .Select(m => new DeadLetteredOutboxItem(
                    m.Id,
                    m.OccurredOnUtc,
                    m.DeadLetteredOnUtc!.Value,
                    m.Type,
                    m.Error,
                    m.RetryCount))
                .ToListAsync(ct);

            return Ok(new PagedResult<DeadLetteredOutboxItem>
            {
                Items = items,
                Page = safe.Page,
                PageSize = safe.PageSize,
                TotalCount = total
            });
        }

        /// <summary>
        /// Requeue a dead-lettered outbox message for retry.
        /// </summary>
        [HttpPost("{id:guid}/retry")]
        public async Task<IActionResult> Retry(
            Guid id,
            CancellationToken ct = default)
        {
            var message = await _db.OutBoxMessages
                .FirstOrDefaultAsync(m => m.Id == id, ct);

            if (message == null)
                return NotFound();

            if (!message.IsDeadLettered)
                return BadRequest("Message is not dead-lettered.");

            message.Revive();
            await _db.SaveChangesAsync(ct);

            _audit.Log(HttpContext, "outbox.retry", new
            {
                messageId = id
            });

            return NoContent();
        }

        [HttpPost("idempotency/purge")]
        public async Task<ActionResult<int>> PurgeIdempotency([FromQuery] int olderThanHours = 168, CancellationToken ct = default)
        {
            olderThanHours = Math.Clamp(olderThanHours, 1, 24 * 90);
            var cutoff = DateTime.UtcNow.AddHours(-olderThanHours);
            var deleted = await _idempotencyStore.DeleteOlderThanAsync(cutoff, ct);
            return Ok(deleted);
        }
    }
}
