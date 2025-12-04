using LMS.Domain.Common;
using LMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Domain.Entities
{
    public class Book : BaseEntity
    {
        public string Title { get; set; }

        public string Author { get; set; }

        public string Genre { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public ReadingStatus ReadingStatus { get; set; } = ReadingStatus.NotStarted;

        public string UserId { get; set; } = string.Empty;

        public string? CoverImageUrl { get; set; }

        public int? Rating { get; set; }

        public int? PublicationYear { get; set; }


    }
}
