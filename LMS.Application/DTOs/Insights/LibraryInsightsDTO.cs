using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.DTOs.Insights
{
    public class LibraryInsightsDTO
    {
        public string Summary { get; set; }

        public List<InsightItemDTO> Insights { get; set; }
        public LibraryStatisticsDTO Statistics { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
