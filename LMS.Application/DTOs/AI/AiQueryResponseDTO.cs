using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.DTOs.AI
{
    public class AiQueryResponseDTO
    {
        public bool Success { get; set; }
        public string Answer { get; set; } = string.Empty;
        public string? InterpretedQuery { get; set; }
        public List<Dictionary<string, object>>? Data { get; set; } 
        public string? ChartType { get; set; } 
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
