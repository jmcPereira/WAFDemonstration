using Microsoft.EntityFrameworkCore;
using WAF.Middleware;
using WAFRuleModels;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<WafRuleDbContext>(options =>
    options.UseSqlServer(
        Environment.GetEnvironmentVariable("CONNECTION_STRING")
        ?? builder.Configuration.GetConnectionString("DefaultConnection")));
var app = builder.Build();
app.UseRouting();
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
// 🛑 Move WAF Middleware before Swagger
app.UseMiddleware<WafMiddleware>();
app.Run();