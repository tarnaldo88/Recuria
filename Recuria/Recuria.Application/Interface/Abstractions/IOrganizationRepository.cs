using Recuria.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Interface.Abstractions
{
    public interface IOrganizationRepository
    {
        Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct);
    }
}
