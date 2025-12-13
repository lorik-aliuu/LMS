using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.DTOs.Recommendation
{
    public class SaveRecommendationRequestDTO
    {
        public string Title { get; set; } = null!;
        public string Author { get; set; } = null!;
        public string Genre { get; set; } = null!;
        public decimal EstimatedPrice
        {
            get; set;
        }
    }
}
