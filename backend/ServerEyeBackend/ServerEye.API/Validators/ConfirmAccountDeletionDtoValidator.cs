namespace ServerEye.API.Validators;

using FluentValidation;
using ServerEye.Core.DTOs.Auth;

public class ConfirmAccountDeletionDtoValidator : AbstractValidator<ConfirmAccountDeletionDto>
{
    public ConfirmAccountDeletionDtoValidator() =>
        this.RuleFor(x => x.ConfirmationCode)
            .NotEmpty().WithMessage("Confirmation code is required")
            .Length(6).WithMessage("Confirmation code must be 6 characters")
            .Matches("^[0-9]+$").WithMessage("Confirmation code must contain only digits");
}
