using Microsoft.Extensions.Logging;
using Recuria.Application.Interface;
using Recuria.Domain.Events.Organization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Organizations
{
    public sealed class LogUserAddedToOrganization
    : IDomainEventHandler<UserAddedToOrganizationDomainEvent>
    {
        private readonly ILogger<LogUserAddedToOrganization> _logger;

        public LogUserAddedToOrganization(
            ILogger<LogUserAddedToOrganization> logger)
        {
            _logger = logger;
        }

        public Task HandleAsync(
            UserAddedToOrganizationDomainEvent @event,
            CancellationToken ct)
        {
            _logger.LogInformation(
                "User {UserId} added to Organization {OrganizationId} with role {Role}",
                @event.UserId,
                @event.OrganizationId,
                @event.Role);

            return Task.CompletedTask;
        }
    }
}
