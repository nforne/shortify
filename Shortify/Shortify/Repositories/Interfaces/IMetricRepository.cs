using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shortify.Models;

namespace Shortify.Repositories.Interfaces
{
    public interface IMetricRepository
    {
        Task<MetricEntity> AddAsync(MetricEntity entity, CancellationToken ct = default);
        Task<MetricEntity?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<MetricEntity?> GetByKeyAsync(string tenantId, string name, DateTime from, DateTime to, CancellationToken ct = default);
        Task<IEnumerable<MetricEntity>> ListAsync(string tenantId, string? name = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
        Task UpdateAsync(MetricEntity entity, CancellationToken ct = default);
        Task DeleteAsync(MetricEntity entity, CancellationToken ct = default);
    }
}
