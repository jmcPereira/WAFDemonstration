namespace WAFRuleModels;

public record WafRule
{
    public int Id { get; set; }
    public string Pattern { get; set; }
    public TrafficDirection TrafficDirectionKind { get; set; }
    public WafAction WafAction { get; set; }
    public string TrollReplacement { get; set; }
}