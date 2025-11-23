using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shortify.Data;
using Shortify.Models;
using Shortify.Repositories.Interfaces;

namespace Shortify.Repositories
{
    public class AttributeRepository : IAttributeRepository
    {
        private readonly ShortifyDbContext _db;
        public AttributeRepository(ShortifyDbContext db) => _db = db;

        public async Task<AttributeEntity> AddAsync(AttributeEntity entity, CancellationToken ct = default)
        {
            _db.Attributes.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task<AttributeEntity?> GetByIdAsync(int id, CancellationToken ct = default)
            => await _db.Attributes.AsNoTracking().SingleOrDefaultAsync(a => a.Id == id && a.Status != "deleted", ct);

        public async Task<AttributeEntity?> GetByKeyAsync(string tenantId, string key, CancellationToken ct = default)
            => await _db.Attributes.SingleOrDefaultAsync(a => a.TenantId == tenantId && a.Key == key && a.Status != "deleted", ct);

        public async Task<IEnumerable<AttributeEntity>> ListAsync(string tenantId, bool includePrivate = false, CancellationToken ct = default)
        {
            var q = _db.Attributes.AsNoTracking().Where(a => a.TenantId == tenantId && a.Status == "active");
            q = q.Where(a => a.ExpiryDate == null || a.ExpiryDate > DateTime.UtcNow);
            if (!includePrivate) q = q.Where(a => a.Visibility == "public");
            return await q.OrderBy(a => a.Key).ToListAsync(ct);
        }

        public async Task UpdateAsync(AttributeEntity entity, CancellationToken ct = default)
        {
            _db.Attributes.Update(entity);
            try { await _db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException) { throw new ConflictException("Attribute concurrency conflict"); }
        }

        public async Task SoftDeleteAsync(AttributeEntity entity, CancellationToken ct = default)
        {
            entity.Status = "deleted";
            entity.DeletedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            _db.Attributes.Update(entity);
            await _db.SaveChangesAsync(ct);
        }

        public async Task HardDeleteAsync(AttributeEntity entity, CancellationToken ct = default)
        {
            _db.Attributes.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }

    // keep/centralize domain exceptions elsewhere; shown inline for compile completeness
    //public class ConflictException : Exception { public ConflictException(string m) : base(m) { } }
}
