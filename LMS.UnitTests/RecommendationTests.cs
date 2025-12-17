using FluentAssertions;
using LMS.Application.DTOs.Recommendation;
using LMS.Application.RepositoryInterfaces;
using LMS.Application.ServiceInterfaces;
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
    public class RecommendationTests
    {
        private readonly Mock<IBookRepository> _bookRepoMock;
        private readonly Mock<IOpenAiService> _openAiMock;
        private readonly Mock<ICacheService> _cacheMock;
        private readonly Mock<ILogger<RecommendationService>> _loggerMock;

        private readonly RecommendationService _service;

        public RecommendationTests()
        {
            _bookRepoMock = new Mock<IBookRepository>();
            _openAiMock = new Mock<IOpenAiService>();
            _cacheMock = new Mock<ICacheService>();
            _loggerMock = new Mock<ILogger<RecommendationService>>();

            _service = new RecommendationService(
                _bookRepoMock.Object,
                _openAiMock.Object,
                _loggerMock.Object,
                _cacheMock.Object
            );
        }

        [Fact]
        public async Task GetRecommendationsAsync_WhenUserHasNoBooks()
        {
         
            _bookRepoMock
                .Setup(r => r.GetBooksByUserIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<Book>());

            var request = new RecommendationRequestDTO
            {
                Count = 2
            };

            
            var result = await _service.GetRecommendationsAsync("user-1", request);

           
            result.Success.Should().BeTrue();
            result.RecommendationType.Should().Be("Popular");
            result.Recommendations.Should().HaveCount(2);
            result.Message.Should().Contain("popular");
        }

        
        [Fact]
        public async Task SaveRecommendedBookAsync_ValidRequest_AddsBookAndReturnsSuccess()
        {
           
            var request = new SaveRecommendationRequestDTO
            {
                Title = "Clean Code",
                Author = "Robert C. Martin",
                Genre = "Programming",
                EstimatedPrice = 25.99m
            };

            _bookRepoMock
                .Setup(r => r.AddAsync(It.IsAny<Book>()))
                .ReturnsAsync((Book b) => b);


            _bookRepoMock
           .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);



            var result = await _service.SaveRecommendedBookAsync("user-1", request);

            
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Book added to your library.");

            _bookRepoMock.Verify(
                r => r.AddAsync(It.Is<Book>(b =>
                    b.Title == request.Title &&
                    b.Author == request.Author &&
                    b.UserId == "user-1" &&
                    b.ReadingStatus == ReadingStatus.NotStarted
                )),
                Times.Once
            );

            _bookRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
