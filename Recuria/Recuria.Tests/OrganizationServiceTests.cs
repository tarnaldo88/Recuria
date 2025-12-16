using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Recuria.Domain;
//using Recuria.Services;
using Recuria.Application;

namespace Recuria.Tests
{
    public class OrganizationServiceTests
    {
        private readonly OrganizationService _service;
        //private readonly Organization _org;

        public OrganizationServiceTests()
        {
            _service = new OrganizationService();
        }

        private static User CreateUser(string email = "user@test.com")
        {
            return new User( email:email, name:"Test User", role: UserRole.Member, org: null);
        }

        [Fact]
        public void CreateOrganization_Should_CreateOrg_And_AddOwner()
        {
            var owner = CreateUser("owner@test.com");
            var org = _service.CreateOrganization("Test Org", owner);

            org.Should().NotBeNull();
            org.Name.Should().Be("Test Org");
            org.Users.Should().HaveCount(1);
            org.Users[0].Role.Should().Be(UserRole.Owner);
            org.Users[0].Organization.Should().Be(org);
        }

        [Fact]
        public void AddUser_Should_AddUserToOrganization()
        {
            var owner = CreateUser("owner@test.com");
            var org = _service.CreateOrganization("Test Org", owner);
            var user = CreateUser("member@test.com");

            _service.AddUser(org, user, UserRole.Member);

            org.Users.Should().HaveCount(2);
            org.Users.Should().Contain(user);
            user.Organization.Should().Be(org);
            user.Role.Should().Be(UserRole.Member);

        }

        [Fact]
        public void CreateOrganization_Should_Throw_WhenNameIsEmpty()
        {
            var owner = CreateUser();

            Action act = () => _service.CreateOrganization("", owner);

            act.Should().Throw<ArgumentException>().WithMessage("Organization name is required.");
        }
    }
}
