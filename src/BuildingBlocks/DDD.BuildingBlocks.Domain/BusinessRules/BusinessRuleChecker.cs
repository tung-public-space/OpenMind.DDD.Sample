namespace BuildingBlocks.Domain.BusinessRules;

/// <summary>
/// Static helper for checking business rules outside of entities.
/// Useful in domain services or application layer validation.
/// </summary>
public static class BusinessRuleChecker
{
    public static void CheckRule(IBusinessRule rule)
    {
        if (rule.IsBroken())
        {
            throw new BusinessRuleValidationException(rule);
        }
    }
    
    public static void CheckRules(params IBusinessRule[] rules)
    {
        foreach (var rule in rules)
        {
            CheckRule(rule);
        }
    }
    
    public static IReadOnlyList<IBusinessRule> GetBrokenRules(params IBusinessRule[] rules)
    {
        return rules.Where(r => r.IsBroken()).ToList();
    }
    
    public static void ValidateAll(params IBusinessRule[] rules)
    {
        var brokenRules = GetBrokenRules(rules);
        if (brokenRules.Count > 0)
        {
            throw new AggregateBusinessRuleValidationException(brokenRules);
        }
    }
}

public class AggregateBusinessRuleValidationException(IReadOnlyList<IBusinessRule> brokenRules) 
    : DomainException("MULTIPLE_RULES_VIOLATED", string.Join("; ", brokenRules.Select(r => r.Message)))
{
    public IReadOnlyList<IBusinessRule> BrokenRules { get; } = brokenRules;
}
