using LMS.Domain.Entities;
using LMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.RepositoryInterfaces
{
    public interface IBookRepository : IRepository<Book>
    {
        Task<IEnumerable<Book>> GetBooksByUserIdAsync(string userId);

        Task<IEnumerable<Book>> GetBooksByGenreAsync(string userId, string genre);

        Task<IEnumerable<Book>> GetBooksByStatusAsync(string userId, ReadingStatus status);

        Task<IEnumerable<Book>> SearchBooksAsync(string userId, string search);

        Task<IEnumerable<Book>> GetAllBooksForAdminAsync();

        Task<bool> IsBookOwnedByUserAsync(int bookId, string userId);

        Task<int> GetBookCountByUserAsync(string userId);




    }
}
