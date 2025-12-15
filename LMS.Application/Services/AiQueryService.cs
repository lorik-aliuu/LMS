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
        private readonly ICacheService _cacheService;
        private readonly IUserService _userService;

        private const int RATE_LIMIT_MAX_CALLS = 4; 
        private static readonly TimeSpan RATE_LIMIT_WINDOW = TimeSpan.FromMinutes(1);

        private static readonly TimeSpan CACHE_TTL_FAST = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan CACHE_TTL_NORMAL = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan CACHE_OPERATION_TIMEOUT = TimeSpan.FromMilliseconds(500);

    



        public AiQueryService(
                IBookRepository bookRepository,
                IOpenAiService openAIService,
                ILogger<AiQueryService> logger,
                 ICacheService cacheService,
                IUserService userService)
            {
                _bookRepository = bookRepository;
                _openAIService = openAIService;
                _logger = logger;
            _cacheService = cacheService;
            _userService = userService;
            }

        public async Task<AiQueryResponseDTO> ProcessQueryAsync(string query, string userId, bool isAdmin)
        {
            try
            {
                if (!await CanExecuteQueryAsync(userId))
                {
                    return new AiQueryResponseDTO
                    {
                        Success = false,
                        ErrorMessage = "Rate limit exceeded. Try again later.",
                        Answer = "You have reached the maximum number of queries allowed per minute."
                    };
                }

                var queryHash = Convert.ToBase64String(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(query)));

                var cacheKey = $"aiquery:{userId}:{queryHash}";

                var cached = await TryGetFromCacheAsync<AiQueryResponseDTO>(cacheKey);
                if (cached != null)
                {
                    _logger.LogInformation("Cache hit for query");
                    return cached;
                }

                var context = isAdmin
                    ? "User is an admin and can query all books."
                    : $"User can only query their own books (UserId: {userId}).";

                var aiResponse = await _openAIService.AnalyzeQueryAsync(query, context);
                var intent = ParseAIResponse(aiResponse);

                var result = await ExecuteQueryAsync(intent, userId, isAdmin);

                var dataJson = JsonSerializer.Serialize(result.Data);
                var answer = await _openAIService.GenerateAnswerAsync(query, dataJson);

                var response = new AiQueryResponseDTO
                {
                    Success = true,
                    Answer = answer,
                    InterpretedQuery = intent.QueryType,
                    Data = result.Data,
                    ChartType = result.ChartType
                };


                var ttl = intent.QueryType switch
                {
                    "USER_STATISTICS" => CACHE_TTL_FAST,
                    "CURRENTLY_READING" => CACHE_TTL_FAST,
                    _ => CACHE_TTL_NORMAL
                };

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await TrySetCacheAsync(cacheKey, response, ttl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "cache write failed");
                    }
                });

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI query failed");
                return new AiQueryResponseDTO
                {
                    Success = false,
                    Answer = "An error occurred while processing your request.",
                    ErrorMessage = "AI processing error"
                };
            }
        }

        private async Task<T?> TryGetFromCacheAsync<T>(string key) where T : class
        {
            try
            {
                using var cts = new CancellationTokenSource(CACHE_OPERATION_TIMEOUT);
                var cacheTask = _cacheService.GetAsync<T>(key);
                var completedTask = await Task.WhenAny(cacheTask, Task.Delay(CACHE_OPERATION_TIMEOUT, cts.Token));

                if (completedTask == cacheTask)
                {
                    cts.Cancel(); 
                    return await cacheTask;
                }

                _logger.LogWarning("Cache read timed out", CACHE_OPERATION_TIMEOUT.TotalMilliseconds);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache read failed");
                return null;
            }
        }

        private async Task TrySetCacheAsync<T>(string key, T value, TimeSpan ttl) where T : class
        {
            try
            {
                using var cts = new CancellationTokenSource(CACHE_OPERATION_TIMEOUT);
                var cacheTask = _cacheService.SetAsync(key, value, ttl);
                var completedTask = await Task.WhenAny(cacheTask, Task.Delay(CACHE_OPERATION_TIMEOUT, cts.Token));

                if (completedTask != cacheTask)
                {
                    _logger.LogWarning("Cache write timed out", CACHE_OPERATION_TIMEOUT.TotalMilliseconds);
                }
                else
                {
                    cts.Cancel();
                    await cacheTask;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache write failed");
            }
        }


        private async Task<bool> CanExecuteQueryAsync(string userId)
        {
            try
            {
                using var cts = new CancellationTokenSource(CACHE_OPERATION_TIMEOUT);
                var key = $"rate:{userId}";
                var incrementTask = _cacheService.IncrementAsync(key, RATE_LIMIT_WINDOW);
                var completedTask = await Task.WhenAny(incrementTask, Task.Delay(CACHE_OPERATION_TIMEOUT, cts.Token));

                if (completedTask == incrementTask)
                {
                    cts.Cancel();
                    var count = await incrementTask;
                    return count <= RATE_LIMIT_MAX_CALLS;
                }

                _logger.LogWarning("Rate limit check timed out, allowing request");
                return true; 
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Rate limit check failed, allowing request");
                return true; 
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

                    return intent ?? throw new ValidationException("Couldnt parse");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing  : {Response}", aiResponse);
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
                    "MY_BOOK_COUNT" => await GetBookCountForUserAsync(userId),
                    "CURRENTLY_READING" => await GetCurrentlyReadingBooksAsync(userId, isAdmin),
                    "COMMON_GENRE" => await GetMostCommonGenreAsync(userId, isAdmin),
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

        private async Task<QueryResult> GetMostCommonGenreAsync(string userId, bool isAdmin)
        {
            var books = isAdmin
                ? await _bookRepository.GetAllBooksForAdminAsync()
                : await _bookRepository.GetBooksByUserIdAsync(userId);

            if (!books.Any())
            {
                return new QueryResult
                {
                    Data = new List<Dictionary<string, object>>(),
                    ChartType = "single"
                };
            }

            var mostCommonGenre = books
                .GroupBy(b => b.Genre)
                .OrderByDescending(g => g.Count())
                .First().Key;

            var result = new List<Dictionary<string, object>>
    {
        new()
        {
            ["metric"] = "Most Common Genre",
            ["value"] = mostCommonGenre
        }
    };

            return new QueryResult
            {
                Data = result,
                ChartType = "single"
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

        private async Task<QueryResult> GetCurrentlyReadingBooksAsync(string userId, bool isAdmin)
        {
            var books = isAdmin
                ? (await _bookRepository.GetAllBooksForAdminAsync())
                    .Where(b => b.ReadingStatus == ReadingStatus.Completed)
                : await _bookRepository.GetBooksByStatusAsync(userId, ReadingStatus.Reading);

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


        private async Task<QueryResult> GetBookCountForUserAsync(string userId)
        {
            var count = await _bookRepository.GetBookCountByUserAsync(userId);

            var result = new List<Dictionary<string, object>>
    {
        new()
        {
            ["metric"] = "Total Books",
            ["value"] = count
        }
    };

            return new QueryResult
            {
                Data = result,
                ChartType = "single" 
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
