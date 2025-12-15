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
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Organization name is required.");

            Organization newOrg = new(name);            
            AddUser(newOrg, owner, UserRole.Owner);
            return newOrg;
        }

        void AddUser(Organization organization, User user, UserRole role)
        {
            if (organization.Users.Any(u => u.Id == user.Id))
                throw new InvalidOperationException("User already exists in organization.");

            user.AssignToOrganization(organization, role);
            var newUser = new User(user.Email, user.Name, user.Role, organization);
            organization.Users.Add(newUser);
        }

        void ChangeUserRole(Organization organization, Guid userId, UserRole role)
        {

        }

        void RemoveUser(Organization organization, Guid userId)
        {

        }
    }
}
