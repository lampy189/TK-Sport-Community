using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Google;
using be_da.Authorization;
using be_da.Application.Abstractions;
using be_da.Application.Accounts;
using be_da.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.OperationFilter<be_da.Swagger.AuthDocumentationFilter>());
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddScoped<IAccountStore>(services => new SqlAccountStore(
    services.GetRequiredService<IConfiguration>().GetConnectionString("Sep490")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Sep490 using local secrets or environment variables.")));
builder.Services.AddScoped<ISessionStore>(services => new SqlSessionStore(
    services.GetRequiredService<IConfiguration>().GetConnectionString("Sep490")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Sep490 using local secrets or environment variables.")));
builder.Services.AddScoped<GoogleSignIn>();
builder.Services.AddScoped<AccountCookieEvents>();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__Host-TKSC.Csrf";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddAuthentication(AccountPrincipal.Scheme)
    .AddCookie(AccountPrincipal.Scheme, options =>
    {
        options.Cookie.Name = "__Host-TKSC.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
        options.EventsType = typeof(AccountCookieEvents);
    })
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.SignInScheme = AccountPrincipal.Scheme;
        options.UsePkce = true;
        options.SaveTokens = false;
        options.Events.OnRedirectToAuthorizationEndpoint = async context =>
        {
            if (context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
                && context.Request.Headers["X-TKSC-Swagger"] == "1")
                await context.Response.WriteAsJsonAsync(new { authorizationUrl = context.RedirectUri });
            else
                context.Response.Redirect(context.RedirectUri);
        };
        options.Events.OnCreatingTicket = async context =>
        {
            var verified = (context.User.TryGetProperty("verified_email", out var value)
                || context.User.TryGetProperty("email_verified", out value)) && value.ValueKind == JsonValueKind.True;
            var service = context.HttpContext.RequestServices.GetRequiredService<GoogleSignIn>();
            var account = await service.ExecuteAsync(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier),
                context.Principal?.FindFirstValue(ClaimTypes.Email), context.Principal?.FindFirstValue(ClaimTypes.Name),
                verified, context.HttpContext.RequestAborted);
            context.Principal = AccountPrincipal.Create(account);
        };
        options.Events.OnRemoteFailure = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { code = "google_sign_in_failed", message = "Sign-in could not be completed. Retry or contact support." });
        };
    });
builder.Services.AddOptions<GoogleOptions>(GoogleDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, config) =>
    {
        // Keep the API available for setup; the login endpoint refuses missing credentials.
        var clientId = config["Authentication:Google:ClientId"];
        var clientSecret = config["Authentication:Google:ClientSecret"];
        options.ClientId = string.IsNullOrWhiteSpace(clientId) ? "unconfigured" : clientId;
        options.ClientSecret = string.IsNullOrWhiteSpace(clientSecret) ? "unconfigured" : clientSecret;
    });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.InjectJavascript("/swagger/tksc-auth.js");
        options.UseRequestInterceptor("function(request) { return window.tkscSwaggerRequest(request); }");
        options.UseResponseInterceptor("function(response) { return window.tkscSwaggerResponse(response); }");
    });
    app.MapGet("/swagger/tksc-auth.js", () => Results.Stream(
        typeof(Program).Assembly.GetManifestResourceStream("be_da.Swagger.tksc-auth.js")!,
        "text/javascript; charset=utf-8")).ExcludeFromDescription();
}
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/auth") || context.Request.Path == "/signin-google")
        context.Response.Headers.CacheControl = "no-store";
    await next(context);
});
app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
