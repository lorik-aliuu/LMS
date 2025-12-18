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
        private readonly IUserService _userService;

        private static readonly TimeSpan CACHE_OPERATION_TIMEOUT = TimeSpan.FromMilliseconds(500);

        public BookService(IBookRepository bookRepository, IMapper mapper, ICacheService cacheService, IUserService userService)
        {
            _bookRepository = bookRepository;
            _mapper = mapper;
            _cacheService = cacheService;
           _userService = userService;
        }

        public async Task<BookDTO> CreateBookAsync(CreateBookDTO createBookDto, string userId)
        {
            var book = _mapper.Map<Book>(createBookDto);
            book.UserId = userId;
            book.CreatedAt = DateTime.UtcNow;

            await _bookRepository.AddAsync(book);
            await _bookRepository.SaveChangesAsync();


           InvalidateUserCacheAsync(userId);

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

            InvalidateUserCacheAsync(userId);


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

            InvalidateUserCacheAsync(userId);
        }

        public async Task<IEnumerable<BookListDTO>> SearchUserBooksAsync(string userId, string search)
        {
            var books = await _bookRepository.SearchBooksAsync(userId, search);
            return _mapper.Map<IEnumerable<BookListDTO>>(books);
        }

        public async Task<IEnumerable<BookListDTO>> GetAllBooksForAdminAsync()
        {
            var books = await _bookRepository.GetAllBooksForAdminAsync(); 
            var users = await _userService.GetAllUsersAsync();            

          
            var booksWithOwners = books.Select(b =>
            {
                var user = users.FirstOrDefault(u => u.UserId == b.UserId);
                return new BookListDTO
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    ReadingStatus = b.ReadingStatus,
                    UserId = b.UserId,
                    UserName = user?.UserName ?? "Unknown",
                    Rating = b.Rating,
                    Genre = b.Genre
                };
            });

            return booksWithOwners;
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
                InvalidateUserCacheAsync(book.UserId);

            _ = Task.Run(async () =>
            {
                try
                {
                    await TryRemoveCacheAsync("aiquery:admin:*");
                }
                catch (Exception ex)
                {
                   throw new Exception("Cache invalidation failed", ex);
                }
            });

        }

        private void InvalidateUserCacheAsync(string userId)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var tasks = new[]
                    {
                    TryRemoveByPatternAsync($"aiquery:{userId}:*"),
                    TryRemoveByPatternAsync("aiquery:admin:*"),
                };

                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                  
                    Console.WriteLine($"Background cache invalidation failed: {ex.Message}");
                }
            });
        }


        private async Task TryRemoveCacheAsync(string key)
        {
            try
            {
                using var cts = new CancellationTokenSource(CACHE_OPERATION_TIMEOUT);
                var removeTask = _cacheService.RemoveAsync(key);
                var completedTask = await Task.WhenAny(removeTask, Task.Delay(CACHE_OPERATION_TIMEOUT, cts.Token));

                if (completedTask != removeTask)
                {
                  
                    Console.WriteLine($"Cache remove timed out for key: {key}");
                }
                else
                {
                    cts.Cancel();
                    await removeTask;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cache remove failed for key {key}: {ex.Message}");
            }
        }

        private async Task TryRemoveByPatternAsync(string pattern)
        {
            try
            {
                using var cts = new CancellationTokenSource(CACHE_OPERATION_TIMEOUT);
                var removeTask = _cacheService.RemoveByPatternAsync(pattern);
                var completedTask = await Task.WhenAny(removeTask, Task.Delay(CACHE_OPERATION_TIMEOUT, cts.Token));

                if (completedTask != removeTask)
                {
                    Console.WriteLine($"Cache remove by pattern timed out for pattern: {pattern}");
                }
                else
                {
                    cts.Cancel();
                    await removeTask;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cache remove by pattern failed for {pattern}: {ex.Message}");
            }
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

       
    }
}
