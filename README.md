# Domain-Driven Design Microservices Application

A comprehensive implementation of Domain-Driven Design (DDD) patterns from Eric Evans' book "Domain-Driven Design: Tackling Complexity in the Heart of Software" using C# and .NET 10.

## 🎯 Key DDD Principles Demonstrated

1. **Rich Domain Model**: Business logic in domain entities, not services
2. **Encapsulation**: Aggregate roots control access to internal entities
3. **Immutability**: Value objects are immutable
4. **Ubiquitous Language**: Code reflects domain terminology
5. **Persistence Ignorance**: Domain layer has no infrastructure dependencies
6. **Explicit Boundaries**: Clear separation between bounded contexts

## 📖 References

- Evans, Eric. "Domain-Driven Design: Tackling Complexity in the Heart of Software"
- Vernon, Vaughn. "Implementing Domain-Driven Design"
- Microsoft .NET Microservices Architecture Guide

## 📚 DDD Patterns Implemented

### Strategic Patterns

#### 1. Bounded Contexts
Each service represents a separate bounded context with its own:
- Domain model
- Ubiquitous language
- Persistence mechanism
- API

#### 2. Context Mapping
- **Integration Events**: Cross-context communication via events
- **Anti-Corruption Layer**: Integration event handlers translate between contexts

### Tactical Patterns

#### 3. Entities
Objects defined by their identity, not their attributes.
```csharp
public abstract class Entity<TId> : IEquatable<Entity<TId>>
{
    public TId Id { get; protected set; }
    // Identity-based equality
}
```

#### 4. Value Objects
Immutable objects defined by their attributes.
```csharp
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    // Attribute-based equality
}
```

#### 5. Aggregates & Aggregate Roots
Consistency boundaries with a single entry point.
```csharp
public class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderItem> _orderItems = new();
    // Only Order can modify OrderItems
    public void AddItem(ProductId productId, ...) { ... }
}
```

#### 6. Domain Events
Facts about what happened in the domain.
```csharp
public record OrderSubmittedDomainEvent : DomainEventBase
{
    public OrderId OrderId { get; }
    public decimal TotalAmount { get; }
}
```

#### 7. Repository Pattern
Collection-like interface for aggregates.
```csharp
public interface IOrderRepository : IRepository<Order, OrderId>
{
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(CustomerId customerId);
}
```

#### 8. Domain Services
Stateless operations that don't belong to entities.
```csharp
public interface IPaymentProcessingService : IDomainService
{
    PaymentValidationResult ValidatePayment(Payment payment);
    Money CalculateProcessingFee(Money amount, PaymentMethod method);
}
```

#### 9. Specification Pattern
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

#### 10. Business Rule Pattern
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

### Specification vs Business Rule Pattern

> 💡 Think of it this way: **Specification** is a *tester* (tells you if something is true), while **Business Rule** is a *guard* (enforces a policy and explains what went wrong).

| Aspect | 🔍 Specification Pattern | 🛡️ Business Rule Pattern |
|:-------|:-------------------------|:--------------------------|
| **Primary Goal** | Selection & Filtering | Validation & Enforcement |
| **Output** | Simple `boolean` | Result with error message + code |
| **Logic Style** | Declarative | Policy-based |
| **Example** | *"Is this order cancellable?"* | *"Order must have items to submit"* |
| **Composition** | Chainable (`And`, `Or`, `Not`) | Flat list of rules |
| **Use Case** | Repository queries, filtering | Invariant enforcement |
| **Data Access** | Database queries (LINQ expressions) | In-memory validation |
| **Reusability** | High (UI, Repository, Services) | Specific to action/command |
| **Error Handling** | ❌ No error details | ✅ Rich error messages |

**When to use which:**
- Use **Specification** when you need to **filter** collections or check a **state** used in multiple places
- Use **Business Rule** when you need to **validate** a specific action and return a clear reason when blocked

#### 11. Enumeration Pattern
Type-safe, behavior-rich enumerations.
```csharp
public class OrderStatus : Enumeration
{
    public static OrderStatus Draft = new(1, nameof(Draft));
    public static OrderStatus Submitted = new(2, nameof(Submitted));
    
    public bool CanBeCancelled() => this == Draft || this == Submitted;
}
```

#### 12. Factory Pattern
Encapsulated object creation.
```csharp
public static Order Create(CustomerId customerId, Address address)
{
    var order = new Order { ... };
    order.RaiseDomainEvent(new OrderCreatedDomainEvent(...));
    return order;
}
```

## 🔄 Integration Between Bounded Contexts

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
## 🏗️ Project Overview

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

## 🚀 Getting Started

### Prerequisites
- .NET 10.0 SDK
- Visual Studio 2022 or VS Code
- Docker & Docker Compose (optional, for containerized deployment)

### Build
```bash
dotnet build DDD.sln
```

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

### 🐳 Run with Docker

#### Local Development (Infrastructure Only)

Start only infrastructure services (MongoDB) and run APIs from your IDE:

```bash
docker-compose --profile infra up -d
```

Then run Order API and Payment API from Visual Studio or VS Code. MongoDB will be available at `localhost:27017`.

#### Full Stack (All Services)

Start all services (MongoDB, Order API, Payment API):

```bash
docker-compose --profile all up -d
```

This will:
- Start MongoDB on port 27017
- Build and start Order API on http://localhost:5001
- Build and start Payment API on http://localhost:5002

#### Docker Commands

| Command | Description |
|---------|-------------|
| `docker-compose --profile infra up -d` | Start infrastructure only (MongoDB) |
| `docker-compose --profile all up -d` | Start all services |
| `docker-compose --profile all up -d --build` | Rebuild and start all services |
| `docker-compose --profile all down` | Stop all services |
| `docker-compose down -v` | Stop and remove volumes |

## 📋 API Examples

### Create an Order
```http
POST /api/orders
Content-Type: application/json

{
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "shippingAddress": {
    "street": "123 Main St",
    "city": "Seattle",
    "state": "WA",
    "country": "USA",
    "zipCode": "98101"
  },
  "currency": "USD"
}
```

### Add Item to Order
```http
POST /api/orders/{orderId}/items
Content-Type: application/json

{
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "productName": "Laptop",
  "unitPrice": 999.99,
  "quantity": 1
}
```

### Submit Order
```http
POST /api/orders/{orderId}/submit
```

