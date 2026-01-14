using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Interface
{
    public interface IUserContext
    {
        Guid UserId { get; }
        Guid OrganizationId { get; }
    }
}
