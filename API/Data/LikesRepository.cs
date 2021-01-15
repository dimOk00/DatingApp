using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class LikesRepository : ILikesRepository
    {
        private readonly DataContext _context;

        public LikesRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<UserLike> GetUserLike(int sourceUserId, int likedUserId)
        {
            return await _context.Likes.FindAsync(sourceUserId, likedUserId);
        }

        private async Task<IEnumerable<UserLike>> GetUserLikes(AppUser user)
        {
            var likes = _context.Likes.AsQueryable();

            likes = likes.Where(like => like.SourceUserId == user.Id || like.LikedUserId == user.Id);

            return await likes.ToListAsync();
        }

        public async Task<bool> HasLikes(AppUser user)
        {
            var likes = _context.Likes.AsQueryable();

            likes = likes.Where(like => like.SourceUserId == user.Id || like.LikedUserId == user.Id);

            return await likes.AnyAsync();
        }

        public async Task DeleteLikes(AppUser user)
        {
            var likes = await GetUserLikes(user);
            _context.Likes.RemoveRange(likes);
        }

        public async Task<PagedList<LikeDto>> GetUserLikes(LikesParams likesParams)
        {
            var users = _context.Users.Include(p => p.Photos).OrderBy(u => u.UserName).AsQueryable();
            var likes = _context.Likes.AsQueryable();

            switch (likesParams.Predicate)
            {
                case "liked":
                    likes = likes.Where(like => like.SourceUserId == likesParams.UserId);
                    users = likes.Select(like => like.LikedUser);
                    break;
                case "likedBy":
                    likes = likes.Where(like => like.LikedUserId == likesParams.UserId);
                    users = likes.Select(like => like.SourceUser);
                    break;
            }

            var likedUsers = users.Select(user => new LikeDto
            {
                Username = user.UserName,
                KnownAs = user.KnownAs,
                Age = user.DateOfBirth.CalculateAge(),
                PhotoUrl = user.Photos.FirstOrDefault(p => p.IsMain).Url,
                City = user.City,
                Id = user.Id
            });

            return await PagedList<LikeDto>.CreateAsync(likedUsers, likesParams.PageNumber, likesParams.PageSize);
        }

        public async Task<AppUser> GetUserWithLikes(int userId)
        {
            return await _context.Users
                .Include(x => x.LikedUsers)
                .FirstOrDefaultAsync(user => user.Id == userId);
        }

        public void RemoveConnection(Connection connection)
        {
            _context.Connections.Remove(connection);
        }
    }
}