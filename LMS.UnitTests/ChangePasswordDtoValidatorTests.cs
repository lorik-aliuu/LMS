using FluentAssertions;
using LMS.Application.DTOs.Users;
using LMS.Application.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.UnitTests
{
    public class ChangePasswordDtoValidatorTests
    {
        private readonly ChangePasswordDtoValidator _validator;

        public ChangePasswordDtoValidatorTests()
        {
            _validator = new ChangePasswordDtoValidator();
        }

       
        [Fact]
        public void Validate_ValidChangePasswordDTO_ShouldPass()
        {
           
            var dto = new ChangePasswordDTO
            {
                CurrentPassword = "OldPass1",
                NewPassword = "NewPass2",
                ConfirmNewPassword = "NewPass2"
            };

          
            var result = _validator.Validate(dto);

           
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_InvalidChangePasswordDTO_ShouldFail()
        {
           
            var dto = new ChangePasswordDTO
            {
                CurrentPassword = "",           
                NewPassword = "short",          
                ConfirmNewPassword = "diff"    
            };

           
            var result = _validator.Validate(dto);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "CurrentPassword");
            result.Errors.Should().Contain(e => e.PropertyName == "NewPassword");
            result.Errors.Should().Contain(e => e.PropertyName == "ConfirmNewPassword");
        }
    }
}
