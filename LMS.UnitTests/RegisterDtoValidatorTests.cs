using FluentAssertions;
using LMS.Application.DTOs.Auth;
using LMS.Application.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMS.UnitTests
{
    public class RegisterDtoValidatorTests
    {
        private readonly RegisterDtoValidator _validator;

        public RegisterDtoValidatorTests()
        {
            _validator = new RegisterDtoValidator();
        }

        [Fact]
        public void Validate_ValidRegisterDTO_ShouldPass()
        {
           
            var dto = new RegisterDTO
            {
                UserName = "User_123",
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                Password = "Password1",
                ConfirmPassword = "Password1"
            };

         
            var result = _validator.Validate(dto);

          
            result.IsValid.Should().BeTrue();
        }

      
        [Fact]
        public void Validate_InvalidRegisterDTO_ShouldFail()
        {
            
            var dto = new RegisterDTO
            {
                UserName = "Us",
                FirstName = "John123",          
                LastName = "",                   
                Email = "invalidemail",        
                Password = "pass",               
                ConfirmPassword = "different"    
            };

           
            var result = _validator.Validate(dto);

          
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCountGreaterThan(0);

        
            result.Errors.Should().ContainSingle(e => e.PropertyName == "UserName");
            result.Errors.Should().ContainSingle(e => e.PropertyName == "FirstName");
            result.Errors.Should().ContainSingle(e => e.PropertyName == "LastName");
            result.Errors.Should().ContainSingle(e => e.PropertyName == "Email");
            result.Errors.Should().Contain(e => e.PropertyName == "Password");
            result.Errors.Should().Contain(e => e.PropertyName == "ConfirmPassword");
        }
    }
}
