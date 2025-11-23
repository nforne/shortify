using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shortify.Models;

namespace Shortify.Repositories.Interfaces
{
    public interface IAttributeRepository
    {
        Task<AttributeEntity> AddAsync(AttributeEntity entity, CancellationToken ct = default);
        Task<AttributeEntity?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<AttributeEntity?> GetByKeyAsync(string tenantId, string key, CancellationToken ct = default);
        Task<IEnumerable<AttributeEntity>> ListAsync(string tenantId, bool includePrivate = false, CancellationToken ct = default);
        Task UpdateAsync(AttributeEntity entity, CancellationToken ct = default);
        Task SoftDeleteAsync(AttributeEntity entity, CancellationToken ct = default);
        Task HardDeleteAsync(AttributeEntity entity, CancellationToken ct = default);
    }
}
