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
            _validator = validator;
            _uow = uow;
        }

        
        public async Task<Guid> CreateOrganizationAsync(
            CreateOrganizationRequest request,
            CancellationToken ct)
        {
            await _validator.ValidateAsync(request);

            var owner = await _users.GetByIdAsync(request.OwnerId, ct);

            if (owner == null)
                throw new InvalidOperationException("Owner not found.");

            var organization = new Organization(request.Name); //uses domain factory
            organization.AddUser(owner, UserRole.Owner);

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

            org.AddUser(user, role: request.Role);

            _organizations.Update(org);

            await _uow.CommitAsync(ct);
        }

        public async Task ChangeUserRoleAsync(
            Guid organizationId,
            Guid userId,
            UserRole newRole,
            CancellationToken ct)
        {
            var org = await _organizations.GetByIdAsync(organizationId, ct);

            if (org == null)
                throw new InvalidOperationException("Organization not found.");

            // DOMAIN BEHAVIOR
            org.ChangeUserRole(userId, newRole);

            _organizations.Update(org);

            await _uow.CommitAsync(ct);
        }

        public async Task RemoveUserAsync(
            Guid organizationId,
            Guid userId,
            CancellationToken ct)
        {
            var org = await _organizations.GetByIdAsync(organizationId, ct);

            if (org == null)
                throw new InvalidOperationException("Organization not found.");

            // DOMAIN BEHAVIOR
            org.RemoveUser(userId);

            _organizations.Update(org);

            await _uow.CommitAsync(ct);
        }
    }
}
