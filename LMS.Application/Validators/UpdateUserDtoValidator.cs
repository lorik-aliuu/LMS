using FluentValidation;
using LMS.Application.DTOs.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.Application.Validators
{
   
    public class UpdateUserProfileDtoValidator : AbstractValidator<UpdateUserDTO>
    {
        public UpdateUserProfileDtoValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters")
                .Matches("^[a-zA-Z ]*$").WithMessage("First name can only contain letters and spaces");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters")
                .Matches("^[a-zA-Z ]*$").WithMessage("Last name can only contain letters and spaces");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Phone number must be valid")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.DateOfBirth)
                .LessThan(DateTime.Today).WithMessage("Date of birth must be in the past")
                .GreaterThan(DateTime.Today.AddYears(-120)).WithMessage("Date of birth is too far in the past")
                .Must(BeAtLeast13YearsOld).WithMessage("You must be at least 13 years old")
                .When(x => x.DateOfBirth.HasValue);

         
        }

        private bool BeAtLeast13YearsOld(DateTime? dateOfBirth)
        {
            if (!dateOfBirth.HasValue) return true;

            var age = DateTime.Today.Year - dateOfBirth.Value.Year;
            if (dateOfBirth.Value.Date > DateTime.Today.AddYears(-age)) age--;

            return age >= 13;
        }

       
    }
}
