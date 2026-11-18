using be_da.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace TKSC.Api.Tests;

public sealed class ManagedSessionValidationTests
{
    [Theory]
    [InlineData(1, 1, 1, 8, 0, 100, true)]
    [InlineData(1, 1, 1, 1, 100000, 0, true)]
    [InlineData(0, 1, 1, 8, 0, 0, false)]
    [InlineData(1, 0, 1, 8, 0, 0, false)]
    [InlineData(1, 1, 0, 8, 0, 0, false)]
    [InlineData(1, 1, 1, 0, 0, 0, false)]
    [InlineData(1, 1, 1, 9, 0, 0, false)]
    [InlineData(1, 1, 1, 8, -1, 0, false)]
    [InlineData(1, 1, 1, 8, 0, 101, false)]
    public void Mvc_validation_accepts_valid_requests_and_rejects_invalid_fields(
        long sport, long court, long slot, short capacity, int price, int bandLength, bool valid)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvcCore().AddDataAnnotations();
        using var provider = services.BuildServiceProvider();
        var context = new ActionContext(new DefaultHttpContext { RequestServices = provider },
            new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        var request = new SaveManagedSessionRequest(sport, court, slot,
            new DateOnly(2026, 11, 20), new string('B', bandLength), capacity, price);
        provider.GetRequiredService<IObjectModelValidator>().Validate(context, null, "", request);
        Assert.Equal(valid, context.ModelState.IsValid);
    }
}
