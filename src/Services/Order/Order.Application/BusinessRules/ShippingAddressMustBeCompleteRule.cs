using BuildingBlocks.Domain.BusinessRules;

namespace Order.Application.BusinessRules;

/// <summary>
/// Business rule: Shipping address must be complete.
/// </summary>
public class ShippingAddressMustBeCompleteRule : IBusinessRule
{
    private readonly string? _street;
    private readonly string? _city;
    private readonly string? _country;
    private readonly string? _zipCode;

    public ShippingAddressMustBeCompleteRule(
        string? street, 
        string? city, 
        string? country, 
        string? zipCode)
    {
        _street = street;
        _city = city;
        _country = country;
        _zipCode = zipCode;
    }

    public bool IsBroken() => 
        string.IsNullOrWhiteSpace(_street) ||
        string.IsNullOrWhiteSpace(_city) ||
        string.IsNullOrWhiteSpace(_country) ||
        string.IsNullOrWhiteSpace(_zipCode);

    public string Message => "Shipping address must include street, city, country, and zip code.";
    
    public string Code => "INCOMPLETE_SHIPPING_ADDRESS";
}
