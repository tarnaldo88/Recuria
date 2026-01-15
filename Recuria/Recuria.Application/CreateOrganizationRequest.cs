using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application
{
    public class CreateOrganizationRequest
    {
        public string Name { get; init; } = string.Empty;

        public string OwnerEmail { get; init; } = string.Empty;

        public string OwnerFirstName { get; init; } = string.Empty;

        public string OwnerLastName { get; init; } = string.Empty;
    }
}
