using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using System.Text.Json;
using Xunit;
namespace TKSC.Api.Tests;
public class SwaggerTests
{
 [Fact]
 public async Task DevelopmentSwaggerIncludesTicketAftercare()
 {
  await using var app = new LoginApp { EnvironmentName = "Development" };
  using var client = app.Client();
  app.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
  var response = await client.GetAsync("/swagger/v1/swagger.json");
  var body = await response.Content.ReadAsStringAsync();
  Assert.True(response.IsSuccessStatusCode, body);
  using var document = JsonDocument.Parse(body);
  Assert.True(document.RootElement.GetProperty("paths").TryGetProperty("/api/ticket-aftercare/reviews", out _));
 }
}
