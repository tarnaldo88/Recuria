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
        public Organization CreateOrganization(string name, User owner)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Organization name is required.");

            Organization newOrg = new(name);            
            AddUser(newOrg, owner, UserRole.Owner);
            return newOrg;
        }

        public void AddUser(Organization organization, User user, UserRole role)
        {
            if (organization.Users.Any(u => u.Id == user.Id))
                throw new InvalidOperationException("User already exists in organization.");

            user.AssignToOrganization(organization, role);
            organization.Users.Add(user);
        }

        public void ChangeUserRole(Organization organization, Guid userId, UserRole newRole)
        {
            var user = organization.Users.FirstOrDefault(u => u.Id == userId) ?? throw new InvalidOperationException("User not found.");
            if(user.Role == UserRole.Owner)
            {
                throw new InvalidOperationException("Cannot change owner role.");
            }
            user.ChangeRole(newRole);

        }

        public void RemoveUser(Organization organization, Guid userId)
        {
            var user = organization.Users.FirstOrDefault(u => u.Id == userId) ?? throw new InvalidOperationException("User not found.");
            if (user.Role == UserRole.Owner)
            {
                throw new InvalidOperationException("Cannot change owner role.");
            }

            organization.Users.Remove(user);
        }
    }
}
