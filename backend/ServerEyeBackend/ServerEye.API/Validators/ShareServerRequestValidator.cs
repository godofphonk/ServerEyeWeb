namespace ServerEye.API.Validators;

using FluentValidation;
using ServerEye.Core.DTOs.Server;

public class ShareServerRequestValidator : AbstractValidator<ShareServerRequest>
{
    public ShareServerRequestValidator()
    {
        this.RuleFor(x => x.ServerId)
            .NotEmpty().WithMessage("Server ID is required");

        this.RuleFor(x => x.TargetUserEmail)
            .NotEmpty().WithMessage("Target user email is required")
            .EmailAddress().WithMessage("Invalid email address");

        this.RuleFor(x => x.AccessLevel)
            .IsInEnum().WithMessage("Invalid access level");
    }
}
