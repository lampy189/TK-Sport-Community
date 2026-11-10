using TKSC.Infrastructure;
using TKSC.Application.Interfaces;
using TKSC.Application.Services;
using TKSC.Controllers.Middlewares;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Google;
using be_da.Authorization;
using be_da.Application.Abstractions;
using be_da.Application.Accounts;
using be_da.Infrastructure.Notifications;
using be_da.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("Controllers/appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"Controllers/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
if (builder.Environment.IsDevelopment()) builder.Configuration.AddUserSecrets<Program>(optional: true);
builder.Configuration.AddJsonFile("Controllers/appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);
// Trust forwarded scheme/IP only from deployment-configured proxy addresses.
var trustedProxies = builder.Configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [];
if (trustedProxies.Length > 0)
    builder.Services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
            | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
        foreach (var address in trustedProxies) options.KnownProxies.Add(System.Net.IPAddress.Parse(address));
    });
builder.Services.AddControllers();
builder.Services.AddOptions<be_da.Application.Chatbot.ChatbotOptions>()
    .Bind(builder.Configuration.GetSection("Chatbot"));
builder.Services.AddScoped<be_da.Application.Chatbot.IPlayerChatData, be_da.Infrastructure.Persistence.SqlPlayerChatData>();
builder.Services.AddScoped<be_da.Application.Chatbot.PlayerChatbot>();
builder.Services.AddHttpClient<be_da.Application.Chatbot.IChatAnswerGenerator, be_da.Infrastructure.Services.OpenAiChatAnswerGenerator>(
    client => client.Timeout = TimeSpan.FromSeconds(40));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.OperationFilter<be_da.Swagger.AuthDocumentationFilter>());
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<PaymentExceptionHandler>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<TicketCheckInService>();
builder.Services.AddScoped<TicketAftercareService>();
builder.Services.AddScoped<TicketSupportService>();
builder.Services.AddScoped<SkillAssessmentService>();
builder.Services.AddScoped<SkillReviewService>();
builder.Services.AddScoped<GoodsPaymentService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<TKSC.Infrastructure.Notifications.TicketEmailWorker>();
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
builder.Services.AddScoped<ISkillProfileStore>(services => new SqlSkillProfileStore(
    services.GetRequiredService<IConfiguration>().GetConnectionString("Sep490")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Sep490 using local secrets or environment variables.")));
builder.Services.AddScoped<IProfileStore>(services => new SqlProfileStore(
    services.GetRequiredService<IConfiguration>().GetConnectionString("Sep490")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Sep490 using local secrets or environment variables.")));
builder.Services.AddScoped<IFriendshipStore>(services => new SqlFriendshipStore(
    services.GetRequiredService<IConfiguration>().GetConnectionString("Sep490")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Sep490 using local secrets or environment variables.")));
builder.Services.AddScoped<IFirebaseNotificationService, FirebaseNotificationService>();
builder.Services.AddScoped<IStockStore>(services => new SqlStockStore(
    services.GetRequiredService<IConfiguration>().GetConnectionString("Sep490")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Sep490 using local secrets or environment variables.")));
builder.Services.AddScoped<ISessionSupplyStore>(services => new SqlSessionSupplyStore(
    services.GetRequiredService<IConfiguration>().GetConnectionString("Sep490")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Sep490 using local secrets or environment variables.")));
builder.Services.AddScoped<GoogleSignIn>();
builder.Services.AddScoped<AccountCookieEvents>();
var isDevelopment = builder.Environment.IsDevelopment();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = isDevelopment ? "TKSC.Csrf" : "__Host-TKSC.Csrf";
    options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
    options.Cookie.SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.Strict;
});
builder.Services.AddAuthentication(AccountPrincipal.Scheme)
    .AddCookie(AccountPrincipal.Scheme, options =>
    {
        options.Cookie.Name = isDevelopment ? "TKSC.Session" : "__Host-TKSC.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
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
            if (context.Properties.Items.TryGetValue("login_portal", out var requestedPortal))
                context.HttpContext.Items["login_portal"] = requestedPortal;
            var service = context.HttpContext.RequestServices.GetRequiredService<GoogleSignIn>();
            var account = await service.ExecuteAsync(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier),
                context.Principal?.FindFirstValue(ClaimTypes.Email), context.Principal?.FindFirstValue(ClaimTypes.Name),
                verified, context.HttpContext.RequestAborted, context.Properties.Items.TryGetValue("login_portal", out var selectedPortal) ? selectedPortal : null);
            context.Principal = AccountPrincipal.Create(account);
        };
        options.Events.OnRemoteFailure = async context =>
        {
            context.HandleResponse();
            if (context.HttpContext.Items["login_portal"] is string portal && LoginPortal.IsValid(portal))
            {
                var origin = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
                    ? "http://localhost:5173" : "";
                context.Response.Redirect(origin + LoginPortal.Path(portal) + "?loginError=1");
                return;
            }
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("player-chatbot", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();
if (trustedProxies.Length > 0) app.UseForwardedHeaders();
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
// The deployment image bundles the React build; local Vite development remains supported.
app.UseDefaultFiles();
app.UseStaticFiles();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/auth") || context.Request.Path == "/signin-google")
        context.Response.Headers.CacheControl = "no-store";
    await next(context);
});
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseMiddleware<SportCenterMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapFallback(async context =>
{
    var path = context.Request.Path;
    var index = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "index.html");
    if (!HttpMethods.IsGet(context.Request.Method) || path.StartsWithSegments("/api")
        || path.StartsWithSegments("/signin-google") || Path.HasExtension(path.Value) || !File.Exists(index))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    context.Response.ContentType = "text/html; charset=utf-8";
    context.Response.Headers.CacheControl = "no-cache";
    await context.Response.SendFileAsync(index, context.RequestAborted);
});
app.Run();

public partial class Program { }
