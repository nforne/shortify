using System;
using System.Collections.Generic;
using System.Linq;

namespace Shortify.DTOs.GroupDTOs
{
    public class AttrGroupRecordDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Key { get; set; } = null!;
        public object? Value { get; set; }
        public string Status { get; set; } = "active"; // active | inactive
        public Dictionary<string, string>? Meta { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ExpiryDate { get; set; } // optional expiry
    }

    /// <summary>
    /// In-memory container for attribute-group metadata (not persisted directly).
    /// Serialize to JSON and store into GroupEntity.MetadataJson.
    /// Includes action members for single and bulk attribute operations.
    /// </summary>
    public class AttrGroupMData
    {
        public string Mode { get; set; } = "attributes";
        public int Version { get; set; } = 1;
        public List<AttrGroupRecordDto> Attributes { get; set; } = new();

        public IReadOnlyList<AttrGroupRecordDto> GetAllAttributes() => Attributes;

        public AttrGroupRecordDto? GetAttribute(string attrId)
            => Attributes.FirstOrDefault(a => string.Equals(a.Id, attrId, StringComparison.Ordinal));

        public AttrGroupRecordDto? GetByKey(string key)
            => Attributes.FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase));

        public int CountActiveAttributes()
            => Attributes.Count(a => string.Equals(a.Status, "active", StringComparison.OrdinalIgnoreCase)
                                     && (a.ExpiryDate == null || a.ExpiryDate > DateTime.UtcNow));

        public AttrGroupRecordDto AddAttribute(AttrGroupRecordDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (string.IsNullOrWhiteSpace(dto.Id)) dto.Id = Guid.NewGuid().ToString();
            if (Attributes.Any(a => string.Equals(a.Id, dto.Id, StringComparison.Ordinal)))
                throw new InvalidOperationException($"Attribute id {dto.Id} already exists");
            if (!string.IsNullOrWhiteSpace(dto.Key) && Attributes.Any(a => string.Equals(a.Key, dto.Key, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Attribute key {dto.Key} already exists");

            dto.CreatedAt = DateTime.UtcNow;
            dto.UpdatedAt = null;
            dto.Status ??= "active";
            Attributes.Add(dto);
            Version++;
            return dto;
        }

        public bool RemoveAttribute(string attrId)
        {
            var a = GetAttribute(attrId);
            if (a == null) return false;
            Attributes.Remove(a);
            Version++;
            return true;
        }

        public bool DeactivateAttribute(string attrId)
        {
            var a = GetAttribute(attrId);
            if (a == null) return false;
            if (string.Equals(a.Status, "inactive", StringComparison.OrdinalIgnoreCase)) return false;
            a.Status = "inactive";
            a.UpdatedAt = DateTime.UtcNow;
            Version++;
            return true;
        }

        public AttrGroupRecordDto UpdateAttribute(string attrId, Action<AttrGroupRecordDto> apply)
        {
            if (apply == null) throw new ArgumentNullException(nameof(apply));
            var a = GetAttribute(attrId) ?? throw new KeyNotFoundException("Attribute not found");
            apply(a);
            a.UpdatedAt = DateTime.UtcNow;
            Version++;
            return a;
        }

        public int BulkAddAttributes(IEnumerable<AttrGroupRecordDto> incoming)
        {
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));
            var list = incoming.ToList();
            var dupIncoming = list.GroupBy(x => x.Id).FirstOrDefault(g => g.Count() > 1)?.Key;
            if (dupIncoming != null) throw new InvalidOperationException($"Duplicate attribute id in payload: {dupIncoming}");

            var keyCollision = list.Select(x => x.Key).Intersect(Attributes.Select(a => a.Key), StringComparer.OrdinalIgnoreCase).FirstOrDefault();
            if (keyCollision != null) throw new InvalidOperationException($"Attribute key collision with existing attribute: {keyCollision}");

            foreach (var dto in list)
            {
                if (string.IsNullOrWhiteSpace(dto.Id)) dto.Id = Guid.NewGuid().ToString();
                dto.CreatedAt = DateTime.UtcNow;
                dto.UpdatedAt = null;
                dto.Status ??= "active";
                Attributes.Add(dto);
            }

            Version++;
            return list.Count;
        }

        public int BulkDeactivateAttributes(IEnumerable<string> attrIds)
        {
            if (attrIds == null) throw new ArgumentNullException(nameof(attrIds));
            var ids = new HashSet<string>(attrIds);
            var changed = 0;
            foreach (var a in Attributes.Where(x => ids.Contains(x.Id)))
            {
                if (!string.Equals(a.Status, "inactive", StringComparison.OrdinalIgnoreCase))
                {
                    a.Status = "inactive";
                    a.UpdatedAt = DateTime.UtcNow;
                    changed++;
                }
            }

            if (changed > 0) Version++;
            return changed;
        }

        public int DeactivateAllAttributes()
        {
            var changed = 0;
            foreach (var a in Attributes)
            {
                if (!string.Equals(a.Status, "inactive", StringComparison.OrdinalIgnoreCase))
                {
                    a.Status = "inactive";
                    a.UpdatedAt = DateTime.UtcNow;
                    changed++;
                }
            }

            if (changed > 0) Version++;
            return changed;
        }

        public int ReplaceAttributes(IEnumerable<AttrGroupRecordDto> newAttrs)
        {
            var prev = Attributes.Count;
            Attributes = newAttrs?.ToList() ?? new List<AttrGroupRecordDto>();
            Version++;
            return prev;
        }
    }
}
