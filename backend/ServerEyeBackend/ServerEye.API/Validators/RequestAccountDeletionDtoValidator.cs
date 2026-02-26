namespace ServerEye.API.Validators;

using FluentValidation;
using ServerEye.Core.DTOs.Auth;

public class RequestAccountDeletionDtoValidator : AbstractValidator<RequestAccountDeletionDto>
{
    public RequestAccountDeletionDtoValidator() =>
        this.RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");
}
