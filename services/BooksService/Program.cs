using BooksService.Data;
using BooksService.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<BooksDbContext>(opt =>
        opt.UseNpgsql(connectionString));
    builder.Services.AddSingleton<IBookRepository, PostgresBookRepository>();
}
else
{
    builder.Services.AddSingleton<IBookRepository, InMemoryBookRepository>();
}

var app = builder.Build();

// Apply EF migrations / seed data on startup when using PostgreSQL.
if (!string.IsNullOrWhiteSpace(connectionString))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BooksDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();

app.Run();






