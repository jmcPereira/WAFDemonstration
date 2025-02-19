using Microsoft.EntityFrameworkCore;
using VulnerableApp.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<MessageContext>(options =>
    options.UseSqlServer(
        Environment.GetEnvironmentVariable("CONNECTION_STRING") 
        ?? builder.Configuration.GetConnectionString("DefaultConnection")));
// Add services to the container.
builder.Services.AddControllersWithViews();
var app = builder.Build();
app.Use(async (context, next) =>
{
    context.Response.Headers.Remove("Content-Security-Policy");
    await next();
});
// Apply pending migrations at startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MessageContext>();
    dbContext.Database.Migrate();  // Apply migrations, create the database if it doesn't exist
}
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Message}/{action=Index}/{id?}");

app.Run();