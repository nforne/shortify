using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shortify.DTOs.GroupDTOs;
using Shortify.Models;

namespace Shortify.Repositories.Interfaces
{
    public interface IGroupRepository
    {
        Task<IEnumerable<GroupEntity>> GetAllAsync(string tenantId, int page, int pageSize, CancellationToken ct);
        Task<GroupEntity?> GetByIdAsync(int id, CancellationToken ct);
        Task<GroupEntity?> GetByNameAsync(string tenantId, string name, CancellationToken ct);
        Task<GroupEntity> AddAsync(GroupEntity entity, CancellationToken ct);
        Task UpdateAsync(GroupEntity entity, CancellationToken ct);
        Task DeleteAsync(GroupEntity entity, CancellationToken ct);


        // User-group metadata helpers (repo-only surface)
        Task<UserGroupMData> LoadUserMetadataAsync(int groupId, CancellationToken ct = default);
        Task SaveUserMetadataAsync(int groupId, UserGroupMData meta, CancellationToken ct = default);
        Task Ugrp_ReplaceMetadataAsync(int groupId, UserGroupMData newMeta, CancellationToken ct = default);
        Task<int> Ugrp_CountActiveMembersAsync(int groupId, CancellationToken ct = default);
        Task Ugrp_ReplaceMetadataJsonAsync(int groupId, string metadataJson, CancellationToken ct = default);

        // Attribute-group metadata helpers (repo-only surface)
        Task<AttrGroupMData> LoadAttrMetadataAsync(int groupId, CancellationToken ct = default);
        Task SaveAttrMetadataAsync(int groupId, AttrGroupMData meta, CancellationToken ct = default);
        Task Agrp_ReplaceMetadataAsync(int groupId, AttrGroupMData newMeta, CancellationToken ct = default);
        Task<int> Agrp_CountActiveAttributesAsync(int groupId, CancellationToken ct = default);
        Task Agrp_ReplaceMetadataJsonAsync(int groupId, string metadataJson, CancellationToken ct = default);

    }
}
