using LMS.Application.RepositoryInterfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using LMS.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Infrastructure.Repositories
{
    public class BookRepository : Repository<Book>, IBookRepository
    {
        public BookRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Book>> GetBooksByUserIdAsync(string userId)
        {
            return await _dbSet
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetBooksByGenreAsync(string userId, string genre)
        {
            return await _dbSet
                .Where(b => b.UserId == userId && b.Genre == genre)
                .OrderBy(b => b.Title)
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetBooksByStatusAsync(string userId, ReadingStatus status)
        {
            return await _dbSet
                .Where(b => b.UserId == userId && b.ReadingStatus == status)
                .OrderByDescending(b => b.UpdatedAt ?? b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> SearchBooksAsync(string userId, string searchTerm)
        {
            var lowerSearchTerm = searchTerm.ToLower();

            return await _dbSet
                .Where(b => b.UserId == userId &&
                       (b.Title.ToLower().Contains(lowerSearchTerm) ||
                        b.Author.ToLower().Contains(lowerSearchTerm)))
                .OrderBy(b => b.Title)
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetAllBooksForAdminAsync()
        {
            return await _dbSet
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> IsBookOwnedByUserAsync(int bookId, string userId)
        {
            return await _dbSet
                .AnyAsync(b => b.Id == bookId && b.UserId == userId);
        }

        public async Task<int> GetBookCountByUserAsync(string userId)
        {
            return await _dbSet
                .CountAsync(b => b.UserId == userId);
        }




    }
}
