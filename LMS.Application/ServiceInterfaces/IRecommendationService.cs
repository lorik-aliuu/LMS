using LMS.Application.DTOs.Recommendation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.ServiceInterfaces
{
    public interface IRecommendationService
    {
        Task<BookRecommendationResponseDTO> GetRecommendationsAsync( string userId,  RecommendationRequestDTO request);

        Task<ActionRecommendationResponseDTO> SaveRecommendedBookAsync(string userId, SaveRecommendationRequestDTO request);
        Task<ActionRecommendationResponseDTO> DismissRecommendedBookAsync(string userId, DismissRecommendationDTO request);

    }
}
