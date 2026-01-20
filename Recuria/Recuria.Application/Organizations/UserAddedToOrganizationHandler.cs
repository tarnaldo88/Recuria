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
        private readonly IUnitOfWork _uow;

        public UserAddedToOrganizationHandler(
            IUserRepository users,
            IUnitOfWork uow)
        {
            _users = users;
            _uow = uow;
        }
    }
}
