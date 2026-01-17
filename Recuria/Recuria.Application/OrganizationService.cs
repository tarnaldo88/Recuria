using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Requests;
using Recuria.Application.Validation;
using Recuria.Domain;
using Recuria.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application
{
    public class OrganizationService : IOrganizationService
    {
        private readonly IOrganizationRepository _organizations;
        private readonly IUserRepository _users;
        private readonly IOrganizationQueries _queries;
        private readonly ValidationBehavior _validator;
        private readonly IUnitOfWork _uow;

        public OrganizationService(
            IOrganizationRepository organizations,
            IUserRepository users,
            IOrganizationQueries queries,
            ValidationBehavior validator,
            IUnitOfWork uow)
        {
            _organizations = organizations;
            _users = users;
            _queries = queries;
            _validator = validator;
            _uow = uow;
        }

        public Organization CreateOrganization(string name, User owner)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Organization name is required.");
            if (owner == null) throw new ArgumentNullException(nameof(owner));


            Organization newOrg = new(name);   
            CancellationToken cancellationToken = new CancellationToken();
            AddUser(newOrg, owner, UserRole.Owner, cancellationToken);
            return newOrg;
        }

        public async void AddUser(Organization organization, User user, UserRole role, CancellationToken ct)
        {
            if (organization == null) throw new ArgumentNullException(nameof(organization));

            if (user == null) throw new ArgumentNullException(nameof(user));

            if (organization.Users.Any(u => u.Id == user.Id))
                throw new InvalidOperationException("User already exists in organization.");

            user.AssignToOrganization(organization, role);
            organization.Users.Add(user);
            _organizations.Update(organization);

            await _uow.CommitAsync(ct);
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

        public async Task<Guid> CreateOrganizationAsync(
            CreateOrganizationRequest request,
            CancellationToken ct)
        {
            await _validator.ValidateAsync(request);

            var owner = await _users.GetByIdAsync(request.OwnerId, ct);

            if (owner == null)
                throw new InvalidOperationException("Owner not found.");

            var organization = new Organization(request.Name);

            // Domain behavior
            AddUser(organization, owner, UserRole.Owner, ct);

            await _organizations.AddAsync(organization, ct);
            await _uow.CommitAsync(ct);

            return organization.Id;
        }

        public async Task AddUserAsync(Guid id, AddUserRequest request, CancellationToken ct)
        {
            await _validator.ValidateAsync(request);

            var org = await _organizations.GetByIdAsync(id, ct);

            if (org == null)
                throw new InvalidOperationException("Organization not found.");

            var user = await _users.GetByIdAsync(request.UserId, ct);

            if (user == null)
                throw new InvalidOperationException("User not found.");

            AddUser(org, user, request.Role, ct);

            _organizations.Update(org);

            await _uow.CommitAsync(ct);
        }
    }
}
