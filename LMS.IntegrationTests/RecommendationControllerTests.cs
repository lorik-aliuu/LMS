using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LMS.Application.DTOs.Recommendation;
using LMS.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LMS.IntegrationTests
{
    public class RecommendationControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public RecommendationControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();

          
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
                Title = "The Great Gatsby",
                Author = "F. Scott Fitzgerald",
                Genre = "Classic",
                Price = 10.99m,
                UserId = "test-user-id"
            });

            db.SaveChanges();
        }

        [Fact]
        public async Task GetRecommendations_Returns_Success_With_Requested_Count()
        {
            
            var request = new RecommendationRequestDTO
            {
                Count = 3
            };

          
            var response = await _client.PostAsJsonAsync("/api/recommendation", request);

            
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

          
            if (result.ValueKind == JsonValueKind.Array)
            {
                result.GetArrayLength().Should().BeLessThanOrEqualTo(3);
            }
          
            else if (result.TryGetProperty("data", out var data))
            {
                data.GetArrayLength().Should().BeLessThanOrEqualTo(3);
            }
        }

        [Fact]
        public async Task DismissRecommendation_Returns_Ok_With_Valid_Payload()
        {
           
            var request = new DismissRecommendationDTO
            {
                Title = "The Great Gatsby",
                Author = "F. Scott Fitzgerald"
            };

          
            var response = await _client.PostAsJsonAsync("/api/recommendation/dismiss", request);

           
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

          
            if (result.ValueKind == JsonValueKind.True || result.ValueKind == JsonValueKind.False)
            {
                result.GetBoolean().Should().BeTrue();
            }
        }
    }
}