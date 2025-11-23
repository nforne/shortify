using System;
using System.Collections.Generic;
using System.Linq;

namespace Shortify.DTOs.GroupDTOs
{
    public class UserGroupMemberDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SubjectId { get; set; } = null!;
        public string Role { get; set; } = "member";
        public string Status { get; set; } = "active"; // active | inactive
        public Dictionary<string, string>? Meta { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// In-memory container for user-group metadata.
    /// Not persisted directly; caller serializes this instance to JSON and stores into Group.MetadataJson.
    /// Provides action members for common group-space operations (single and bulk).
    /// </summary>
    public class UserGroupMData
    {
        public string Mode { get; set; } = "members";
        public int Version { get; set; } = 1;
        public List<UserGroupMemberDto> Members { get; set; } = new();

        // Read helpers
        public IReadOnlyList<UserGroupMemberDto> GetAllMembers() => Members;

        public UserGroupMemberDto? GetMember(string memberId)
            => Members.FirstOrDefault(m => string.Equals(m.Id, memberId, StringComparison.Ordinal));

        public UserGroupMemberDto? GetBySubjectId(string subjectId)
            => Members.FirstOrDefault(m => string.Equals(m.SubjectId, subjectId, StringComparison.OrdinalIgnoreCase));

        public int CountActiveMembers()
            => Members.Count(m => string.Equals(m.Status, "active", StringComparison.OrdinalIgnoreCase));

        // Single-item mutators

        /// <summary>
        /// Adds a member. Throws InvalidOperationException if a member with same Id already exists.
        /// If dto.Id is null/empty a new GUID will be assigned.
        /// Returns the added member instance (the same object stored in Members).
        /// </summary>
        public UserGroupMemberDto AddMember(UserGroupMemberDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Id)) dto.Id = Guid.NewGuid().ToString();

            if (Members.Any(m => string.Equals(m.Id, dto.Id, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Member with id {dto.Id} already exists");

            dto.CreatedAt = DateTime.UtcNow;
            dto.UpdatedAt = null;
            dto.Status = string.IsNullOrWhiteSpace(dto.Status) ? "active" : dto.Status;
            Members.Add(dto);
            Version++;
            return dto;
        }

        /// <summary>
        /// Removes member by id. Returns true if removed, false if not found.
        /// </summary>
        public bool RemoveMember(string memberId)
        {
            var m = GetMember(memberId);
            if (m == null) return false;
            Members.Remove(m);
            Version++;
            return true;
        }

        /// <summary>
        /// Deactivates a member (sets Status=inactive). Returns true if changed.
        /// </summary>
        public bool DeactivateMember(string memberId)
        {
            var m = GetMember(memberId);
            if (m == null) return false;
            if (string.Equals(m.Status, "inactive", StringComparison.OrdinalIgnoreCase)) return false;
            m.Status = "inactive";
            m.UpdatedAt = DateTime.UtcNow;
            Version++;
            return true;
        }

        /// <summary>
        /// Update fields on a member; only non-null properties in the update action should be applied.
        /// </summary>
        public UserGroupMemberDto UpdateMember(string memberId, Action<UserGroupMemberDto> apply)
        {
            if (apply == null) throw new ArgumentNullException(nameof(apply));
            var m = GetMember(memberId) ?? throw new KeyNotFoundException("Member not found");
            apply(m);
            m.UpdatedAt = DateTime.UtcNow;
            Version++;
            return m;
        }

        // Bulk operations

        /// <summary>
        /// Adds multiple members atomically in-memory. Throws InvalidOperationException if any duplicate ids found (either within incoming or existing).
        /// Returns number added.
        /// </summary>
        public int BulkAddMembers(IEnumerable<UserGroupMemberDto> incoming)
        {
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));
            var incomingList = incoming.ToList();
            // check duplicates within incoming
            var dupIncoming = incomingList.GroupBy(x => x.Id).Where(g => g.Key != null && g.Count() > 1).Select(g => g.Key).FirstOrDefault();
            if (dupIncoming != null) throw new InvalidOperationException($"Duplicate member id in payload: {dupIncoming}");

            // check collisions with existing
            var collision = incomingList.Select(x => x.Id).Intersect(Members.Select(m => m.Id)).FirstOrDefault();
            if (collision != null) throw new InvalidOperationException($"Member id collision with existing member: {collision}");

            foreach (var dto in incomingList)
            {
                if (string.IsNullOrWhiteSpace(dto.Id)) dto.Id = Guid.NewGuid().ToString();
                dto.CreatedAt = DateTime.UtcNow;
                dto.UpdatedAt = null;
                dto.Status = string.IsNullOrWhiteSpace(dto.Status) ? "active" : dto.Status;
                Members.Add(dto);
            }

            Version++;
            return incomingList.Count;
        }

        /// <summary>
        /// Bulk deactivate: sets Status=inactive for provided ids. Returns count deactivated.
        /// </summary>
        public int BulkDeactivateMembers(IEnumerable<string> memberIds)
        {
            if (memberIds == null) throw new ArgumentNullException(nameof(memberIds));
            var ids = new HashSet<string>(memberIds);
            var changed = 0;
            foreach (var m in Members.Where(x => ids.Contains(x.Id)))
            {
                if (!string.Equals(m.Status, "inactive", StringComparison.OrdinalIgnoreCase))
                {
                    m.Status = "inactive";
                    m.UpdatedAt = DateTime.UtcNow;
                    changed++;
                }
            }

            if (changed > 0) Version++;
            return changed;
        }

        /// <summary>
        /// Deactivate all members. Returns number deactivated.
        /// </summary>
        public int DeactivateAllMembers()
        {
            var changed = 0;
            foreach (var m in Members)
            {
                if (!string.Equals(m.Status, "inactive", StringComparison.OrdinalIgnoreCase))
                {
                    m.Status = "inactive";
                    m.UpdatedAt = DateTime.UtcNow;
                    changed++;
                }
            }

            if (changed > 0) Version++;
            return changed;
        }

        // Replace/clear

        /// <summary>
        /// Replace entire members collection (caller must validate). Returns previous count.
        /// </summary>
        public int ReplaceMembers(IEnumerable<UserGroupMemberDto> newMembers)
        {
            var prev = Members.Count;
            Members = newMembers?.ToList() ?? new List<UserGroupMemberDto>();
            Version++;
            return prev;
        }
    }
}
