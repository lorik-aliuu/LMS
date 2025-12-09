using LMS.Application.DTOs.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.ServiceInterfaces
{
    public interface IBookService
    {
        Task<BookDTO> CreateBookAsync(CreateBookDTO createBookDto, string userId);
        Task<BookDTO> GetBookByIdAsync(int bookId, string userId);
        Task<IEnumerable<BookListDTO>> GetUserBooksAsync(string userId);
        Task<BookDTO> UpdateBookAsync(int bookId, UpdateBookDTO updateBookDto, string userId);
        Task DeleteBookAsync(int bookId, string userId);
        Task<IEnumerable<BookListDTO>> SearchUserBooksAsync(string userId, string searchTerm);

        //ADMIN
        Task<IEnumerable<BookListDTO>> GetAllBooksForAdminAsync();
        Task<BookDTO> GetAnyBookByIdAsync(int bookId);
        Task DeleteAnyBookAsync(int bookId); 
        Task<int> GetTotalBooksCountAsync();
        Task<int> GetUserBooksCountAsync(string userId);
    }
}
