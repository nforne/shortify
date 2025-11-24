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
    public class ResolveEventRepository : IResolveEventRepository
    {
        private readonly ShortifyDbContext _db;

        public ResolveEventRepository(ShortifyDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task AddAsync(ResolveEventEntity evt, CancellationToken ct = default)
        {
            if (evt == null) throw new ArgumentNullException(nameof(evt));

            await _db.ResolveEvents.AddAsync(evt, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<IEnumerable<ResolveEventEntity>> ListByAttributeAsync(int attributeId, int limit = 50, CancellationToken ct = default)
        {
            limit = Math.Max(1, Math.Min(limit, 1000));
            return await _db.ResolveEvents
                .AsNoTracking()
                .Where(e => e.AttributeId == attributeId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(limit)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<ResolveEventEntity>> ListByAccountRootAsync(int accountRootUserPk, int limit = 50, CancellationToken ct = default)
        {
            limit = Math.Max(1, Math.Min(limit, 2000));
            return await _db.ResolveEvents
                .AsNoTracking()
                .Where(e => e.AccountRootUserPk == accountRootUserPk)
                .OrderByDescending(e => e.CreatedAt)
                .Take(limit)
                .ToListAsync(ct);
        }
    }
}
