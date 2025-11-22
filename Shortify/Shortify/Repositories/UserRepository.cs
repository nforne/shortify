using Microsoft.EntityFrameworkCore;
using Shortify.Data;
using Shortify.Models;
using Shortify.Repositories.Interfaces;

namespace Shortify.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ShortifyDbContext _db;
        public UserRepository(ShortifyDbContext db) => _db = db;

        public async Task<IEnumerable<UserEntity>> GetAllAsync(string tenantId, int page, int pageSize, CancellationToken ct = default)
        {
            return await _db.Users
                .Where(u => u.TenantId == tenantId && u.Status != "deleted")
                .OrderBy(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
        }

        public Task<UserEntity?> GetByIdAsync(int id, CancellationToken ct = default) =>
            _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Status != "deleted", ct);

        public Task<UserEntity?> GetByEmailAsync(string tenantId, string email, CancellationToken ct = default) =>
            _db.Users.FirstOrDefaultAsync(u => u.TenantId == tenantId && u.Email == email && u.Status != "deleted", ct);

        public async Task<UserEntity> AddAsync(UserEntity entity, CancellationToken ct = default)
        {
            _db.Users.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task UpdateAsync(UserEntity entity, CancellationToken ct = default)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _db.Users.Update(entity);
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(UserEntity entity, CancellationToken ct = default)
        {
            _db.Users.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }
}
