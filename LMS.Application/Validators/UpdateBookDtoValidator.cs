using FluentValidation;
using LMS.Application.DTOs.Books;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.Validators
{
    public class UpdateBookDtoValidator  : AbstractValidator<UpdateBookDTO>
    {

        public UpdateBookDtoValidator() {


            RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

            RuleFor(x => x.Author)
                .NotEmpty().WithMessage("Author is required")
                .MaximumLength(150).WithMessage("Author cannot exceed 150 characters");

            RuleFor(x => x.Genre)
                .NotEmpty().WithMessage("Genre is required")
                .MaximumLength(100).WithMessage("Genre cannot exceed 100 characters");

            RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative")
            .LessThan(1000000).WithMessage("Price seems too high");

            RuleFor(x => x.PublicationYear)
            .GreaterThan(1000).WithMessage("Publication year must be after year 1000")
            .LessThanOrEqualTo(DateTime.UtcNow.Year).WithMessage("Publication year cannot be in the future")
            .When(x => x.PublicationYear.HasValue);

            RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5")
            .When(x => x.Rating.HasValue);




        }
    }
}
