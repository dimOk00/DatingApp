using System.Threading.Tasks;
using API.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Storage;

namespace API.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly IPhotoService _photoService;

        private IDbContextTransaction _contextTransaction;

        public UnitOfWork(DataContext context, IMapper mapper, IPhotoService photoService)
        {
            _context = context;
            _mapper = mapper;
            _photoService = photoService;
        }

        public IUserRepository UserRepository => new UserRepository(_context, _mapper, _photoService);

        public IMessageRepository MessageRepository => new MessageRepository(_context, _mapper);

        public ILikesRepository LikesRepository => new LikesRepository(_context);

        public async Task<bool> Complete()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public bool HasChanges()
        {
            return _context.ChangeTracker.HasChanges();
        }

        public void BeginTransaction()
        {
            _contextTransaction = _context.Database.BeginTransaction();
        }

        public void CommitTransaction()
        {
            if (_contextTransaction == null)
                return;

            _contextTransaction.Commit();
            _contextTransaction.Dispose();
        }
    }
}