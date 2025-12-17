using AutoMapper;
using FluentAssertions;
using LMS.Application.Services;
using LMS.Domain.Entities;
using LMS.Domain.Exceptions;
using Moq;
using LMS.Application.RepositoryInterfaces;
using LMS.Application.ServiceInterfaces;
using Xunit;

namespace LMS.UnitTests
{
    public class GetBookById_Test
    {
        [Fact]
        public async Task GetBookByIdAsync()
        {
          
            var repoMock = new Mock<IBookRepository>();
            var mapperMock = new Mock<IMapper>();
            var cacheMock = new Mock<ICacheService>();

            var book = new Book
            {
                Id = 10,
                UserId = "owner-user"
            };

            repoMock.Setup(r => r.GetByIdAsync(10))
                    .ReturnsAsync(book);

            var service = new BookService(
                repoMock.Object,
                mapperMock.Object,
                cacheMock.Object
            );

          
            Func<Task> act = async () =>
                await service.GetBookByIdAsync(10, "other-user");

         
            await act.Should().ThrowAsync<UnauthorizedException>();
        }
    }
}
