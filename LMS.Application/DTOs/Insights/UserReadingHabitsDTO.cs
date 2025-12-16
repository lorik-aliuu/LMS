using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.DTOs.Insights
{
    public class UserReadingHabitsDTO
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Summary { get; set; }
        public List<string> PreferredGenres { get; set; }
        public int TotalBooks { get; set; }
        public int CompletedBooks { get; set; }
        public int BooksInProgress { get; set; }
        public string ReadingPattern { get; set; }
        public List<string> Characteristics { get; set; }
    }
}
