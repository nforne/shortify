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
    public class MetricRepository : IMetricRepository
    {
        private readonly ShortifyDbContext _db;
        public MetricRepository(ShortifyDbContext db) => _db = db;

        public async Task<MetricEntity> AddAsync(MetricEntity entity, CancellationToken ct = default)
        {
            _db.Metrics.Add(entity);
            await _db.SaveChangesAsync(ct);
            return entity;
        }

        public async Task<MetricEntity?> GetByIdAsync(int id, CancellationToken ct = default)
            => await _db.Metrics.SingleOrDefaultAsync(m => m.Id == id, ct);

        public async Task<MetricEntity?> GetByKeyAsync(string tenantId, string name, DateTime from, DateTime to, CancellationToken ct = default)
            => await _db.Metrics.SingleOrDefaultAsync(m =>
                    m.TenantId == tenantId
                    && m.Name == name
                    && m.From == from
                    && m.To == to, ct);

        public async Task<IEnumerable<MetricEntity>> ListAsync(string tenantId, string? name = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
        {
            var q = _db.Metrics.AsQueryable();
            q = q.Where(m => m.TenantId == tenantId);
            if (!string.IsNullOrEmpty(name)) q = q.Where(m => m.Name == name);
            if (from.HasValue) q = q.Where(m => m.From >= from.Value);
            if (to.HasValue) q = q.Where(m => m.To <= to.Value);
            return await q.OrderByDescending(m => m.CreatedAt).ToListAsync(ct);
        }

        public async Task UpdateAsync(MetricEntity entity, CancellationToken ct = default)
        {
            _db.Metrics.Update(entity);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("Concurrency conflict updating metric");
            }
        }

        public async Task DeleteAsync(MetricEntity entity, CancellationToken ct = default)
        {
            _db.Metrics.Remove(entity);
            await _db.SaveChangesAsync(ct);
        }
    }

    // Domain exceptions used here
    public class ConflictException : Exception { public ConflictException(string m) : base(m) { } }
    public class NotFoundException : Exception { public NotFoundException(string m) : base(m) { } }
}
