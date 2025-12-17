using AutoMapper;
using FluentAssertions;
using LMS.Application.DTOs.Books;
using LMS.Application.RepositoryInterfaces;
using LMS.Application.ServiceInterfaces;
using LMS.Application.Services;
using LMS.Domain.Entities;
using Moq;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.UnitTests
{

  
    public class CreateBook_Test
    {

        [Fact]
        public async Task CreateBookAsync()
        {
            var repoMock = new Mock<IBookRepository>();
            var mapperMock = new Mock<IMapper>();
            var cacheMock = new Mock<ICacheService>();

            var createDto = new CreateBookDTO
            {
                Title = "Programming with Robert",
                Author = "Robert C. Martin"
            };

            var book = new Book { Id = 1 };

            mapperMock.Setup(m => m.Map<Book>(createDto))
                      .Returns(book);

            mapperMock.Setup(m => m.Map<BookDTO>(It.IsAny<Book>()))
                     .Returns(new BookDTO { Id = 1, Title = "Programming with Robert" });

            var service = new BookService(
               repoMock.Object,
               mapperMock.Object,
               cacheMock.Object
           );

         
            var result = await service.CreateBookAsync(createDto, "user-123");

           
            result.Should().NotBeNull();
            result.Title.Should().Be("Programming with Robert");

            repoMock.Verify(r => r.AddAsync(It.Is<Book>(b => b.UserId == "user-123")), Times.Once);
            repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
