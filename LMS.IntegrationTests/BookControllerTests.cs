using FluentAssertions;
using LMS.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LMS.IntegrationTests
{
    public class BookControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public BookControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;

          
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

           
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

          
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            db.Books.Add(new LMS.Domain.Entities.Book
            {
                Title = "Test Book",
                Author = "Author",
                Genre = "Fiction",
                Price = 9.99m,
                UserId = "test-user-id" 
            });
            db.SaveChanges();
        }

        [Fact]
        public async Task GET_my_books_returns_200_And_Data()
        {
           
            var response = await _client.GetAsync("/api/book/my-books");

         

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

            var dataArray = result.GetProperty("data");
            dataArray.GetArrayLength().Should().Be(1);
        }

        [Fact]
        public async Task GET_my_books_count_returns_1()
        {
          
            var response = await _client.GetAsync("/api/book/my-books/count");

          
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

            result.GetProperty("count").GetInt32().Should().Be(1);
        }
    }
}