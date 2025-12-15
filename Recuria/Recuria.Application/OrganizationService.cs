using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application
{
    public class OrganizationService : IOrganizationService
    {
        Organization CreateOrganization(string name, User owner)
        {
            Organization newOrg = new Organization(name);
            newOrg.Users.Add(owner);
            return newOrg;
        }

        void AddUser(Organization organization, User user, UserRole role)
        {
            var newUser = new User(user.Email, user.Name, user.Role, organization);
            organization.Users.Add(newUser);
        }

        void ChangeUserRole(Organization organization, Guid userId, UserRole role)
        {

        }

        void RemoveUser(Organization organization, Guid userId);
    }
}
