using Recuria.Application.Requests;
using Recuria.Domain;
using Recuria.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recuria.Domain.Enums;

namespace Recuria.Application.Interface
{
    public interface IOrganizationService
    {
        Task<Guid> CreateOrganizationAsync(CreateOrganizationRequest request, CancellationToken cancellationToken);
        Task AddUserAsync(Guid id, AddUserRequest request, CancellationToken cancellationToken);
        Task ChangeUserRoleAsync(Guid organizationId,
            Guid userId,
            UserRole newRole,
            CancellationToken ct);
        Task RemoveUserAsync(
            Guid organizationId,
            Guid userId,
            CancellationToken ct);
    }
}
