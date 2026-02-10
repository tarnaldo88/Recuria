using Recuria.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recuria.Domain.Enums;
using System.Security.Cryptography;

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
        public Guid? OrganizationId { get; private set; }
        public Organization? Organization { get; private set; } = null;

        public string? PasswordHash { get; private set; }
        public string? PasswordSalt { get; private set; }
        public int TokenVersion { get; private set; }

        public User(string email, string name)
        {
            Id = Guid.NewGuid();
            Email = email;
            Name = name;
        }

        public User(string email, string name, UserRole role, Organization? org)
        {
            Id = Guid.NewGuid();
            Email = email;
            Name = name;
            Role = role;
            Organization = org;
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

        public void SetPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password is required.", nameof(password));

            var saltBytes = RandomNumberGenerator.GetBytes(16);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                120_000,
                HashAlgorithmName.SHA256,
                32);

            PasswordSalt = Convert.ToBase64String(saltBytes);
            PasswordHash = Convert.ToBase64String(hashBytes);
        }

        public bool VerifyPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(PasswordHash) ||
                string.IsNullOrWhiteSpace(PasswordSalt))
            {
                return false;
            }

            byte[] saltBytes;
            byte[] expectedHashBytes;
            try
            {
                saltBytes = Convert.FromBase64String(PasswordSalt);
                expectedHashBytes = Convert.FromBase64String(PasswordHash);
            }
            catch
            {
                return false;
            }

            var actualHashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                120_000,
                HashAlgorithmName.SHA256,
                expectedHashBytes.Length);

            return CryptographicOperations.FixedTimeEquals(actualHashBytes, expectedHashBytes);
        }

        public void RotateTokenVersion()
        {
            checked { TokenVersion++; }
        }
    }
}
