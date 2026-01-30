using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Requests
{
    /// <summary>
    /// Request to create an invoice.
    /// </summary>
    public class CreateInvoiceRequest
    {
        /// <summary>
        /// Organization id to invoice.
        /// </summary>
        public Guid OrganizationId { get; init; }
        /// <summary>
        /// Amount to charge.
        /// </summary>
        public decimal Amount { get; init; }
        /// <summary>
        /// Invoice description.
        /// </summary>
        public string Description { get; init; } = string.Empty;
    }
}
