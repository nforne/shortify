using System.Security.Claims;
using System.Text.Json;
using Shortify.DTOs.UserDTOs;
using Shortify.Services;

namespace Shortify.RegExtention
{

    public static class ClaimsPrincipalExtensions
    {
        public static T? GetJsonClaim<T>(this ClaimsPrincipal? principal, string claimType)
        {
            if (principal == null) return default;
            var json = principal.FindFirst(claimType)?.Value;
            if (string.IsNullOrWhiteSpace(json)) return default;
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }      

    }

}

// examples :
//var userDto = HttpContext.User.GetJsonClaim<UserDto>("user");

/***************OR***************/

namespace Shortify.RegExtention
{
    public class SomeService
    {
        private readonly IAuthContext _authContext;
        public SomeService(IAuthContext authContext) => _authContext = authContext;

        public UserDto? GetCurrentUserDto()
        {
            return _authContext.User.GetJsonClaim<UserDto>("user");
        }
    }
}


