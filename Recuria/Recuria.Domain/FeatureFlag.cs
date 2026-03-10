using System;

namespace Recuria.Domain
{
    public class FeatureFlag
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public string? EnabledFor { get; set; } // JSON: ["user1@example.com", "org:guid"]
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Environment { get; set; } = "all"; // all, dev, staging, prod
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
    }
}