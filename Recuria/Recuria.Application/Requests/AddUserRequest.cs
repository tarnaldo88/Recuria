using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Recuria.Domain.Enums;

namespace Recuria.Application.Requests
{
    /// <summary>
    /// Request to add a user to an organization.
    /// </summary>
    public class AddUserRequest
    {
        /// <summary>
        /// User display name.
        /// </summary>
        public string? Name { get; init; }
        /// <summary>
        /// User email address (required if user does not exist).
        /// </summary>
        public string? Email { get; init; }
        /// <summary>
        /// Role to assign.
        /// </summary>
        public UserRole Role { get; init; }
        /// <summary>
        /// User id.
        /// </summary>
        public Guid UserId { get; init; }
    }
}
