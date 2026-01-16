using BuildingBlocks.Domain.BusinessRules;

namespace Order.Application.BusinessRules;

/// <summary>
/// Business rule: All items in the imported order must have valid quantities.
/// </summary>
public class ImportedItemsMustHaveValidQuantitiesRule : IBusinessRule
{
    private readonly IEnumerable<int> _quantities;

    public ImportedItemsMustHaveValidQuantitiesRule(IEnumerable<int> quantities)
    {
        _quantities = quantities;
    }

    public bool IsBroken() => _quantities.Any(qty => qty <= 0);

    public string Message => "All items must have a quantity greater than zero.";
    
    public string Code => "INVALID_ITEM_QUANTITY";
}
