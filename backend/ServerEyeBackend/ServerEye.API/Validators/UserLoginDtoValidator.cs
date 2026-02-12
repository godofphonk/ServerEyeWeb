namespace ServerEye.API.Validators;

using FluentValidation;
using ServerEye.Core.DTOs.UserDto;

public sealed class UserLoginDtoValidator : AbstractValidator<UserLoginDto>
{
    public UserLoginDtoValidator()
    {
        this.RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        this.RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
