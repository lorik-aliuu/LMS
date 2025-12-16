using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LMS.Application.DTOs.Insights
{
    public class LibraryStatisticsDTO
    {
        public int TotalBooks { get; set; }
       
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? TotalUsers { get; set; }
        public string MostPopularGenre { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MostActiveUser { get; set; }
        public int CompletedBooksCount { get; set; }
        public int InProgressBooksCount { get; set; }
        public Dictionary<string, int> GenreDistribution { get; set; }
        public Dictionary<string, int> StatusDistribution { get; set; }
    }
}
