using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shortify.Models;

namespace Shortify.Repositories.Interfaces
{
    public interface IResolveEventRepository
    {
        /// <summary>
        /// Persist a resolve event to the database.
        /// </summary>
        Task AddAsync(ResolveEventEntity evt, CancellationToken ct = default);

        /// <summary>
        /// Get recent resolve events for a given attribute (ordered by CreatedAt desc).
        /// Useful for diagnostics and tests.
        /// </summary>
        Task<IEnumerable<ResolveEventEntity>> ListByAttributeAsync(int attributeId, int limit = 50, CancellationToken ct = default);

        /// <summary>
        /// Get recent resolve events for a given account root user (ordered by CreatedAt desc).
        /// </summary>
        Task<IEnumerable<ResolveEventEntity>> ListByAccountRootAsync(int accountRootUserPk, int limit = 50, CancellationToken ct = default);
    }
}
