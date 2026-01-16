using BuildingBlocks.Domain.BusinessRules;

namespace Order.Application.BusinessRules;

/// <summary>
/// Business rule: Customer ID must be provided for order creation.
/// </summary>
public class CustomerIdMustBeProvidedRule : IBusinessRule
{
    private readonly Guid _customerId;

    public CustomerIdMustBeProvidedRule(Guid customerId)
    {
        _customerId = customerId;
    }

    public bool IsBroken() => _customerId == Guid.Empty;

    public string Message => "Customer ID is required to create an order.";
    
    public string Code => "CUSTOMER_ID_REQUIRED";
}
