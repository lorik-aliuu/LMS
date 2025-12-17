using FluentAssertions;
using LMS.Application.DTOs.Users;
using LMS.Application.RepositoryInterfaces;
using LMS.Application.ServiceInterfaces;
using LMS.Application.Services;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.UnitTests
{
    public class LibraryInsightsTests
    {
        private readonly Mock<IBookRepository> _bookRepoMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IOpenAiService> _openAiMock;
        private readonly Mock<ILogger<LibraryInsightsService>> _loggerMock;

        private readonly LibraryInsightsService _service;

        public LibraryInsightsTests()
        {
            _bookRepoMock = new Mock<IBookRepository>();
            _userServiceMock = new Mock<IUserService>();
            _openAiMock = new Mock<IOpenAiService>();
            _loggerMock = new Mock<ILogger<LibraryInsightsService>>();

            _service = new LibraryInsightsService(
                _bookRepoMock.Object,
                _userServiceMock.Object,
                _openAiMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task GenerateLibraryInsightsAsync_ReturnsEmptyInsights()
        {
           
            _bookRepoMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Book>());

            _userServiceMock
                .Setup(u => u.GetAllUsersAsync())
                .ReturnsAsync(new List<UserProfileDTO>());

            _openAiMock
                .Setup(a => a.GenerateAnswerAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("AI insights unavailable");

          
            var result = await _service.GenerateLibraryInsightsAsync(null);

           
            result.Should().NotBeNull();
            result.Insights.Should().BeEmpty();
            result.Statistics.TotalBooks.Should().Be(0);
            result.Summary.Should().Be("AI insights unavailable");
        }

        [Fact]
        public async Task SummarizeUserReadingHabitsAsync()
        {
           
            var userId = "user1";
            var user = new UserProfileDTO { UserId = userId, UserName = "Test User" };

            _userServiceMock
                .Setup(u => u.GetUserByIdAsync(userId))
                .ReturnsAsync(user);

            var books = new List<Book>
        {
            new Book { Title = "Book 1", ReadingStatus = ReadingStatus.Completed },
            new Book { Title = "Book 2", ReadingStatus = ReadingStatus.Reading },
            new Book { Title = "Book 3", ReadingStatus = ReadingStatus.Completed }
        };

            _bookRepoMock
                .Setup(r => r.GetBooksByUserIdAsync(userId))
                .ReturnsAsync(books);

            _openAiMock
                .Setup(a => a.GenerateAnswerAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("AI summary");

           
            var result = await _service.SummarizeUserReadingHabitsAsync(userId);

           
            result.Should().NotBeNull();
            result.TotalBooks.Should().Be(3);
            result.CompletedBooks.Should().Be(2);
            result.BooksInProgress.Should().Be(1);
            result.PreferredGenres.Should().BeEmpty(); 
            result.Summary.Should().Be("AI summary");
        }
    }
}
