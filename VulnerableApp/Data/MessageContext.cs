using Microsoft.EntityFrameworkCore;
using VulnerableApp.Models;

namespace VulnerableApp.Data;

public class MessageContext : DbContext
{
    public MessageContext(DbContextOptions<MessageContext> options) : base(options) { }
    public DbSet<Message> Messages { get; set; }
}