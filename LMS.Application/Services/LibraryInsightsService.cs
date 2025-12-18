using LMS.Application.DTOs.Insights;
using LMS.Application.DTOs.Users;
using LMS.Application.RepositoryInterfaces;
using LMS.Application.ServiceInterfaces;
using LMS.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Linq;

namespace LMS.Application.Services
{
    public class LibraryInsightsService : ILibraryInsightsService
    {
        private readonly IBookRepository _bookRepository;
        private readonly IUserService _userService;
        private readonly IOpenAiService _openAIService;
        private readonly ILogger<LibraryInsightsService> _logger;

        public LibraryInsightsService(
            IBookRepository bookRepository,
            IUserService userService,
            IOpenAiService openAIService,
            ILogger<LibraryInsightsService> logger)
        {
            _bookRepository = bookRepository;
            _userService = userService;
            _openAIService = openAIService;
            _logger = logger;
        }


        private static bool IsAdmin(UserProfileDTO user)
        {
            return Enum.TryParse<UserRole>(
                user.Role,
                ignoreCase: true,
                out var role
            ) && role == UserRole.Admin;
        }


        public async Task<LibraryInsightsDTO> GenerateLibraryInsightsAsync(string? scopedUserId)
        {
            try
            {
                if (scopedUserId != null)
                {
                    var scopedUser = await _userService.GetUserByIdAsync(scopedUserId);
                    if (scopedUser != null && IsAdmin(scopedUser))
                    {
                        return new LibraryInsightsDTO
                        {
                            Summary = "Admin users are excluded from reading insights.",
                            Insights = new(),
                            Statistics = new LibraryStatisticsDTO(),
                            GeneratedAt = DateTime.UtcNow
                        };
                    }
                }

                bool isUserScoped = scopedUserId != null;

                var books = isUserScoped
                    ? await _bookRepository.GetBooksByUserIdAsync(scopedUserId!)
                    : await _bookRepository.GetAllAsync();

                var users = (await _userService.GetAllUsersAsync())
                    .Where(u => !IsAdmin(u))
                    .ToList();

                var allowedUserIds = users.Select(u => u.UserId).ToHashSet();
                books = books.Where(b => allowedUserIds.Contains(b.UserId));

                var statistics = CalculateStatistics(books, users, isUserScoped);
                var insights = GenerateAutoInsights(books, statistics);

                string aiSummary;
                try
                {
                    aiSummary = await _openAIService.GenerateAnswerAsync(
                        isUserScoped
                            ? "Summarize this user's reading habits"
                            : "Summarize insights for the entire library",
                        PrepareDataContext(books, users, statistics, scopedUserId)
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI summary generation failed");
                    aiSummary = "AI insights are currently unavailable.";
                }

                return new LibraryInsightsDTO
                {
                    Summary = aiSummary,
                    Insights = insights,
                    Statistics = statistics,
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate library insights");
                throw;
            }
        }



        public async Task<UserReadingHabitsDTO> SummarizeUserReadingHabitsAsync(string userId)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId)
           ?? throw new KeyNotFoundException($"User {userId} not found");

                if (IsAdmin(user))
                {
                    throw new InvalidOperationException("Admin users do not have reading habits.");
                }

                var books = await _bookRepository.GetBooksByUserIdAsync(userId);

                var completed = books.Count(b => b.ReadingStatus == ReadingStatus.Completed);
                var inProgress = books.Count(b => b.ReadingStatus == ReadingStatus.Reading);

                var preferredGenres = books
                    .Where(b => !string.IsNullOrEmpty(b.Genre))
                    .GroupBy(b => b.Genre!)
                    .OrderByDescending(g => g.Count())
                    .Take(3)
                    .Select(g => g.Key)
                    .ToList();

                string aiSummary;
                try
                {
                    aiSummary = await _openAIService.GenerateAnswerAsync(
                        "Summarize this user's reading habits",
                        BuildUserContext(user, books, completed, inProgress, preferredGenres)
                    );
                }
                catch
                {
                    aiSummary = "User shows consistent reading activity.";
                }

                return new UserReadingHabitsDTO
                {
                    UserId = userId,
                    UserName = user.UserName,
                    Summary = aiSummary,
                    PreferredGenres = preferredGenres,
                    TotalBooks = books.Count(),
                    CompletedBooks = completed,
                    BooksInProgress = inProgress,
                    ReadingPattern = "Derived from activity",
                    Characteristics = GenerateUserCharacteristics(
                books.Count(),
                completed,
                inProgress,
                preferredGenres)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to summarize user reading habits");
                throw;
            }
        }

        private List<InsightItemDTO> GenerateAutoInsights(
     IEnumerable<Domain.Entities.Book> books,
     LibraryStatisticsDTO stats)
        {
            var list = books.ToList();  
            var insights = new List<InsightItemDTO>();

            if (!list.Any())
                return insights;

          
            insights.Add(new InsightItemDTO
            {
                Type = "GENRE",
                Title = "Most Read Genre",
                Description = $"{stats.MostPopularGenre} is the most read genre."
            });

         
            if (stats.TotalBooks > 0)
            {
                var completionRate =
                    (double)stats.CompletedBooksCount / stats.TotalBooks * 100;

                if (completionRate >= 70)
                {
                    insights.Add(new InsightItemDTO
                    {
                        Type = "COMPLETION",
                        Title = "Strong Completion Habit",
                        Description = "The reader finishes most of the books they start."
                    });
                }
                else if (completionRate < 40)
                {
                    insights.Add(new InsightItemDTO
                    {
                        Type = "COMPLETION",
                        Title = "Low Completion Rate",
                        Description = "Many books are started but not completed."
                    });
                }
            }

           
            if (stats.InProgressBooksCount > 1)
            {
                insights.Add(new InsightItemDTO
                {
                    Type = "COMPLETION",
                    Title = "Multi-Book Reader",
                    Description = "Multiple books are being read at the same time."
                });
            }
            else if (stats.InProgressBooksCount == 1)
            {
                insights.Add(new InsightItemDTO
                {
                    Type = "COMPLETION",
                    Title = "Focused Reader",
                    Description = "The reader usually focuses on one book at a time."
                });
            }

            return insights;
        }



        private LibraryStatisticsDTO CalculateStatistics(
            IEnumerable<Domain.Entities.Book> books,
            IEnumerable<UserProfileDTO> users,
            bool isUser)
        {
            var list = books.ToList();

            return new LibraryStatisticsDTO
            {
                TotalBooks = list.Count,
                TotalUsers = isUser ? null : users.Count(),
                CompletedBooksCount = list.Count(b => b.ReadingStatus == ReadingStatus.Completed),
                InProgressBooksCount = list.Count(b => b.ReadingStatus == ReadingStatus.Reading),
                MostPopularGenre = list
                    .GroupBy(b => b.Genre.Trim() ?? "Unknown")
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault() ?? "N/A",
                MostActiveUser = isUser ? null : list
                    .GroupBy(b => b.UserId)
                    .OrderByDescending(g => g.Count())
                    .Select(g => users.FirstOrDefault(u => u.UserId == g.Key)?.UserName)
                    .FirstOrDefault(),
                GenreDistribution = list
                    .GroupBy(b => b.Genre.Trim() ?? "Unknown")
                    .ToDictionary(g => g.Key, g => g.Count()),
                StatusDistribution = list
                    .GroupBy(b => b.ReadingStatus.ToString())
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }

        private string PrepareDataContext(
            IEnumerable<Domain.Entities.Book> books,
            IEnumerable<UserProfileDTO> users,
            LibraryStatisticsDTO stats,
            string? userId)
        {
            var sb = new StringBuilder();
            var list = books.ToList();

            sb.AppendLine(userId != null
                ? "User Library Overview"
                : "Complete Library Overview");

            sb.AppendLine($"Total Books: {stats.TotalBooks}");
            sb.AppendLine($"Completed: {stats.CompletedBooksCount}");
            sb.AppendLine($"In Progress: {stats.InProgressBooksCount}");
            sb.AppendLine($"Most Popular Genre: {stats.MostPopularGenre}");

            if (userId == null)
            {
                sb.AppendLine($"Total Users: {stats.TotalUsers}");
                sb.AppendLine($"Most Active User: {stats.MostActiveUser}");
            }

            return sb.ToString();
        }

        private string BuildUserContext(
            UserProfileDTO user,
            IEnumerable<Domain.Entities.Book> books,
            int completed,
            int inProgress,
            List<string> genres)
        {
            return $@"
User: {user.UserName}
Total Books: {books.Count()}
Completed: {completed}
In Progress: {inProgress}
Top Genres: {string.Join(", ", genres)}
";
        }


        private List<string> GenerateUserCharacteristics(
    int totalBooks,
    int completedBooks,
    int inProgressBooks,
    List<string> preferredGenres)
        {
            var characteristics = new List<string>();

            if (totalBooks == 0)
                return characteristics;

            var completionRate = (double)completedBooks / totalBooks * 100;

            if (completionRate >= 70)
                characteristics.Add("Consistent reader");
            else if (completionRate < 40)
                characteristics.Add("Starts more books than finishes");

            if (inProgressBooks == 1)
                characteristics.Add("Focused reader");
            else if (inProgressBooks > 1)
                characteristics.Add("Reads multiple books at once");

            if (preferredGenres.Count == 1)
                characteristics.Add("Genre-loyal reader");
            else if (preferredGenres.Count >= 3)
                characteristics.Add("Explores multiple genres");

            return characteristics;
        }



    }
}
