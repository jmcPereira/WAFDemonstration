using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VulnerableApp.Data;

public class MessageController(MessageContext context) : Controller
{
    public IActionResult Index()
    {
        var messages = context.Messages.OrderByDescending(m => m.Timestamp).ToList();
        return View(messages);
    }

    [HttpPost]
    public IActionResult PostMessage(string owner, string content)
    {
        // Vulnerable to SQL Injection - unsafe use of raw SQL with concatenation
        var query = $"INSERT INTO Messages (Owner, Content, Timestamp) VALUES ('{owner}', '{content}', '{DateTime.UtcNow}')";

        context.Database.ExecuteSqlRaw(query);  // Directly executing the raw query without parameterization.
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult DeleteAll()
    {
        context.Messages.RemoveRange(context.Messages);
        context.SaveChanges();
        return RedirectToAction("Index");
    }
}