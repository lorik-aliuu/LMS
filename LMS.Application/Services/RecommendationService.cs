using LMS.Application.DTOs.Recommendation;
using LMS.Application.RepositoryInterfaces;
using LMS.Application.ServiceInterfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class RecommendationService : IRecommendationService
{
    private readonly IBookRepository _bookRepository;
    private readonly IOpenAiService _openAIService;
    private readonly ILogger<RecommendationService> _logger;
    private readonly ICacheService _redisCache;

    private const string DismissedPrefix = "dismissed_recommendations:";
    private readonly TimeSpan DismissTime = TimeSpan.FromMinutes(30);

    public RecommendationService(
        IBookRepository bookRepository,
        IOpenAiService openAIService,
        ILogger<RecommendationService> logger,
        ICacheService redisCache)
    {
        _bookRepository = bookRepository;
        _openAIService = openAIService;
        _logger = logger;
        _redisCache = redisCache;
    }

    public async Task<BookRecommendationResponseDTO> GetRecommendationsAsync(
        string userId,
        RecommendationRequestDTO request)
    {
        try
        {
            var userBooks = (await _bookRepository.GetBooksByUserIdAsync(userId)).ToList();

            if (!userBooks.Any())
            {
                return new BookRecommendationResponseDTO
                {
                    Success = true,
                    Message = "You dont have any books yet. Here are some popular recommendations:",
                    Recommendations = GetPopularBooks(request.Count),
                    RecommendationType = "Popular",
                    Timestamp = DateTime.UtcNow
                };
            }

         
            var dismissedJson = await _redisCache.GetAsync<string>($"{DismissedPrefix}{userId}");
            var dismissed = string.IsNullOrEmpty(dismissedJson)
                ? new HashSet<string>()
                : JsonSerializer.Deserialize<HashSet<string>>(dismissedJson) ?? new HashSet<string>();

            return await GetAIRecommendationsAsync(userBooks, dismissed, request.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate recommendations for user {UserId}", userId);

            return new BookRecommendationResponseDTO
            {
                Success = false,
                Message = "Unable to generate recommendations at this time.",
                Timestamp = DateTime.UtcNow
            };
        }
    }

    private async Task<BookRecommendationResponseDTO> GetAIRecommendationsAsync(
        List<Book> userBooks,
        HashSet<string> dismissed,
        int count)
    {
        var genreDistribution = userBooks
            .GroupBy(b => b.Genre)
            .Select(g => $"{g.Key}: {g.Count()} books")
            .ToList();

        var completedBooks = userBooks
            .Where(b => b.ReadingStatus == ReadingStatus.Completed)
            .Select(b => b.Title)
            .Take(10);

        var prompt = $@"
User reading history analysis:
- Genres owned: {string.Join(", ", genreDistribution)}
- Completed books: {string.Join(", ", completedBooks)}

Recommend {count} books the user has NOT read.
Mix genres proportionally based on the user's reading habits.
Do NOT repeat any owned books or dismissed books:
{string.Join(", ", dismissed)}

Return ONLY valid JSON in this format:
{{
  ""recommendations"": [
    {{
      ""title"": """",
      ""author"": """",
      ""genre"": """",
      ""estimatedPrice"": 0,
      ""reason"": """"
    }}
  ]
}}";

        var aiResponse = await _openAIService.AnalyzeQueryAsync(
            prompt,
            "You are a smart book recommendation engine."
        );

        var recommendations = ParseAIResponse(aiResponse)
            .Where(r => !dismissed.Contains(r.Title)) 
            .Take(count)
            .ToList();

        return new BookRecommendationResponseDTO
        {
            Success = true,
            Message = "Here are personalized recommendations based on your reading history:",
            Recommendations = recommendations,
            RecommendationType = "AI-Generated",
            Timestamp = DateTime.UtcNow
        };
    }

    private List<BookRecommendationDTO> ParseAIResponse(string aiResponse)
    {
        try
        {
            var cleaned = aiResponse
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            var parsed = JsonSerializer.Deserialize<AIResponse>(cleaned,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return parsed?.Recommendations ?? new List<BookRecommendationDTO>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse AI response: {Response}", aiResponse);
            return new List<BookRecommendationDTO>();
        }
    }

    private List<BookRecommendationDTO> GetPopularBooks(int count)
    {
        var popularBooks = new List<BookRecommendationDTO>
        {
            new() { Title = "1984", Author = "George Orwell", Genre = "Dystopian", EstimatedPrice = 15.99m, Reason = "A universally acclaimed classic." },
            new() { Title = "The Hobbit", Author = "J.R.R. Tolkien", Genre = "Fantasy", EstimatedPrice = 19.99m, Reason = "A timeless fantasy adventure." },
            new() { Title = "To Kill a Mockingbird", Author = "Harper Lee", Genre = "Classic", EstimatedPrice = 14.99m, Reason = "A beloved literary masterpiece." },
            new() { Title = "Dune", Author = "Frank Herbert", Genre = "Science Fiction", EstimatedPrice = 18.99m, Reason = "One of the most influential sci-fi novels ever written." }
        };

        return popularBooks.Take(count).ToList();
    }

    private class AIResponse
    {
        public List<BookRecommendationDTO> Recommendations { get; set; } = new();
    }

    public async Task<ActionRecommendationResponseDTO> SaveRecommendedBookAsync(string userId, SaveRecommendationRequestDTO request)
    {
        var book = new Book
        {
            UserId = userId,
            Title = request.Title,
            Author = request.Author,
            Genre = request.Genre,
            Price = request.EstimatedPrice,
            ReadingStatus = ReadingStatus.NotStarted,
            CreatedAt = DateTime.UtcNow
        };

        await _bookRepository.AddAsync(book);
        await _bookRepository.SaveChangesAsync();

        return new ActionRecommendationResponseDTO
        {
            Success = true,
            Message = "Book added to your library."
        };
    }

    public async Task<ActionRecommendationResponseDTO> DismissRecommendedBookAsync(string userId, DismissRecommendationDTO request)
    {
        var key = $"{DismissedPrefix}{userId}";

        
        var dismissedJson = await _redisCache.GetAsync<string>(key);
        var dismissed = string.IsNullOrEmpty(dismissedJson)
            ? new HashSet<string>()
            : JsonSerializer.Deserialize<HashSet<string>>(dismissedJson) ?? new HashSet<string>();

        dismissed.Add(request.Title);

       
        await _redisCache.SetAsync(key, JsonSerializer.Serialize(dismissed), DismissTime);

        return new ActionRecommendationResponseDTO
        {
            Success = true,
            Message = "Book dismissed "
        };
    }
}
