using Microsoft.AspNetCore.Http;
using Shortify.DTOs.ResolveDTOs;
using System.Threading;
using System.Threading.Tasks;

namespace Shortify.Services.Interfaces
{
    public interface IResolveService
    {
        Task<ResolveResponseDto> ResolveAsync(int accountRootUserPk, int attrId, HttpRequest request, string returnFmt = "json", CancellationToken ct = default);
    }
}
