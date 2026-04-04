namespace ServerEye.UnitTests.Validators;

using ServerEye.API.Validators;
using ServerEye.Core.DTOs.Metrics;

internal class MetricsRequestValidatorTests
{
    private readonly MetricsRequestValidator sut;

    public MetricsRequestValidatorTests()
    {
        this.sut = new MetricsRequestValidator();
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldPass()
    {
        var dto = new MetricsRequest
        {
            Start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            Granularity = "1h"
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithNullGranularity_ShouldPass()
    {
        var dto = new MetricsRequest
        {
            Start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            Granularity = null
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("1m")]
    [InlineData("5m")]
    [InlineData("10m")]
    [InlineData("1h")]
    public void Validate_WithAllowedGranularities_ShouldPass(string granularity)
    {
        var dto = new MetricsRequest
        {
            Start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            Granularity = granularity
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("2m")]
    [InlineData("30s")]
    [InlineData("1d")]
    [InlineData("invalid")]
    public void Validate_WithInvalidGranularity_ShouldFail(string granularity)
    {
        var dto = new MetricsRequest
        {
            Start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            Granularity = granularity
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Granularity");
    }

    [Fact]
    public void Validate_WithNullStart_ShouldFail()
    {
        var dto = new MetricsRequest
        {
            Start = null,
            End = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Start");
    }

    [Fact]
    public void Validate_WithNullEnd_ShouldFail()
    {
        var dto = new MetricsRequest
        {
            Start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            End = null
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "End");
    }

    [Fact]
    public void Validate_WithStartAfterEnd_ShouldFail()
    {
        var dto = new MetricsRequest
        {
            Start = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Start");
    }

    [Fact]
    public void Validate_WithStartEqualToEnd_ShouldFail()
    {
        var ts = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var dto = new MetricsRequest
        {
            Start = ts,
            End = ts
        };

        var result = this.sut.Validate(dto);

        result.IsValid.Should().BeFalse();
    }
}
