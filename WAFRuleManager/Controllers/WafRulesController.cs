using Microsoft.AspNetCore.Mvc;
using WAFRuleModels;

namespace WAFRuleManager.Controllers;

public class WafRulesController(WafRuleDbContext context) : Controller
{
    public IActionResult Index()
    {
        return View(context.WafRules.ToList());
    }

    [HttpPost]
    [HttpPost]
    public IActionResult Add([FromForm] string pattern, [FromForm] TrafficDirection direction, [FromForm] WafAction action, [FromForm] string trollReplacement)
    {
        context.WafRules.Add(new WafRule { Pattern = pattern, TrafficDirectionKind = direction, WafAction = action, TrollReplacement = trollReplacement ?? string.Empty });
        context.SaveChanges();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Delete([FromForm] int id)
    {
        var ruleToDelete = context.WafRules.Find(id);

        if (ruleToDelete == null)
        {
            // If the rule doesn't exist, return to the index page
            return RedirectToAction(nameof(Index));
        }

        // Remove the rule from the database
        context.WafRules.Remove(ruleToDelete);
        context.SaveChanges();

        // Redirect to the index page
        return RedirectToAction(nameof(Index));
    }
}