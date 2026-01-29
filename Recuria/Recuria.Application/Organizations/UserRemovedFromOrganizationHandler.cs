using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain.Events.Organization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Organizations
{
    public sealed class UserRemovedFromOrganizationHandler
        : IDomainEventHandler<UserRemovedFromOrganizationDomainEvent>
    {
        private readonly IUserRepository _users;

        public UserRemovedFromOrganizationHandler(
            IUserRepository users)
        {
            _users = users;
        }

        public async Task HandleAsync(
            UserRemovedFromOrganizationDomainEvent @event,
            CancellationToken ct)
        {
            var user = await _users.GetByIdAsync(@event.UserId, ct);
            if (user == null)
                return;

            // Example future logic:
            // user.RevokeAccess();
            // user.DisableIfNoOrganizations();
        }
    }
}
