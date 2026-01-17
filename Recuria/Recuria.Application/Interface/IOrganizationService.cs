using Recuria.Application.Requests;
using Recuria.Domain;
using Recuria.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Interface
{
    public interface IOrganizationService
    {
        Organization CreateOrganization(string name, User owner);

        Task<Guid> CreateOrganizationAsync(CreateOrganizationRequest request, CancellationToken cancellationToken);
        Task AddUserAsync(Guid id, AddUserRequest request, CancellationToken cancellationToken);
    }
}
