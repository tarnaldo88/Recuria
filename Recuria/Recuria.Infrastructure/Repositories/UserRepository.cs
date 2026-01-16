using Microsoft.EntityFrameworkCore;
using Recuria.Application.Interface.Abstractions;
using Recuria.Domain;
using Recuria.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recuria.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly RecuriaDbContext _db;

        public UserRepository(RecuriaDbContext db)
        {
            _db = db;
        }

        public async Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken ct)
        {
            return await _db.Users
                .FirstOrDefaultAsync(u => u.Id == id, ct);
        }
    }

}
