using FluentAssertions;

//using Recuria.Services;
using Recuria.Application;
using Recuria.Application.Interface;
using Recuria.Application.Interface.Abstractions;
using Recuria.Application.Requests;
using Recuria.Application.Validation;
using Recuria.Domain;
using Recuria.Domain.Entities;
using Recuria.Domain.Enums;
using Recuria.Domain.Events.Organization;
using Recuria.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Recuria.Tests.Unit.Domain
{
    public class OrganizationServiceTests
    {
        private readonly OrganizationService _service;
        private readonly IOrganizationRepository _organizations;
        private readonly IUserRepository _users;
        private readonly IOrganizationQueries _queries;
        private readonly ValidationBehavior _validator;
        private readonly UnitOfWork unitOfWork;
        //private readonly Organization _org;

        public OrganizationServiceTests()
        {
            _service = new OrganizationService(_organizations, _users, _queries, _validator, unitOfWork);
        }

        private static User CreateUser(string email = "user@test.com")
        {
            return new User( email:email, name:"Test User", role: UserRole.Member, org: null);
        }

        [Fact]
        public void OrganizationConstructor_Should_AddOwner_And_RaiseEvent()
        {
            // Arrange
            var owner = CreateUser("owner@test.com");

            // Act
            var org = new Organization("Test Org");
            org.AddUser(owner, UserRole.Owner);

            // Assert
            org.Name.Should().Be("Test Org");
            org.Users.Should().ContainSingle();
            org.Users.Single().Role.Should().Be(UserRole.Owner);

            org.DomainEvents.Should()
                .ContainSingle(e => e is OrganizationCreatedDomainEvent);
        }

    }
}
