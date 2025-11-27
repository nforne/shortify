using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shortify.DTOs.AttrDTOs;

namespace Shortify.Services.Interfaces
{
    public interface IAttributeService
    {
        Task<AttributeDto> CreateAsync(CreateAttributeDto dto, CancellationToken ct = default);
        Task<AttributeDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<AttributeDto>> ListAsync(string tenantId, bool includePrivate = false, CancellationToken ct = default);
        Task<IEnumerable<AttributeDto>> ListAsync(bool includePrivate = false, CancellationToken ct = default);
        Task UpdateAsync(int id, UpdateAttributeDto dto, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
        Task ToggleVisibilityAsync(int id, string visibility, CancellationToken ct = default);
    }
}
