using AutoMapper;
using LMS.Application.DTOs.Books;
using LMS.Application.RepositoryInterfaces;
using LMS.Application.ServiceInterfaces;
using LMS.Domain.Entities;
using LMS.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.Services
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _bookRepository;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public BookService(IBookRepository bookRepository, IMapper mapper, ICacheService cacheService)
        {
            _bookRepository = bookRepository;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<BookDTO> CreateBookAsync(CreateBookDTO createBookDto, string userId)
        {
            var book = _mapper.Map<Book>(createBookDto);
            book.UserId = userId;
            book.CreatedAt = DateTime.UtcNow;

            await _bookRepository.AddAsync(book);
            await _bookRepository.SaveChangesAsync();


            await InvalidateUserCache(userId);

            return _mapper.Map<BookDTO>(book);
        }

        public async Task<BookDTO> GetBookByIdAsync(int bookId, string userId)
        {
            var book = await _bookRepository.GetByIdAsync(bookId);

            if (book == null)
            {
                throw new NotFoundException(nameof(Book), bookId);
            }


            if (book.UserId != userId)
            {
                throw new UnauthorizedException("You dont have permission to view this book");
            }

            return _mapper.Map<BookDTO>(book);
        }

        public async Task<IEnumerable<BookListDTO>> GetUserBooksAsync(string userId)
        {
            var books = await _bookRepository.GetBooksByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<BookListDTO>>(books);
        }

        public async Task<BookDTO> UpdateBookAsync(int bookId, UpdateBookDTO updateBookDto, string userId)
        {

            var book = await _bookRepository.GetByIdAsync(bookId);

            if (book == null)
            {
                throw new NotFoundException(nameof(Book), bookId);
            }


            if (book.UserId != userId)
            {
                throw new UnauthorizedException("You dont have permission to update this book");
            }

            _mapper.Map(updateBookDto, book);


            book.UpdatedAt = DateTime.UtcNow;

            await _bookRepository.UpdateAsync(book);
            await _bookRepository.SaveChangesAsync();

            await InvalidateUserCache(userId);


            return _mapper.Map<BookDTO>(book);

        }

        public async Task DeleteBookAsync(int bookId, string userId)
        {
            var book = await _bookRepository.GetByIdAsync(bookId);

            if (book == null)
            {
                throw new NotFoundException(nameof(Book), bookId);
            }


            if (book.UserId != userId)
            {
                throw new UnauthorizedException("You dont have permission to delete this book");
            }

            await _bookRepository.DeleteAsync(book);
            await _bookRepository.SaveChangesAsync();

            await InvalidateUserCache(userId);
        }

        public async Task<IEnumerable<BookListDTO>> SearchUserBooksAsync(string userId, string search)
        {
            var books = await _bookRepository.SearchBooksAsync(userId, search);
            return _mapper.Map<IEnumerable<BookListDTO>>(books);
        }

        public async Task<IEnumerable<BookListDTO>> GetAllBooksForAdminAsync()
        {
            var books = await _bookRepository.GetAllBooksForAdminAsync();
            return _mapper.Map<IEnumerable<BookListDTO>>(books);
        }

        public async Task<BookDTO> GetAnyBookByIdAsync(int bookId)
        {
            var book = await _bookRepository.GetByIdAsync(bookId);

            if (book == null)
            {
                throw new NotFoundException(nameof(Book), bookId);
            }

            return _mapper.Map<BookDTO>(book);
        }

        public async Task DeleteAnyBookAsync(int bookId)
        {
            var book = await _bookRepository.GetByIdAsync(bookId);

            if (book == null)
            {
                throw new NotFoundException(nameof(Book), bookId);
            }

            await _bookRepository.DeleteAsync(book);
            await _bookRepository.SaveChangesAsync();

            if (!string.IsNullOrEmpty(book.UserId))
                await InvalidateUserCache(book.UserId);

            await _cacheService.RemoveAsync("aiquery:admin:*");
        }

        public async Task<int> GetTotalBooksCountAsync()
        {
            var books = await _bookRepository.GetAllAsync();
            return books.Count();
        }

        public async Task<int> GetUserBooksCountAsync(string userId)
        {
            return await _bookRepository.GetBookCountByUserAsync(userId);
        }

        private async Task InvalidateUserCache(string userId)
        {

            await _cacheService.RemoveByPatternAsync($"aiquery:{userId}:*");


            await _cacheService.RemoveByPatternAsync("aiquery:admin:*");

            await _cacheService.RemoveAsync($"insights:user:{userId}");
            await _cacheService.RemoveAsync("insights:library");



        }
    }
}
