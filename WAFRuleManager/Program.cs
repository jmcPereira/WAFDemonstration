using Microsoft.EntityFrameworkCore;
using WAFRuleModels;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WafRuleDbContext>(options =>
    options.UseSqlServer(
        Environment.GetEnvironmentVariable("CONNECTION_STRING")
        ?? builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<WafRuleDbContext>();
        dbContext.Database.Migrate(); // Applies any pending migrations
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while applying migrations.");
    }
}

app.UseRouting();
app.UseAuthorization();

// Set MVC routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=WafRules}/{action=Index}/{rules?}"
);

app.Run();