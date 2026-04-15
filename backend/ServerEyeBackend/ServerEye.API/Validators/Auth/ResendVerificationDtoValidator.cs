namespace ServerEye.API.Validators;

using FluentValidation;
using ServerEye.Core.DTOs.Auth;

public sealed class ResendVerificationDtoValidator : AbstractValidator<ResendVerificationDto>
{
    public ResendVerificationDtoValidator() =>
        this.RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");
}
