using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Requests
{
    public class CreateOrganizationRequest
    {
        public string Name { get; init; } = string.Empty;
        public Guid OwnerId { get; init; }
    }
}
