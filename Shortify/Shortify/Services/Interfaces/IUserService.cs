using Shortify.DTOs.UserDTOs;

namespace Shortify.Services.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllAsync(string tenantId, int page = 1, int pageSize = 50);
        Task<IEnumerable<UserDto>> GetAllAsync(int page = 1, int pageSize = 50);
        Task<UserDto?> GetByIdAsync(int id);
        Task<UserDto> CreateAsync(CreateUserDto dto);
        Task<UserDto?> ReplaceAsync(int id, UserDto dto);
        Task<UserDto?> PatchAsync(int id, PatchUserDto dto);
        Task DeleteAsync(int id, bool soft = true);
    }
}
