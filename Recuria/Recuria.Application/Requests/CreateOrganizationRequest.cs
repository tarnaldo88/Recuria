using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Requests
{
    /// <summary>
    /// Request to create an organization.
    /// </summary>
    public class CreateOrganizationRequest
    {
        /// <summary>
        /// Organization name.
        /// </summary>
        public string Name { get; init; } = string.Empty;
        /// <summary>
        /// Owner user id.
        /// </summary>
        public Guid OwnerId { get; init; }
    }
}
