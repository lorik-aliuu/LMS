using LMS.Application.DTOs.AI;
using LMS.Application.DTOs.Insights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.ServiceInterfaces
{
    public interface ILibraryInsightsService
    {
        Task<LibraryInsightsDTO> GenerateLibraryInsightsAsync(string? userId = null);
        Task<UserReadingHabitsDTO> SummarizeUserReadingHabitsAsync(string userId);
     
    }
}
