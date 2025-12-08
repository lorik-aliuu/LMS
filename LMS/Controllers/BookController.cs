using LMS.Application.DTOs.Books;
using LMS.Application.ServiceInterfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BookController : ControllerBase
    {
        private readonly IBookService _bookService;

        public BookController(IBookService bookService)
        {
            _bookService = bookService;
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found");
        }

       
        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }



        [HttpGet("my-books")]
        public async Task<IActionResult> GetMyBooks()
        {
            var userId = GetUserId();
            var books = await _bookService.GetUserBooksAsync(userId);

            return Ok(new
            {
                success = true,
                data = books
            });
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookById(int id)
        {
            var userId = GetUserId();
            var book = await _bookService.GetBookByIdAsync(id, userId);

            return Ok(new
            {
                success = true,
                data = book
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateBook([FromBody] CreateBookDTO createBookDto)
        {
            var userId = GetUserId();
            var book = await _bookService.CreateBookAsync(createBookDto, userId);

            return CreatedAtAction(
                nameof(GetBookById),
                new { id = book.Id },
                new
                {
                    success = true,
                    message = "Book created successfully",
                    data = book
                });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBook(int id, [FromBody] UpdateBookDTO updateBookDto)
        {
           

            var userId = GetUserId();
            var book = await _bookService.UpdateBookAsync(id, updateBookDto, userId);

            return Ok(new
            {
                success = true,
                message = "Book updated ",
                data = book
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var userId = GetUserId();
            await _bookService.DeleteBookAsync(id, userId);

            return Ok(new
            {
                success = true,
                message = "Book deleted"
            });
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchBooks([FromQuery] string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Search string is required"
                });
            }

            var userId = GetUserId();
            var books = await _bookService.SearchUserBooksAsync(userId, search);

            return Ok(new
            {
                success = true,
                data = books
            });
        }

        [HttpGet("my-books/count")]
        public async Task<IActionResult> GetMyBooksCount()
        {
            var userId = GetUserId();
            var count = await _bookService.GetUserBooksCountAsync(userId);

            return Ok(new
            {
                success = true,
                count
            });
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllBooks()
        {
            var books = await _bookService.GetAllBooksForAdminAsync();

            return Ok(new
            {
                success = true,
                data = books
            });
        }

        [HttpGet("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAnyBookById(int id)
        {
            var book = await _bookService.GetAnyBookByIdAsync(id);

            return Ok(new
            {
                success = true,
                data = book
            });
        }

        [HttpDelete("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAnyBook(int id)
        {
            await _bookService.DeleteAnyBookAsync(id);

            return Ok(new
            {
                success = true,
                message = "Book deleted successfully"
            });
        }

        [HttpGet("admin/count")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetTotalBooksCount()
        {
            var count = await _bookService.GetTotalBooksCountAsync();

            return Ok(new
            {
                success = true,
                totalBooks = count
            });
        }


    }
}
