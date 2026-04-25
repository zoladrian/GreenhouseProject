using Greenhouse.Api.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace Greenhouse.Api.Tests;

public sealed class ApiKeyFailFastTests
{
    [Fact]
    public void Validator_ShouldPass_WhenMutationsDisabled()
    {
        var v = new ApiKeyOptionsValidator();
        var result = v.Validate(null, new ApiKeyOptions { RequireForMutations = false, ApiKey = null });
        Assert.True(result.Succeeded, string.Join("; ", result.Failures ?? Array.Empty<string>()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validator_ShouldFail_WhenMutationsRequired_AndKeyMissing(string? key)
    {
        var v = new ApiKeyOptionsValidator();
        var result = v.Validate(null, new ApiKeyOptions { RequireForMutations = true, ApiKey = key });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, m => m.Contains("ApiSecurity:ApiKey", StringComparison.Ordinal));
    }

    [Fact]
    public void Validator_ShouldFail_WhenKeyTooShort()
    {
        var v = new ApiKeyOptionsValidator();
        var result = v.Validate(null, new ApiKeyOptions { RequireForMutations = true, ApiKey = "short-key-xx" });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, m => m.Contains("16 znaków", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("change-me-greenhouse-prod")]
    [InlineData("dev-only-greenhouse-XYZ123")]
    [InlineData("MY-SECRET-VALUE-1234567890")]
    [InlineData("please-replace-api-key-here")]
    public void Validator_ShouldFail_WhenKeyContainsKnownPlaceholder(string placeholderKey)
    {
        var v = new ApiKeyOptionsValidator();
        var result = v.Validate(null, new ApiKeyOptions { RequireForMutations = true, ApiKey = placeholderKey });

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, m => m.Contains("placeholder", StringComparison.Ordinal));
    }

    [Fact]
    public void Validator_ShouldPass_ForStrongKey()
    {
        var v = new ApiKeyOptionsValidator();
        var result = v.Validate(null, new ApiKeyOptions
        {
            RequireForMutations = true,
            ApiKey = "ZxQv-9fA1bD2eC3hJ4kL5mN6",
        });

        Assert.True(result.Succeeded, string.Join("; ", result.Failures ?? Array.Empty<string>()));
    }

    [Fact]
    public void Application_FailsToStart_WhenRequireForMutations_AndApiKeyMissing()
    {
        var ex = Assert.Throws<OptionsValidationException>(() =>
        {
            using var factory = new MissingKeyFactory();
            _ = factory.CreateClient();
        });

        Assert.Contains("ApiSecurity:ApiKey", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Application_FailsToStart_WhenApiKeyIsKnownPlaceholder()
    {
        var ex = Assert.Throws<OptionsValidationException>(() =>
        {
            using var factory = new PlaceholderKeyFactory();
            _ = factory.CreateClient();
        });

        Assert.Contains("placeholder", ex.Message, StringComparison.Ordinal);
    }

    private sealed class MissingKeyFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"greenhouse-failfast-{Guid.NewGuid()}.db");
            builder.UseSetting("Infrastructure:DatabasePath", dbPath);
            builder.UseSetting("Mqtt:Enabled", "false");
            builder.UseSetting("ApiSecurity:RequireForMutations", "true");
            builder.UseSetting("ApiSecurity:ApiKey", "");
        }
    }

    private sealed class PlaceholderKeyFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var dbPath = Path.Combine(Path.GetTempPath(), $"greenhouse-failfast-{Guid.NewGuid()}.db");
            builder.UseSetting("Infrastructure:DatabasePath", dbPath);
            builder.UseSetting("Mqtt:Enabled", "false");
            builder.UseSetting("ApiSecurity:RequireForMutations", "true");
            builder.UseSetting("ApiSecurity:ApiKey", "change-me-greenhouse-prod-2024");
        }
    }
}
