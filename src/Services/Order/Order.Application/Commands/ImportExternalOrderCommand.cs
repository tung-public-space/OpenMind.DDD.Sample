using BuildingBlocks.Domain.BusinessRules;
using MediatR;
using Order.Application.AntiCorruption;
using Order.Application.BusinessRules;
using Order.Domain.Factories;
using Order.Domain.Repositories;

namespace Order.Application.Commands;

public record ImportExternalOrderCommand : IRequest<Guid>
{
    public string ExternalOrderId { get; init; } = null!;
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = null!;
    public string ShippingStreet { get; init; } = null!;
    public string ShippingCity { get; init; } = null!;
    public string ShippingState { get; init; } = null!;
    public string ShippingCountry { get; init; } = null!;
    public string ShippingZipCode { get; init; } = null!;
    public string Currency { get; init; } = "USD";
    public List<ImportExternalOrderItemCommand> Items { get; init; } = new();
    public string? Notes { get; init; }
}

public record ImportExternalOrderItemCommand
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
}

/// <summary>
/// Application Service - orchestrates the import flow:
/// 1. Validate input using BusinessRuleChecker (application-level validation)
/// 2. Translate external data (ACL)
/// 3. Create order via domain factory (domain invariants)
/// 4. Persist aggregate
/// 
/// NO business logic here - only orchestration and input validation.
/// </summary>
public class ImportExternalOrderCommandHandler(
    IOrderRepository orderRepository,
    ExternalOrderTranslator translator) 
    : IRequestHandler<ImportExternalOrderCommand, Guid>
{
    public async Task<Guid> Handle(ImportExternalOrderCommand request, CancellationToken cancellationToken)
    {
        BusinessRuleChecker.ValidateAll(
            new CustomerIdMustBeProvidedRule(request.CustomerId),
            new ShippingAddressMustBeCompleteRule(
                request.ShippingStreet,
                request.ShippingCity,
                request.ShippingCountry,
                request.ShippingZipCode),
            new ImportedOrderMustHaveItemsRule(request.Items.Count),
            new ImportedItemsMustHaveValidPricesRule(request.Items.Select(i => i.UnitPrice)),
            new ImportedItemsMustHaveValidQuantitiesRule(request.Items.Select(i => i.Quantity))
        );

        var externalDto = new ExternalOrderDto(
            ExternalOrderId: request.ExternalOrderId,
            CustomerId: request.CustomerId,
            CustomerName: request.CustomerName,
            ShippingStreet: request.ShippingStreet,
            ShippingCity: request.ShippingCity,
            ShippingState: request.ShippingState,
            ShippingCountry: request.ShippingCountry,
            ShippingZipCode: request.ShippingZipCode,
            Currency: request.Currency,
            Items: request.Items.Select(i => new ExternalOrderItemDto(
                ProductId: i.ProductId,
                ProductName: i.ProductName,
                UnitPrice: i.UnitPrice,
                Quantity: i.Quantity
            )).ToList(),
            Notes: request.Notes
        );

        var createOrderData = translator.Translate(externalDto);

        var factory = new OrderFactory(createOrderData);
        var order = factory.Create();

        await orderRepository.AddAsync(order, cancellationToken);
        await orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return order.Id.Value;
    }
}
