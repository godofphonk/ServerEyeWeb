namespace ServerEye.API.Validators;

using FluentValidation;
using ServerEye.Core.DTOs.Auth;

public sealed class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordDtoValidator()
    {
        this.RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required.");

        this.RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.")
            .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.");
    }
}
