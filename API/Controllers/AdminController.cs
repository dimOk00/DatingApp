using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AdminController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public AdminController(UserManager<AppUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("users-with-roles")]
        public async Task<ActionResult> GetUsersWithRoles()
        {
            var users = await _userManager.Users
                .Include(r => r.UserRoles)
                .ThenInclude(r => r.Role)
                .OrderBy(u => u.UserName)
                .Select(u => new
                {
                    u.Id,
                    Username = u.UserName,
                    Roles = u.UserRoles.Select(r => r.Role.Name).ToList()
                })
                .ToListAsync();

            return Ok(users);
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpPost("edit-roles/{username}")]
        public async Task<ActionResult> EditRoles(string username, [FromQuery] string roles)
        {
            var selectedRoles = roles?.Split(",").ToArray();

            if (selectedRoles == null || !selectedRoles.Any())
                return BadRequest("User mast have at least one role");

            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
                return NotFound("Could not find user");

            var userRoles = await _userManager.GetRolesAsync(user);

            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

            if (!result.Succeeded)
                return BadRequest("Failed to add to roles");

            result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

            if (!result.Succeeded)
                return BadRequest("Failed to remove from roles");

            return Ok(await _userManager.GetRolesAsync(user));
        }

        [Authorize(Policy = "ModeratePhotoRole")]
        [HttpGet("photos-to-moderate")]
        public ActionResult GetPhotosForModeration()
        {
            return Ok("Admins or moderators can see this");
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpDelete("{username}")]
        public async Task<ActionResult> DeleteUser(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return NotFound("User not found!");

            var rolesForUser = await _userManager.GetRolesAsync(user);

            if (user.Id == User.GetUserId())
                return BadRequest("You can not delete yourself!");

            if (rolesForUser.Any(x => x == "Admin"))
                return BadRequest("You can not delete admin!");

            var result = IdentityResult.Success;

            _unitOfWork.BeginTransaction();

            await _unitOfWork.LikesRepository.DeleteLikes(user);
            if (_unitOfWork.HasChanges() && !await _unitOfWork.Complete())
                result = IdentityResult.Failed();

            if (result.Succeeded)
                result = await DeleteMessages(user);

            if (result.Succeeded)
                result = await DeletePhoto(user);

            if (result.Succeeded)
                result = await RemoveLogins(user);

            if (result.Succeeded)
                result = await RemoveRoles(rolesForUser, user);

            if (result.Succeeded)
                result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
                return BadRequest("Failed to delete user");
            
            _unitOfWork.CommitTransaction();
            return Ok();
        }

        private async Task<IdentityResult> RemoveRoles(IList<string> rolesForUser, AppUser user)
        {
            foreach (var item in rolesForUser)
            {
                var result = await _userManager.RemoveFromRoleAsync(user, item);
                if (result != IdentityResult.Success)
                    return result;
            }

            return IdentityResult.Success;
        }

        private async Task<IdentityResult> RemoveLogins(AppUser user)
        {
            var logins = await _userManager.GetLoginsAsync(user);

            foreach (var login in logins)
            {
                var result = await _userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
                if (result != IdentityResult.Success)
                    return result;
            }

            return IdentityResult.Success;
        }

        private async Task<IdentityResult> DeletePhoto(AppUser user)
        {
            await _unitOfWork.UserRepository.DeletePhotos(user);
            if (_unitOfWork.HasChanges() && !await _unitOfWork.Complete())
                return IdentityResult.Failed();

            return IdentityResult.Success;
        }

        private async Task<IdentityResult> DeleteMessages(AppUser user)
        {
            await _unitOfWork.MessageRepository.DeleteMessagesAsync(user);
            if (_unitOfWork.HasChanges() && !await _unitOfWork.Complete())
                return IdentityResult.Failed();

            return IdentityResult.Success;
        }
    }
}