using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Domain
{
    public class User
    {
        public Guid Id { get; init; }
        public string Email { get; private set; } = null!;
        public string Name { get; private set; } = null!;

        // Role within organization
        public UserRole Role { get; private set; }

        // Reference to organization
        public Guid OrganizationId { get; private set; }
        public Organization Organization { get; private set; } = null!;

        public User(string email, string name)
        {
            Email = email;
            Name = name;
        }

        private User() { } //For EF Core

        public void AssignToOrganization(Organization organization, UserRole role)
        {
            Organization = organization;
            OrganizationId = organization.Id;
            Role = role;
        }

        public void ChangeRole(UserRole newRole)
        {
            Role = newRole;
        }
    }

    public enum UserRole {
        Owner,
        Admin,
        Member
    }

}
