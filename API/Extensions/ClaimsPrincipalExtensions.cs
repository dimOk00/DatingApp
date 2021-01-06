using System.Security.Claims;

namespace API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetUsername(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Name)?.Value;
        }

        public static int GetUserId(this ClaimsPrincipal user)
        {
            if (int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var res))
                return res;

            return -1;
        }
    }
}