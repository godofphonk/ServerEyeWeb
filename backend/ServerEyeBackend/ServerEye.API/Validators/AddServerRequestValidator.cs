namespace ServerEye.API.Validators;

using FluentValidation;
using ServerEye.Core.DTOs.Server;

public class AddServerRequestValidator : AbstractValidator<AddServerRequest>
{
    public AddServerRequestValidator() =>
        this.RuleFor(x => x.ServerKey)
            .NotEmpty().WithMessage("Server key is required")
            .MinimumLength(10).WithMessage("Server key must be at least 10 characters");
}
