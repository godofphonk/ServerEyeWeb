namespace ServerEye.API.Validators;

using FluentValidation;
using ServerEye.Core.DTOs.Metrics;

public class MetricsRequestValidator : AbstractValidator<MetricsRequest>
{
    public MetricsRequestValidator()
    {
        this.RuleFor(x => x.Start)
            .NotEmpty().WithMessage("Start time is required")
            .LessThan(x => x.End).WithMessage("Start time must be before end time");

        this.RuleFor(x => x.End)
            .NotEmpty().WithMessage("End time is required")
            .GreaterThan(x => x.Start).WithMessage("End time must be after start time");

        this.RuleFor(x => x.Granularity)
            .Must(g => g == null || new[] { "1m", "5m", "10m", "1h" }.Contains(g, StringComparer.Ordinal))
            .WithMessage("Granularity must be one of: 1m, 5m, 10m, 1h");
    }
}
