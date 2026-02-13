using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Persistence.Entities
{
    public sealed class ApiIdempotencyRecord
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string Operation { get; set; } = string.Empty; // e.g. "invoice.create"
        public string IdempotencyKey { get; set; } = string.Empty;
        public string RequestHash { get; set; } = string.Empty;
        public Guid ResourceId { get; set; } // invoiceId
        public DateTime CreatedOnUtc { get; set; }

        private ApiIdempotencyRecord() { }

        public ApiIdempotencyRecord(
            Guid organizationId,
            string operation,
            string idempotencyKey,
            string requestHash,
            Guid resourceId)
        {
            Id = Guid.NewGuid();
            OrganizationId = organizationId;
            Operation = operation;
            IdempotencyKey = idempotencyKey;
            RequestHash = requestHash;
            ResourceId = resourceId;
            CreatedOnUtc = DateTime.UtcNow;
        }
    }
}
