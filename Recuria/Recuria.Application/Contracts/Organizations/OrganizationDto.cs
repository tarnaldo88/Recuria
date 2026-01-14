using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Contracts.Organizations
{
    public sealed class OrganizationDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; }
        public DateTime CreatedAt { get; init; }

        public OrganizationDto(Guid id, string name, DateTime createdAt)
        {
            Id = id;
            Name = name;
            CreatedAt = createdAt;
        }
    }
}
