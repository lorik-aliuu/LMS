using LMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.DTOs.Books
{
    public class UpdateBookDTO
    {

       
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public ReadingStatus ReadingStatus { get; set; }
     
        public int? PublicationYear { get; set; }
       
        public string? CoverImageUrl { get; set; }
        public int? Rating { get; set; }
       
    }
}
