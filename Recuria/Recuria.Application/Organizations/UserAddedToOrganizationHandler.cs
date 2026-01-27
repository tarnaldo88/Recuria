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
    public sealed class UserAddedToOrganizationHandler : IDomainEventHandler<UserAddedToOrganizationDomainEvent>
    {
        private readonly IUserRepository _users;

        public UserAddedToOrganizationHandler(
            IUserRepository users)
        {
            _users = users;
        }
        public async Task HandleAsync(
           UserAddedToOrganizationDomainEvent @event,
           CancellationToken ct)
        {
            var user = await _users.GetByIdAsync(@event.UserId, ct);
            if (user == null)
                return;

            // Placeholder for future side effects:
            // - Send invite email
            // - Provision permissions
            // - Audit log

        }
    }
}
