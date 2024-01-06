using System.Security.Cryptography;
using System.Text;
using API.Controllers;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController : BaseApiController
{
    private readonly DataContext _context;
    private readonly ITokenService _tokenService;

    public AccountController(DataContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        if (await UserExists(registerDto.UserName))
        {
            return BadRequest("Username is taken");
        }

        using var hmac = new HMACSHA512();

        var user = new AppUser
        {
            UserName = registerDto.UserName.ToLower(),
            PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
            PasswordSalt = hmac.Key
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return new UserDto
        {
            Username = user.UserName,
            Token = _tokenService.CreateToken(user)
        };
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await _context.Users.SingleOrDefaultAsync(x => x.UserName == loginDto.Username);

        if (user == null)
        {
            return Unauthorized("Invalid usename");
        }

        using var hmac = new HMACSHA512(user.PasswordSalt);

        var computeHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

        for (int i = 0; i < computeHash.Length; i++)
        {
            if (computeHash[i] != user.PasswordHash[i])
            {
                return Unauthorized("Invalid password");
            }
        }

        return new UserDto
        {
            Username = user.UserName,
            Token = _tokenService.CreateToken(user)
        };
    }

    private async Task<bool> UserExists(string username)
    {
        return await _context.Users.AnyAsync(x => x.UserName.ToLower() == username.ToLower());
    }
}