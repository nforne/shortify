using System.Collections.Generic;
using System.Threading.Tasks;
using Shortify.DTOs.GroupDTOs;

namespace Shortify.Services.Interfaces
{
    public interface IGroupService
    {
        Task<IEnumerable<GroupDto>> GetAllAsync(string tenantId, int page = 1, int pageSize = 50);
        Task<IEnumerable<GroupDto>> GetAllAsync(int page = 1, int pageSize = 50);
        Task<GroupDto?> GetByIdAsync(int id);
        Task<GroupDto> CreateAsync(CreateGroupDto dto);
        Task<GroupDto?> ReplaceAsync(int id, GroupDto dto);
        Task<GroupDto?> PatchAsync(int id, PatchGroupDto dto);
        Task DeleteAsync(int id, bool soft = true);
    }
}
