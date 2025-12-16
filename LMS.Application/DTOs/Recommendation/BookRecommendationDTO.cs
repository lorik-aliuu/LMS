using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.DTOs.Recommendation
{
    public class BookRecommendationDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public decimal? EstimatedPrice { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}
