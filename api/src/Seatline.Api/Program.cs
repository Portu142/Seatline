using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Seatline.Api.Auth;
using Seatline.Api.Data;
using Seatline.Api.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Configuration is read lazily (through IConfiguration/IOptions at resolve time) so that
// integration tests can override it with WebApplicationFactory.
services.AddSingleton(TimeProvider.System);
services.AddProblemDetails();
services.AddControllers();
services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

// --- Database -------------------------------------------------------------------------------
services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseNpgsql(ConnectionStrings.Normalize(
        sp.GetRequiredService<IConfiguration>().GetConnectionString("Default"))));

// --- Identity ---------------------------------------------------------------------------------
services
    .AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

// --- JWT authentication -----------------------------------------------------------------------
services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
    {
        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Value.Issuer,
            ValidAudience = jwt.Value.Audience,
            IssuerSigningKey = TokenService.SigningKey(jwt.Value),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = JwtRegisteredClaimNames.Name,
            RoleClaimType = AuthConstants.RoleClaim,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

services.AddAuthorizationBuilder()
    .AddPolicy(AuthPolicies.Admin, policy => policy.RequireRole(Roles.Admin))
    .AddPolicy(AuthPolicies.Door, policy => policy.RequireRole(Roles.Validator, Roles.Admin));

services.AddScoped<TokenService>();

// --- Rate limiting (login / register / refresh) -----------------------------------------------
services.AddRateLimiter(options => options.RejectionStatusCode = StatusCodes.Status429TooManyRequests);
services.AddOptions<RateLimiterOptions>().Configure<IConfiguration>((options, config) =>
{
    var permitsPerMinute = config.GetValue("RateLimiting:AuthRequestsPerMinute", 20);
    options.AddPolicy(AuthConstants.AuthRateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitsPerMinute,
                Window = TimeSpan.FromMinutes(1),
            }));
});

// --- CORS for the React app -------------------------------------------------------------------
services.AddCors();
services.AddOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>().Configure<IConfiguration>((options, config) =>
{
    var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    options.AddDefaultPolicy(policy => policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod());
});

// --- Health -----------------------------------------------------------------------------------
services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

// Behind Render/Fly/Railway the TLS terminates at the proxy.
services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Configuration.GetValue("Swagger:Enabled", true))
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Seatline API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Seatline API";
    });
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()),
        });
    },
});
app.MapControllers();

await DatabaseInitializer.InitializeAsync(app.Services);

await app.RunAsync();

/// <summary>Entry point; partial so integration tests can reference it via WebApplicationFactory.</summary>
public partial class Program;
