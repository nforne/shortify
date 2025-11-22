using Shortify.Models;

namespace Shortify.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<UserEntity>> GetAllAsync(string tenantId, int page, int pageSize, CancellationToken ct = default);
        Task<UserEntity?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<UserEntity?> GetByEmailAsync(string tenantId, string email, CancellationToken ct = default);
        Task<UserEntity> AddAsync(UserEntity entity, CancellationToken ct = default);
        Task UpdateAsync(UserEntity entity, CancellationToken ct = default);
        Task DeleteAsync(UserEntity entity, CancellationToken ct = default);
 
    }
}
