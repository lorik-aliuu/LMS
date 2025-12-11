    using LMS.Application.DTOs.AI;
    using LMS.Application.RepositoryInterfaces;
    using LMS.Application.ServiceInterfaces;
    using LMS.Domain.Enums;
    using LMS.Domain.Exceptions;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    namespace LMS.Application.Services
    {
        public class AiQueryService : IAiQueryService
        {
            private readonly IBookRepository _bookRepository;
            private readonly IOpenAiService _openAIService;
            private readonly ILogger<AiQueryService> _logger;
            private readonly IUserService _userService;


            public AiQueryService(
                IBookRepository bookRepository,
                IOpenAiService openAIService,
                ILogger<AiQueryService> logger,
                IUserService userService)
            {
                _bookRepository = bookRepository;
                _openAIService = openAIService;
                _logger = logger;
                _userService = userService;
            }

            public async Task<AiQueryResponseDTO> ProcessQueryAsync(string query, string userId, bool isAdmin)
            {
                try
                {
                    var context = isAdmin
                        ? "User is an admin and can query all books across all users."
                        : $"User can only query their own books (UserId: {userId}).";

                    var aiResponse = await _openAIService.AnalyzeQueryAsync(query, context);
                    var queryIntent = ParseAIResponse(aiResponse);
                    var result = await ExecuteQueryAsync(queryIntent, userId, isAdmin);

                    var dataJson = JsonSerializer.Serialize(result.Data);
                    var answer = await _openAIService.GenerateAnswerAsync(query, dataJson);

               
                    if (queryIntent.QueryType == "USER_STATISTICS")
                    {
                        if (result.Data.Count == 0 || result.Data.All(d => d.ContainsKey("value") && (int)d["value"] == 0))
                        {
                            answer = "You aren't reading any books.";
                        }
                    }

                    return new AiQueryResponseDTO
                    {
                        Success = true,
                        Answer = answer,
                        InterpretedQuery = queryIntent.QueryType,
                        Data = result.Data,
                        ChartType = result.ChartType
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing AI query: {Query}", query);

                    return new AiQueryResponseDTO
                    {
                        Success = false,
                        ErrorMessage = "I'm sorry, I couldn't process your query. Please try rephrasing it.",
                        Answer = "An error occurred while processing your request."
                    };
                }
            }

            private QueryIntent ParseAIResponse(string aiResponse)
            {
                try
                {
                    var cleaned = aiResponse
                        .Replace("```json", "")
                        .Replace("```", "")
                        .Trim();

                    var intent = JsonSerializer.Deserialize<QueryIntent>(cleaned, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return intent ?? throw new ValidationException("Could not parse AI response");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing AI response: {Response}", aiResponse);
                    throw new ValidationException("Could not understand the query");
                }
            }

            private async Task<QueryResult> ExecuteQueryAsync(QueryIntent intent, string userId, bool isAdmin)
            {
                return intent.QueryType?.ToUpper() switch
                {
                    "USER_WITH_MOST_BOOKS" => await GetUserWithMostBooksAsync(isAdmin),
                    "MOST_POPULAR_BOOK" => await GetMostPopularBookAsync(isAdmin),
                    "EXPENSIVE_BOOKS" => await GetMostExpensiveBooksAsync(intent.Parameters?.Limit ?? 5, userId, isAdmin),
                    "BOOKS_BY_GENRE" => await GetBooksByGenreAsync(intent.Parameters?.Genre ?? "", userId, isAdmin),
                    "BOOKS_BY_STATUS" => await GetBooksByStatusAsync(intent.Parameters?.Status ?? "", userId, isAdmin),
                    "USER_STATISTICS" => await GetUserStatisticsAsync(userId, isAdmin),
                    "GENERAL_STATISTICS" => await GetGeneralStatisticsAsync(isAdmin),
                    _ => throw new ValidationException("Query type not supported")
                };
            }

            private async Task<QueryResult> GetUserWithMostBooksAsync(bool isAdmin)
            {
                if (!isAdmin)
                    throw new UnauthorizedException("Only admins can see all users' book counts");

                var allBooks = await _bookRepository.GetAllBooksForAdminAsync();

                var grouped = allBooks.GroupBy(b => b.UserId)
                                      .OrderByDescending(g => g.Count())
                                      .Take(10);

                var userGroups = new List<Dictionary<string, object>>();

                foreach (var g in grouped)
                {
                    var user = await _userService.GetUserByIdAsync(g.Key);
                    userGroups.Add(new Dictionary<string, object>
                    {
                        ["userId"] = g.Key,
                        ["userName"] = user?.UserName ?? "Unknown",
                        ["bookCount"] = g.Count()
                    });
                }

                return new QueryResult
                {
                    Data = userGroups,
                    ChartType = "bar"
                };
            }


            private async Task<QueryResult> GetMostPopularBookAsync(bool isAdmin)
            {
                if (!isAdmin)
                    throw new UnauthorizedException("Only admins can see most popular books across all users");

                var allBooks = await _bookRepository.GetAllBooksForAdminAsync();
                var bookGroups = allBooks
                .GroupBy(b => new { b.Title, b.Author })
               .Select(g => new Dictionary<string, object>
                {
                    ["title"] = g.Key.Title,
                    ["author"] = g.Key.Author,
                    ["ownedBy"] = g.Count(b => b.ReadingStatus == ReadingStatus.Reading || b.ReadingStatus == ReadingStatus.Completed)
                })
        .OrderByDescending(x => (int)x["ownedBy"])
        .Take(10)
        .ToList();


                return new QueryResult
                {
                    Data = bookGroups,
                    ChartType = "bar"
                };
            }

            private async Task<QueryResult> GetMostExpensiveBooksAsync(int limit, string userId, bool isAdmin)
            {
                var books = isAdmin
                    ? await _bookRepository.GetAllBooksForAdminAsync()
                    : await _bookRepository.GetBooksByUserIdAsync(userId);

                var expensiveBooks = books
                    .OrderByDescending(b => b.Price)
                    .Take(limit)
                    .Select(b => new Dictionary<string, object>
                    {
                        ["id"] = b.Id,
                        ["title"] = b.Title,
                        ["author"] = b.Author,
                        ["price"] = b.Price,
                        ["genre"] = b.Genre
                    })
                    .ToList();

                return new QueryResult
                {
                    Data = expensiveBooks,
                    ChartType = "table"
                };
            }

            private async Task<QueryResult> GetBooksByGenreAsync(string genre, string userId, bool isAdmin)
            {
                var books = isAdmin
                    ? await _bookRepository.GetAllBooksForAdminAsync()
                    : await _bookRepository.GetBooksByUserIdAsync(userId);

                var filteredBooks = books
                    .Where(b => b.Genre.Contains(genre, StringComparison.OrdinalIgnoreCase))
                    .Select(b => new Dictionary<string, object>
                    {
                        ["title"] = b.Title,
                        ["author"] = b.Author,
                        ["genre"] = b.Genre,
                        ["price"] = b.Price,
                        ["status"] = b.ReadingStatus.ToString()
                    })
                    .ToList();

                return new QueryResult
                {
                    Data = filteredBooks,
                    ChartType = "table"
                };
            }

                private async Task<QueryResult> GetBooksByStatusAsync(string status, string userId, bool isAdmin)
                {
                    if (!Enum.TryParse<ReadingStatus>(status, true, out var readingStatus))
                    {
                        throw new ValidationException($"Invalid reading status: {status}");
                    }

                    var books = isAdmin
                        ? (await _bookRepository.GetAllBooksForAdminAsync()).Where(b => b.ReadingStatus == readingStatus)
                        : await _bookRepository.GetBooksByStatusAsync(userId, readingStatus);

                    var bookList = books.Select(b => new Dictionary<string, object>
                    {
                        ["title"] = b.Title,
                        ["author"] = b.Author,
                        ["genre"] = b.Genre,
                        ["status"] = b.ReadingStatus.ToString()
                    }).ToList();

                    return new QueryResult
                    {
                        Data = bookList,
                        ChartType = "table"
                    };
                }

            private async Task<QueryResult> GetUserStatisticsAsync(string userId, bool isAdmin)
            {
                var books = await _bookRepository.GetBooksByUserIdAsync(userId);
                var booksList = books.ToList();

                var stats = new List<Dictionary<string, object>>();

                if (!booksList.Any())
                {
                   
                    return new QueryResult
                    {
                        Data = stats,
                        ChartType = "table"
                    };
                }

                stats = new List<Dictionary<string, object>>
                {
                    new() { ["metric"] = "Total Books", ["value"] = booksList.Count },
                    new() { ["metric"] = "Books Reading", ["value"] = booksList.Count(b => b.ReadingStatus == ReadingStatus.Reading) },
                    new() { ["metric"] = "Books Completed", ["value"] = booksList.Count(b => b.ReadingStatus == ReadingStatus.Completed) },
                    new() { ["metric"] = "Books Not Started", ["value"] = booksList.Count(b => b.ReadingStatus == ReadingStatus.NotStarted) },
                    new() { ["metric"] = "Average Book Price", ["value"] = booksList.Any() ? Math.Round(booksList.Average(b => b.Price), 2) : 0 },
                    new() { ["metric"] = "Most Common Genre", ["value"] = booksList.GroupBy(b => b.Genre).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? "N/A" }
                };

                return new QueryResult
                {
                    Data = stats,
                    ChartType = "table"
                };
            }

            private async Task<QueryResult> GetGeneralStatisticsAsync(bool isAdmin)
            {
                if (!isAdmin)
                    throw new UnauthorizedException("Only admins can see general statistics");

                var allBooks = (await _bookRepository.GetAllBooksForAdminAsync()).ToList();

                var stats = new List<Dictionary<string, object>>
                {
                    new() { ["metric"] = "Total Books", ["value"] = allBooks.Count },
                    new() { ["metric"] = "Total Users with Books", ["value"] = allBooks.Select(b => b.UserId).Distinct().Count() },
                    new() { ["metric"] = "Average Books per User", ["value"] = allBooks.Any() ? Math.Round((double)allBooks.Count / allBooks.Select(b => b.UserId).Distinct().Count(), 2) : 0 },
                    new() { ["metric"] = "Most Expensive Book", ["value"] = allBooks.Any() ? allBooks.Max(b => b.Price) : 0 },
                    new() { ["metric"] = "Average Book Price", ["value"] = allBooks.Any() ? Math.Round(allBooks.Average(b => b.Price), 2) : 0 },
                    new() { ["metric"] = "Most Popular Genre", ["value"] = allBooks.GroupBy(b => b.Genre).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key ?? "N/A" }
                };

                return new QueryResult
                {
                    Data = stats,
                    ChartType = "table"
                };
            }

            private class QueryIntent
            {
                public string QueryType { get; set; } = string.Empty;
                public QueryParameters? Parameters { get; set; }
            }

            private class QueryParameters
            {
                public int? Limit { get; set; }
                public string? Genre { get; set; }
                public string? Status { get; set; }
            }

            private class QueryResult
            {
                public List<Dictionary<string, object>> Data { get; set; } = new();
                public string? ChartType { get; set; }
            }
        }
    }
