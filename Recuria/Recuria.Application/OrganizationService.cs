using Recuria.Domain;
using Recuria.Domain.Entities;
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
            if (owner == null) throw new ArgumentNullException(nameof(owner));


            Organization newOrg = new(name);            
            AddUser(newOrg, owner, UserRole.Owner);
            return newOrg;
        }

        public void AddUser(Organization organization, User user, UserRole role)
        {
            if (organization.Users.Any(u => u.Id == user.Id))
                throw new InvalidOperationException("User already exists in organization.");
            if (organization == null) throw new ArgumentNullException(nameof(organization));
            if (user == null) throw new ArgumentNullException(nameof(user));

            user.AssignToOrganization(organization, role);
            organization.Users.Add(user);
        }

        public void ChangeUserRole(Organization organization, Guid userId, UserRole newRole)
        {
            var user = organization.Users.FirstOrDefault(u => u.Id == userId) ?? throw new InvalidOperationException("User not found.");
            if (newRole == UserRole.Owner)
                throw new InvalidOperationException("Cannot assign owner role.");

            if (user.Role == UserRole.Owner)
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
                throw new InvalidOperationException("Cannot remove owner.");
            }

            organization.Users.Remove(user);
        }
    }
}
