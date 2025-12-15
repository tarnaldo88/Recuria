using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application
{
    public interface IOrganizationService
    {
        Organization CreateOrganization(string name, User owner);

        void AddUser(Organization organization, User user, UserRole role);

        void ChangeUserRole(Organization organization, Guid userId, UserRole role);

        void RemoveUser(Organization organization, Guid userId);
    }
}
