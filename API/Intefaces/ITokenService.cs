using API.Entities;

namespace API.Interaces;

public interface ITokenService
{
    string CreateToken(AppUser user);
}
