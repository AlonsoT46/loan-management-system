
using Microsoft.EntityFrameworkCore;
using ControlDeDinero.Api;
using ControlDeDinero.Application.Clients;
using ControlDeDinero.Application.Loans;
using ControlDeDinero.Application.Summary;
using ControlDeDinero.Infrastructure.Persistence;
using ControlDeDinero.Infrastructure.Repositories;
using ControlDeDinero.Infrastructure.Time;


var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration // Get the connection string from appsettings.json
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Database connection string was not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySQL(connectionString);
});

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

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
