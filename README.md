# Domain-Driven Design

An implementation of Domain-Driven Design Tactical Patterns.

## References

- Evans, Eric. "Domain-Driven Design: Tackling Complexity in the Heart of Software"
- Vernon, Vaughn. "Domain-Driven Design Distilled"
- Vernon, Vaughn. "Implementing Domain-Driven Design"

## Tactical Design Patterns

### Entities
Objects defined by their identity, not their attributes.
```csharp
public abstract class Entity<TId> : IEquatable<Entity<TId>>
{
    public TId Id { get; protected set; }
    // Identity-based equality
}
```

### Value Objects
Immutable objects defined by their attributes.
```csharp
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    // Attribute-based equality
}
```

### Aggregates
Consistency boundaries with a single entry point.
```csharp
public class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderItem> _orderItems = new();
    // Only Order can modify OrderItems
    public void AddItem(ProductId productId, ...) { ... }
}
```

### Domain Events
Facts about what happened in the domain.
```csharp
public record OrderSubmittedDomainEvent : DomainEventBase
{
    public OrderId OrderId { get; }
    public decimal TotalAmount { get; }
}
```

### Repository Pattern
Collection-like interface for aggregates.
```csharp
public interface IOrderRepository : IRepository<Order, OrderId>
{
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(CustomerId customerId);
}
```

### Domain Services
Stateless operations that don't belong to entities.
```csharp
public interface IPaymentProcessingService : IDomainService
{
    PaymentValidationResult ValidatePayment(Payment payment);
    Money CalculateProcessingFee(Money amount, PaymentMethod method);
}
```

### Specification Pattern
Encapsulated business rules for **querying and filtering**.
```csharp
public class OrderReadyForProcessingSpecification : Specification<Order>
{
    public override Expression<Func<Order, bool>> ToExpression()
        => order => order.Status == OrderStatus.Paid;
}

// Usage: Filtering/Querying
var overdueOrders = await repository.FindAsync(new OverdueOrderSpecification(24));
var cancellableOrders = await repository.FindAsync(new CancellableOrderSpecification());

// Composable with And, Or, Not
var spec = new MinimumOrderValueSpecification(100) & new CancellableOrderSpecification();
```

### Business Rule Pattern
Encapsulated business rules for **validation and enforcement** with clear error messages.
```csharp
public interface IBusinessRule
{
    bool IsBroken();
    string Message { get; }
    string Code => "BUSINESS_RULE_VIOLATION";
}

public class OrderMustHaveAtLeastOneItemRule : IBusinessRule
{
    private readonly int _itemCount;
    
    public OrderMustHaveAtLeastOneItemRule(int itemCount) => _itemCount = itemCount;
    
    public bool IsBroken() => _itemCount < 1;
    public string Message => "Order must have at least one item before submission.";
    public string Code => "ORDER_EMPTY";
}

// Usage in Aggregate Root
public void Submit()
{
    CheckRule(new OrderMustHaveAtLeastOneItemRule(_orderItems.Count));
    CheckRule(new OrderMustBeInDraftStatusRule(Status));
    // ... proceed with submission
}
```

### Enumeration Pattern
Type-safe, behavior-rich enumerations.
```csharp
public class OrderStatus : Enumeration
{
    public static OrderStatus Draft = new(1, nameof(Draft));
    public static OrderStatus Submitted = new(2, nameof(Submitted));
    
    public bool CanBeCancelled() => this == Draft || this == Submitted;
}
```

### Factory Pattern
Encapsulated object creation.
```csharp
public static Order Create(CustomerId customerId, Address address)
{
    var order = new Order { ... };
    order.RaiseDomainEvent(new OrderCreatedDomainEvent(...));
    return order;
}
```

## 🔄 Integration Between Bounded Contexts (Context Mapping)

### Event Flow: Order → Payment

1. **Order Submitted**: Order service raises `OrderSubmittedDomainEvent`
2. **Domain Event Handler**: Converts to `OrderSubmittedIntegrationEvent`
3. **Event Bus**: Publishes integration event
4. **Payment Handler**: Creates and processes payment
5. **Payment Completed**: Payment service raises `PaymentCompletedDomainEvent`
6. **Integration Event**: `PaymentCompletedIntegrationEvent` published
7. **Order Handler**: Updates order status to Paid

```
┌─────────────────┐                    ┌─────────────────┐
│  Order Service  │                    │ Payment Service │
├─────────────────┤                    ├─────────────────┤
│ Order.Submit()  │                    │                 │
│       │         │                    │                 │
│       ▼         │                    │                 │
│ OrderSubmitted  │ ──Integration──►   │ Create Payment  │
│ DomainEvent     │    Event           │       │         │
│                 │                    │       ▼         │
│                 │                    │ Process Payment │
│                 │                    │       │         │
│                 │                    │       ▼         │
│ MarkAsPaid()    │ ◄──Integration──   │ PaymentCompleted│
│       │         │    Event           │ DomainEvent     │
│       ▼         │                    │                 │
│ Status = Paid   │                    │                 │
└─────────────────┘                    └─────────────────┘
```
## Project Overview

This solution demonstrates a microservices architecture with two bounded contexts:
- **Order Service** - Manages customer orders
- **Payment Service** - Handles payment processing

```
DDD/
├── src/
│   ├── BuildingBlocks/          # Shared DDD building blocks
│   │   ├── BuildingBlocks.Domain/
│   │   └── BuildingBlocks.Integration/
│   └── Services/
│       ├── Order/               # Order Bounded Context
│       │   ├── Order.Domain/
│       │   ├── Order.Application/
│       │   ├── Order.Infrastructure/
│       │   └── Order.API/
│       └── Payment/             # Payment Bounded Context
│           ├── Payment.Domain/
│           ├── Payment.Application/
│           ├── Payment.Infrastructure/
│           └── Payment.API/
└── DDD.sln
```

## Setup

### Run Order Service
```bash
cd src/Services/Order/Order.API
dotnet run
```
Access Swagger UI at: https://localhost:5001/swagger

### Run Payment Service
```bash
cd src/Services/Payment/Payment.API
dotnet run
```
Access Swagger UI at: https://localhost:5002/swagger

### Local Development (Infrastructure Only)

Start infrastructure services (MongoDB):

```bash
docker-compose --profile infra up -d
```

