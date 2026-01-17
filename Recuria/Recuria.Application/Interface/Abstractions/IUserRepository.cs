using Recuria.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Application.Interface.Abstractions
{
    public interface IUserRepository
    {
        Task AddAsync(User owner, CancellationToken none);
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
        void Update(User user);
    }
}
