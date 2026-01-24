using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recuria.Domain.Enums;

namespace Recuria.Application.Requests
{
    public class AddUserRequest
    {
        public string? Name { get; init; }
        public string? Email { get; init; }
        public UserRole Role { get; init; }
        public Guid UserId { get; init; }
    }
}
