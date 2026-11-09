using System.Text;
using ControlDeDinero.Api;
using ControlDeDinero.Api.Auth;
using ControlDeDinero.Api.Commands;
using ControlDeDinero.Application.Auth;
using ControlDeDinero.Application.Clients;
using ControlDeDinero.Application.Loans;
using ControlDeDinero.Application.Summary;
using ControlDeDinero.Infrastructure.Identity;
using ControlDeDinero.Infrastructure.Persistence;
using ControlDeDinero.Infrastructure.Repositories;
using ControlDeDinero.Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration // Get the connection string from appsettings.json
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Database connection string was not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySQL(connectionString);
});

// Accounts: ASP.NET Core Identity stores users (with hashed passwords) in MySQL through AppDbContext.
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        // MySQL cannot index unlimited text, so Identity's key columns get a fixed length.
        options.Stores.MaxLengthForKeys = 128;

        // Length matters more than symbols: 10+ characters, no other rule.
        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders(); // needed by the reset-password command

// Login token (JWT). The signing key is a secret: dotnet user-secrets set "Jwt:Key" "<long random text>"
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (jwt.Key.Length < 32)
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 32 characters. Set it with: " +
        "dotnet user-secrets set \"Jwt:Key\" \"<a long random text>\" --project ControlDeDinero.Api");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep the claim called "sub" as "sub"
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // A valid signature is not enough: the token must carry the user's current SecurityStamp.
        // Changing or resetting the password changes the stamp, so older tokens stop working.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var validator = context.HttpContext.RequestServices.GetRequiredService<TokenSessionValidator>();
                var userId = context.Principal?.FindFirst("sub")?.Value;
                var stamp = context.Principal?.FindFirst("stamp")?.Value;

                if (!await validator.IsStillValidAsync(userId, stamp))
                    context.Fail("This session is no longer valid. Log in again.");
            }
        };
    });

// Every endpoint requires a logged-in user unless it says [AllowAnonymous] (only login does).
// A new controller can therefore never be left open by forgetting an attribute.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<UserProvisioner>();
builder.Services.AddScoped<TokenSessionValidator>();

builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<ClientService>();

builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<LoanService>();
builder.Services.AddScoped<SummaryService>();

// "Today" is the Costa Rica date, even if the server runs in UTC.
builder.Services.AddSingleton<TimeProvider>(ZonedTimeProvider.CostaRica());

// Errors: ValidationException -> 400 in one place; anything else -> 500.
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();

// CORS: which web pages may call this API from a browser. Origins come from configuration
// (Cors:AllowedOrigins); appsettings.Development.json allows the Vite dev server.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Terminal commands instead of starting the web server:
//   dotnet run --project ControlDeDinero.Api -- create-user someone@example.com
//   dotnet run --project ControlDeDinero.Api -- reset-password someone@example.com
if (args.Length > 0 && args[0] == CreateUserCommand.Name)
    return await CreateUserCommand.RunAsync(app, args);

if (args.Length > 0 && args[0] == ResetPasswordCommand.Name)
    return await ResetPasswordCommand.RunAsync(app, args);

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

// Order matters: first find out who the user is, then decide what they may do.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

return 0;
