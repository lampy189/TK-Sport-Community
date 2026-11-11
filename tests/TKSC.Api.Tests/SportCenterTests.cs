using be_da.Infrastructure.Persistence;
using be_da.Application.DTOs;
using System.Security.Claims;
using be_da.Presentation.Authorization;
using be_da.Presentation.Controllers;
using be_da.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace TKSC.Api.Tests;
public sealed class SportCenterTests
{
    [Fact]
    public void AccountPrincipalCarriesCurrentAssignment()
    {
        var account=new Account(1,"staff@example.invalid","Staff","Staff","Active","subject",false,VenueId:7);
        Assert.Equal("7",AccountPrincipal.Create(account).FindFirstValue(SportCenterConnection.VenueClaim));
        Assert.Equal("8",AccountPrincipal.Create(account with { VenueId=8 }).FindFirstValue(SportCenterConnection.VenueClaim));
    }
    [Theory]
    [InlineData("Staff")][InlineData("Manager")][InlineData("Referee")]
    public async Task UnassignedPersonnelCannotReadBusinessData(string role)
    {
        var called=false;
        var context=new DefaultHttpContext { User=AccountPrincipal.Create(new Account(1,"u@example.invalid","User",role,"Active","subject",false)) };
        context.Response.Body=new MemoryStream();context.Request.Path="/api/v1/revenue/daily";
        await new SportCenterMiddleware(_=>{called=true;return Task.CompletedTask;}).InvokeAsync(context,new ConfigurationBuilder().Build());
        Assert.Equal(403,context.Response.StatusCode);Assert.False(called);
    }
    [Fact]
    public async Task SelectedCenterCannotBeForgedByRequestHeader()
    {
        var context=new DefaultHttpContext { User=AccountPrincipal.Create(new Account(1,"u@example.invalid","User","Staff","Active","subject",false,VenueId:7)) };
        context.Request.Headers["X-Sport-Center"]="8";
        await new SportCenterMiddleware(_=>Task.CompletedTask).InvokeAsync(context,new ConfigurationBuilder().Build());
        Assert.Equal(7L,context.Items["SportCenterId"]);
    }
    [Fact]
    public void ReportRangeIsBounded()
    {
        Assert.True(RevenueController.ValidRange(new(2026,11,1),new(2026,11,5)));
        Assert.False(RevenueController.ValidRange(new(2026,11,5),new(2026,11,1)));
        Assert.False(RevenueController.ValidRange(new(2025,1,1),new(2026,11,1)));
        Assert.False(RevenueController.ValidRange(default,new(2026,11,1)));
    }

    [Theory]
    [InlineData("Development", false, 200)]
    [InlineData("Development", true, 403)]
    [InlineData("Production", false, 403)]
    public async Task LegacyAssignmentOptOutOnlyWorksInDevelopment(string environmentName, bool requireAssignment, int expectedStatus)
    {
        var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(
            new Microsoft.AspNetCore.Builder.WebApplicationOptions { EnvironmentName = environmentName });
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["SportCenters:RequireAssignment"] = requireAssignment.ToString() }).Build();
        var context = new DefaultHttpContext { User = AccountPrincipal.Create(
            new Account(1, "staff@example.invalid", "Staff", "Staff", "Active", "subject", false)) };
        context.Request.Path = "/api/skill-assessments";
        context.Response.Body = new MemoryStream();
        var called = false;
        await new SportCenterMiddleware(_ => { called = true; return Task.CompletedTask; })
            .InvokeAsync(context, configuration, builder.Environment);
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal(expectedStatus == 200, called);
    }
}
