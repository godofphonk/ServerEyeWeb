namespace ServerEye.API.Validators;

using FluentValidation;
using ServerEye.Core.DTOs.Auth;

public sealed class ConfirmEmailChangeDtoValidator : AbstractValidator<ConfirmEmailChangeDto>
{
    public ConfirmEmailChangeDtoValidator() =>
        this.RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code is required.")
            .Length(6).WithMessage("Verification code must be 6 characters long.")
            .Matches(@"^\d{6}$").WithMessage("Verification code must contain only digits.");
}
