using BuildingBlocks.Domain.BusinessRules;

namespace Order.Application.BusinessRules;

/// <summary>
/// Business rule: Imported order must have at least one item.
/// </summary>
public class ImportedOrderMustHaveItemsRule : IBusinessRule
{
    private readonly int _itemCount;

    public ImportedOrderMustHaveItemsRule(int itemCount)
    {
        _itemCount = itemCount;
    }

    public bool IsBroken() => _itemCount < 1;

    public string Message => "Imported order must contain at least one item.";
    
    public string Code => "IMPORT_ORDER_EMPTY";
}
