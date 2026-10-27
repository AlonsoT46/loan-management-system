
using Microsoft.EntityFrameworkCore;
using ControlDeDinero.Application.Clients;
using ControlDeDinero.Application.Loans;
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

// "Today" is the Costa Rica date, even if the server runs in UTC.
builder.Services.AddSingleton<TimeProvider>(ZonedTimeProvider.CostaRica());

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
