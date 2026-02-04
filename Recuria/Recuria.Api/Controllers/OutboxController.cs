using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Recuria.Infrastructure.Outbox;
using Recuria.Infrastructure.Persistence;
using Recuria.Api.Logging;

namespace Recuria.Api.Controllers
{
    /// <summary>
    /// Operational outbox endpoints (admin only).
    /// </summary>
    [ApiController]
    [Authorize(Policy = "OwnerOnly")]
    [Route("api/outbox")]
    public sealed class OutboxController : ControllerBase
    {
        private readonly RecuriaDbContext _db;
        private readonly IAuditLogger _audit;

        public OutboxController(RecuriaDbContext db, IAuditLogger audit)
        {
            _db = db;
            _audit = audit;
        }

        public sealed record DeadLetteredOutboxItem(
            Guid Id,
            DateTime OccurredOnUtc,
            DateTime DeadLetteredOnUtc,
            string Type,
            string? Error,
            int RetryCount);

        /// <summary>
        /// List dead-lettered outbox messages.
        /// </summary>
        [HttpGet("dead-lettered")]
        public async Task<ActionResult<IReadOnlyList<DeadLetteredOutboxItem>>> GetDeadLettered(
            [FromQuery] int take = 50,
            CancellationToken ct = default)
        {
            take = Math.Clamp(take, 1, 200);

            var items = await _db.OutBoxMessages
                .AsNoTracking()
                .Where(m => m.DeadLetteredOnUtc != null)
                .OrderByDescending(m => m.DeadLetteredOnUtc)
                .Take(take)
                .Select(m => new DeadLetteredOutboxItem(
                    m.Id,
                    m.OccurredOnUtc,
                    m.DeadLetteredOnUtc!.Value,
                    m.Type,
                    m.Error,
                    m.RetryCount))
                .ToListAsync(ct);

            return Ok(items);
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
    }
}
