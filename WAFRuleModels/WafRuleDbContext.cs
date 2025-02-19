using Microsoft.EntityFrameworkCore;

namespace WAFRuleModels;

public class WafRuleDbContext(DbContextOptions<WafRuleDbContext> options) : DbContext(options)
{
    public DbSet<WafRule> WafRules { get; set; }
}