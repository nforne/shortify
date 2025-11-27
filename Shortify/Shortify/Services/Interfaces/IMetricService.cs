using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shortify.DTOs.MetricDTOs;

namespace Shortify.Services.Interfaces
{
    public interface IMetricService
    {
        // add this overload alongside existing signatures
        Task<MetricDto> GenerateAsync(CreateJsonMetricDto dto, CancellationToken ct = default);
        Task<MetricDto> GenerateAsync(CreateMetricDto dto, CancellationToken ct = default);       
        Task<MetricDto?> GetByIdAsync(int id, CancellationToken ct = default);
        //Task<IEnumerable<MetricDto>> ListAsync(string tenantId, string? name = null, CancellationToken ct = default);
        Task<IEnumerable<MetricDto>> ListAsync(string? name = null, CancellationToken ct = default);
        Task<MetricDto?> ReplaceAsync(int id, CreateMetricDto dto, CancellationToken ct = default);
        Task PatchAsync(int id, object patch, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
        Task<byte[]?> DownloadAsync(int id, CancellationToken ct = default);

        // Shortify.Core.Contracts.IMetricService
        Task PatchAsync(int id, MetricPatchDto patch, CancellationToken ct = default);

        // Replace entire metric; if returns null it means a new resource was created (caller can return 201).
        Task<MetricDto?> ReplaceAsync(int id, CreateJsonMetricDto dto, CancellationToken ct = default);


    }
}
