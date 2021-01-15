using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;

        public UserRepository(DataContext context, IMapper mapper, IPhotoService photoService)
        {
            _mapper = mapper;
            _photoService = photoService;
            _context = context;
        }

        public async Task<MemberDto> GetMemberAsync(string username)
        {
            return await _context.Users
                .Where(x => x.UserName == username)
                .ProjectTo<MemberDto>(_mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();
        }

        public async Task<PagedList<MemberDto>> GetMembersAsync(UserParams userParams)
        {
            var query = _context.Users.AsQueryable();

            query = query.Where(x => x.UserName != userParams.CurrentUserName);
            query = query.Where(x => x.Gender == userParams.Gender);

            var minDob = DateTime.Today.AddYears(-userParams.MaxAge - 1);
            var maxDob = DateTime.Today.AddYears(-userParams.MinAge);

            query = query.Where(x => x.DateOfBirth >= minDob && x.DateOfBirth <= maxDob);

            query = userParams.OrderBy switch
            {
                "created" => query.OrderByDescending(u => u.Created),
                _ => query.OrderByDescending(u => u.LastActive)
            };

            var source = query.ProjectTo<MemberDto>(_mapper.ConfigurationProvider).AsNoTracking();
            return await PagedList<MemberDto>.CreateAsync(source, userParams.PageNumber, userParams.PageSize);
        }

        public async Task<AppUser> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<AppUser> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(x => x.Photos)
                .SingleOrDefaultAsync(x => x.UserName == username);
        }

        public async Task<string> GetUserGender(string username)
        {
            return await _context.Users
                .Where(x => x.UserName == username)
                .Select(x => x.Gender).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<AppUser>> GetUsersAsync()
        {
            return await _context.Users
               .Include(x => x.Photos)
               .ToListAsync();
        }

        public void Update(AppUser user)
        {
            _context.Entry(user).State = EntityState.Modified;
        }

        public async Task DeletePhotos(AppUser appUser)
        {
            var userPhotos = await _context.Users.Include(x => x.Photos)
                                       .Select(x => new { Username = x.UserName, x.Photos })
                                       .FirstOrDefaultAsync(x => x.Username == appUser.UserName);

            var photos = userPhotos?.Photos;

            if (photos == null || photos.Count == 0)
                return;

            foreach (var photo in photos)
            {
                if (photo.PublicId == null)
                    continue;
                // TODO: Try to think what to do if photo was not deleted
                await _photoService.DeletePhotoAsync(photo.PublicId);
            }

            _context.Photos.RemoveRange(photos);
        }
    }
}