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
    /// Request to change a user's role.
    /// </summary>
    public class ChangeUserRoleRequest
    {
        /// <summary>
        /// New role for the user.
        /// </summary>
        public UserRole NewRole { get; set; }
    }
}
