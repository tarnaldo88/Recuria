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
            if (owner == null) 
                throw new ArgumentNullException(nameof(owner));

            Organization newOrg = new(name);   
            //AddUserToOrganization(newOrg, owner, UserRole.Owner);
            newOrg.AddUser(owner, UserRole.Owner);
            return newOrg;
        }

        //public async void AddUser(Organization organization, User user, UserRole role, CancellationToken ct)
        //{
        //    if (organization == null) throw new ArgumentNullException(nameof(organization));

        //    if (user == null) throw new ArgumentNullException(nameof(user));

        //    if (organization.Users.Any(u => u.Id == user.Id))
        //        throw new InvalidOperationException("User already exists in organization.");

        //    user.AssignToOrganization(organization, role);
        //    organization.Users.Add(user);
        //    _organizations.Update(organization);

        //    await _uow.CommitAsync(ct);
        //}

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

            //AddUser(org, user, request.Role, ct);
            //AddUserToOrganization(org, user, request.Role);
            org.AddUser(user, role: request.Role);


            _organizations.Update(org);

            await _uow.CommitAsync(ct);
        }
        //NO LONGER NEEDED
        //private void AddUserToOrganization(
        //Organization organization,
        //User user,
        //UserRole role)
        //{
        //    if (organization == null)
        //        throw new ArgumentNullException(nameof(organization));

        //    if (user == null)
        //        throw new ArgumentNullException(nameof(user));

        //    if (organization.Users.Any(u => u.Id == user.Id))
        //        throw new InvalidOperationException("User already exists in organization.");

        //    // Domain behavior
        //    user.AssignToOrganization(organization, role);
        //    organization.Users.Add(user);
        //}
    }
}
