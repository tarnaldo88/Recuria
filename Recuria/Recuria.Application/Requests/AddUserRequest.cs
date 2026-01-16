using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Requests
{
    public class AddUserRequest
    {
        public string Name { get; init; } = null!;
        public string Email { get; init; } = null!;
        public UserRole Role { get; init; }
        public Guid UserId { get; init; }
    }
}
