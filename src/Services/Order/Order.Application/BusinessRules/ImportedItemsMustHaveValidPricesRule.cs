using BuildingBlocks.Domain.BusinessRules;

namespace Order.Application.BusinessRules;

/// <summary>
/// Business rule: All items in the imported order must have valid prices.
/// </summary>
public class ImportedItemsMustHaveValidPricesRule : IBusinessRule
{
    private readonly IEnumerable<decimal> _unitPrices;

    public ImportedItemsMustHaveValidPricesRule(IEnumerable<decimal> unitPrices)
    {
        _unitPrices = unitPrices;
    }

    public bool IsBroken() => _unitPrices.Any(price => price <= 0);

    public string Message => "All items must have a positive unit price.";
    
    public string Code => "INVALID_ITEM_PRICE";
}
