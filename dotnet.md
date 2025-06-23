# Development Guidelines for .NET

## Core Philosophy

**TEST-DRIVEN DEVELOPMENT IS NON-NEGOTIABLE.** Every single line of production code must be written in response to a failing test. No exceptions. This is not a suggestion or a preference - it is the fundamental practice that enables all other principles in this document.

I follow Test-Driven Development (TDD) with a strong emphasis on behavior-driven testing and functional programming principles. All work should be done in small, incremental changes that maintain a working state throughout development.

## Quick Reference

**Key Principles:**

- Write tests first (TDD)
- Test behavior, not implementation
- Nullable reference types enabled
- Immutable data only
- Small, pure functions
- C# 12+ features and .NET 9
- Use real models/DTOs in tests, never redefine them

**Preferred Tools:**

- **Language**: C# 12+ (.NET 9)
- **Testing**: xUnit + FluentAssertions + Testcontainers
- **State Management**: Prefer immutable patterns and records
- **Validation**: FluentValidation
- **Serialization**: System.Text.Json

## Testing Principles

### Behavior-Driven Testing

- **No "unit tests"** - this term is not helpful. Tests should verify expected behavior, treating implementation as a black box
- Test through the public API exclusively - internals should be invisible to tests
- No 1:1 mapping between test files and implementation files
- Tests that examine internal implementation details are wasteful and should be avoided
- **Coverage targets**: 100% coverage should be expected at all times, but these tests must ALWAYS be based on business behaviour, not implementation details
- Tests must document expected business behaviour

### Testing Tools

- **xUnit** for testing framework
- **FluentAssertions** for readable assertions
- **Testcontainers** for integration testing with real dependencies
- **Microsoft.AspNetCore.Mvc.Testing** for API testing
- **Moq** for mocking when absolutely necessary (prefer real implementations)
- All test code must follow the same C# standards as production code

### Test Organization

```
src/
  Features/
    Payment/
      PaymentProcessor.cs
      PaymentValidator.cs
      PaymentProcessor.Tests.cs // The validator is an implementation detail. Validation is fully covered, but by testing the expected business behaviour
```

### Test Data Pattern

Use builder pattern with fluent interfaces for test data:

```csharp
public class PaymentRequestBuilder
{
    private decimal _amount = 100m;
    private string _currency = "GBP";
    private string _cardId = "card_123";
    private string _customerId = "cust_456";
    private string? _description;
    private Dictionary<string, object>? _metadata;
    private string? _idempotencyKey;
    private AddressDetails _addressDetails = new AddressDetailsBuilder().Build();
    private PayingCardDetails _payingCardDetails = new PayingCardDetailsBuilder().Build();

    public PaymentRequestBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    public PaymentRequestBuilder WithCurrency(string currency)
    {
        _currency = currency;
        return this;
    }

    public PaymentRequestBuilder WithCardId(string cardId)
    {
        _cardId = cardId;
        return this;
    }

    public PaymentRequestBuilder WithCustomerId(string customerId)
    {
        _customerId = customerId;
        return this;
    }

    public PaymentRequestBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public PaymentRequestBuilder WithMetadata(Dictionary<string, object> metadata)
    {
        _metadata = metadata;
        return this;
    }

    public PaymentRequestBuilder WithIdempotencyKey(string idempotencyKey)
    {
        _idempotencyKey = idempotencyKey;
        return this;
    }

    public PaymentRequestBuilder WithAddressDetails(AddressDetails addressDetails)
    {
        _addressDetails = addressDetails;
        return this;
    }

    public PaymentRequestBuilder WithPayingCardDetails(PayingCardDetails payingCardDetails)
    {
        _payingCardDetails = payingCardDetails;
        return this;
    }

    public PaymentRequest Build()
    {
        return new PaymentRequest(
            _amount,
            _currency,
            _cardId,
            _customerId,
            _description,
            _metadata,
            _idempotencyKey,
            _addressDetails,
            _payingCardDetails
        );
    }
}

public class AddressDetailsBuilder
{
    private string _houseNumber = "123";
    private string? _houseName = "Test House";
    private string _addressLine1 = "Test Address Line 1";
    private string? _addressLine2 = "Test Address Line 2";
    private string _city = "Test City";
    private string _postcode = "SW1A 1AA";

    public AddressDetailsBuilder WithHouseNumber(string houseNumber)
    {
        _houseNumber = houseNumber;
        return this;
    }

    public AddressDetailsBuilder WithHouseName(string? houseName)
    {
        _houseName = houseName;
        return this;
    }

    public AddressDetailsBuilder WithAddressLine1(string addressLine1)
    {
        _addressLine1 = addressLine1;
        return this;
    }

    public AddressDetailsBuilder WithAddressLine2(string? addressLine2)
    {
        _addressLine2 = addressLine2;
        return this;
    }

    public AddressDetailsBuilder WithCity(string city)
    {
        _city = city;
        return this;
    }

    public AddressDetailsBuilder WithPostcode(string postcode)
    {
        _postcode = postcode;
        return this;
    }

    public AddressDetails Build()
    {
        return new AddressDetails(
            _houseNumber,
            _houseName,
            _addressLine1,
            _addressLine2,
            _city,
            _postcode
        );
    }
}

// Usage in tests
var paymentRequest = new PaymentRequestBuilder()
    .WithAmount(250m)
    .WithCurrency("USD")
    .WithMetadata(new Dictionary<string, object> { ["orderId"] = "order_789" })
    .WithAddressDetails(new AddressDetailsBuilder()
        .WithCity("London")
        .WithPostcode("E1 6AN")
        .Build())
    .Build();
```

Key principles:

- Always return complete objects with sensible defaults
- Use fluent interfaces for readable test setup
- Build incrementally - extract nested object builders as needed
- Compose builders for complex objects
- Make builders immutable by returning new instances

## C# and .NET Guidelines

### Project Structure

```
src/
  YourApp.Api/              # Web API project
  YourApp.Application/      # Application layer (use cases, services)
  YourApp.Domain/           # Domain models, entities, value objects
  YourApp.Infrastructure/   # Data access, external services
  YourApp.Contracts/        # DTOs, requests, responses
tests/
  YourApp.Api.Tests/        # API integration tests
  YourApp.Application.Tests/ # Application layer tests
  YourApp.Domain.Tests/     # Domain logic tests
  YourApp.Infrastructure.Tests/ # Infrastructure tests
```

### C# Language Features

#### Records and Immutability

Use records for data transfer objects and value objects:

```csharp
// Good - Immutable record
public record PaymentRequest(
    decimal Amount,
    string Currency,
    string CardId,
    string CustomerId,
    string? Description = null,
    Dictionary<string, object>? Metadata = null,
    string? IdempotencyKey = null,
    AddressDetails AddressDetails,
    PayingCardDetails PayingCardDetails
);

public record AddressDetails(
    string HouseNumber,
    string? HouseName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string Postcode
);

// For domain entities with behavior
public class Payment
{
    public PaymentId Id { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public PaymentStatus Status { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? ProcessedAt { get; private set; }

    public Payment(PaymentId id, decimal amount, string currency)
    {
        Id = id;
        Amount = amount;
        Currency = currency;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsProcessed()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidOperationException("Payment can only be processed when pending");
        }

        Status = PaymentStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
    }
}
```

#### Nullable Reference Types

Always enable nullable reference types:

```xml
<!-- In .csproj -->
<PropertyGroup>
  <Nullable>enable</Nullable>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <WarningsAsErrors />
</PropertyGroup>
```

```csharp
// Good - Explicit nullability
public class PaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Payment?> GetPaymentAsync(PaymentId id, CancellationToken cancellationToken = default)
    {
        return await _paymentRepository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<Payment> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var payment = new Payment(PaymentId.New(), request.Amount, request.Currency);
        await _paymentRepository.AddAsync(payment, cancellationToken);
        
        return payment;
    }
}
```

#### Result Pattern

Use Result pattern for error handling:

```csharp
public abstract record Result<T>
{
    public abstract bool IsSuccess { get; }
    public abstract bool IsFailure => !IsSuccess;
}

public sealed record Success<T>(T Value) : Result<T>
{
    public override bool IsSuccess => true;
}

public sealed record Failure<T>(string Error, Exception? Exception = null) : Result<T>
{
    public override bool IsSuccess => false;
}

// Extension methods for fluent usage
public static class ResultExtensions
{
    public static Result<T> Success<T>(T value) => new Success<T>(value);
    public static Result<T> Failure<T>(string error, Exception? exception = null) => new Failure<T>(error, exception);

    public static Result<TResult> Map<T, TResult>(this Result<T> result, Func<T, TResult> mapper)
    {
        return result switch
        {
            Success<T> success => Success(mapper(success.Value)),
            Failure<T> failure => Failure<TResult>(failure.Error, failure.Exception),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    public static async Task<Result<TResult>> MapAsync<T, TResult>(
        this Result<T> result, 
        Func<T, Task<TResult>> mapper)
    {
        return result switch
        {
            Success<T> success => Success(await mapper(success.Value)),
            Failure<T> failure => Failure<TResult>(failure.Error, failure.Exception),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }

    public static Result<TResult> FlatMap<T, TResult>(this Result<T> result, Func<T, Result<TResult>> mapper)
    {
        return result switch
        {
            Success<T> success => mapper(success.Value),
            Failure<T> failure => Failure<TResult>(failure.Error, failure.Exception),
            _ => throw new InvalidOperationException("Unknown result type")
        };
    }
}

// Usage
public async Task<Result<Payment>> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
{
    var validationResult = await ValidatePaymentRequestAsync(request, cancellationToken);
    if (validationResult.IsFailure)
    {
        return ResultExtensions.Failure<Payment>(validationResult.Error);
    }

    var authorizationResult = await AuthorizePaymentAsync(request, cancellationToken);
    if (authorizationResult.IsFailure)
    {
        return ResultExtensions.Failure<Payment>(authorizationResult.Error);
    }

    var payment = new Payment(PaymentId.New(), request.Amount, request.Currency);
    await _paymentRepository.AddAsync(payment, cancellationToken);

    return ResultExtensions.Success(payment);
}
```

#### Strongly Typed IDs

Use strongly typed IDs to prevent primitive obsession:

```csharp
public readonly record struct PaymentId(Guid Value)
{
    public static PaymentId New() => new(Guid.NewGuid());
    public static PaymentId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}

public readonly record struct CustomerId(Guid Value)
{
    public static CustomerId New() => new(Guid.NewGuid());
    public static CustomerId Parse(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}

// Usage prevents mixing up IDs
public async Task<Payment?> GetPaymentAsync(PaymentId paymentId, CancellationToken cancellationToken = default)
{
    // Compiler prevents passing CustomerId here
    return await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
}
```

### Validation with FluentValidation

```csharp
public class PaymentRequestValidator : AbstractValidator<PaymentRequest>
{
    public PaymentRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(10000)
            .WithMessage("Amount cannot exceed £10,000");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(BeValidCurrency)
            .WithMessage("Currency must be a valid ISO currency code");

        RuleFor(x => x.CardId)
            .NotEmpty()
            .Length(16)
            .WithMessage("Card ID must be 16 characters");

        RuleFor(x => x.AddressDetails)
            .NotNull()
            .SetValidator(new AddressDetailsValidator());

        RuleFor(x => x.PayingCardDetails)
            .NotNull()
            .SetValidator(new PayingCardDetailsValidator());
    }

    private static bool BeValidCurrency(string currency)
    {
        var validCurrencies = new[] { "GBP", "USD", "EUR" };
        return validCurrencies.Contains(currency);
    }
}

public class AddressDetailsValidator : AbstractValidator<AddressDetails>
{
    public AddressDetailsValidator()
    {
        RuleFor(x => x.HouseNumber)
            .NotEmpty()
            .WithMessage("House number is required");

        RuleFor(x => x.AddressLine1)
            .NotEmpty()
            .WithMessage("Address line 1 is required");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required");

        RuleFor(x => x.Postcode)
            .NotEmpty()
            .Matches(@"^[A-Z]{1,2}\d[A-Z\d]? ?\d[A-Z]{2}$")
            .WithMessage("Postcode must be a valid UK postcode");
    }
}
```

#### Model Validation in Tests

**CRITICAL**: Tests must use real validators and models from the main project, not redefine their own.

```csharp
// ❌ WRONG - Defining validators in test files
public class TestPaymentRequestValidator : AbstractValidator<PaymentRequest>
{
    public TestPaymentRequestValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        // ... other rules
    }
}

// ✅ CORRECT - Import validators from the main project
using YourApp.Application.Validators;

[Fact]
public async Task ProcessPayment_ShouldFailValidation_WhenAmountIsNegative()
{
    // Arrange
    var paymentRequest = new PaymentRequestBuilder()
        .WithAmount(-100m)
        .Build();

    var validator = new PaymentRequestValidator();
    var paymentService = new PaymentService(_paymentRepository, validator, _logger);

    // Act
    var result = await paymentService.ProcessPaymentAsync(paymentRequest);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Should().BeOfType<Failure<Payment>>();
    var failure = (Failure<Payment>)result;
    failure.Error.Should().Contain("Amount must be greater than zero");
}
```

## Code Style

### Functional Programming

Follow a "functional light" approach:

- **No data mutation** - work with immutable data structures
- **Pure functions** wherever possible
- **Composition** as the primary mechanism for code reuse
- Avoid heavy FP abstractions unless there's clear advantage
- Use LINQ methods over imperative loops

#### Examples of Functional Patterns

```csharp
// Good - Pure function with immutable updates
public static Order ApplyDiscount(Order order, decimal discountPercent)
{
    var discountMultiplier = (100 - discountPercent) / 100;
    
    var discountedItems = order.Items
        .Select(item => item with { Price = item.Price * discountMultiplier })
        .ToList();

    var newTotalPrice = discountedItems.Sum(item => item.Price);

    return order with 
    { 
        Items = discountedItems,
        TotalPrice = newTotalPrice
    };
}

// Good - Composition over complex logic
public static ProcessedOrder ProcessOrder(Order order)
{
    return order
        .Pipe(ValidateOrder)
        .Pipe(ApplyPromotions)
        .Pipe(CalculateTax)
        .Pipe(AssignWarehouse);
}

// Extension method for pipeline
public static class FunctionalExtensions
{
    public static TResult Pipe<T, TResult>(this T input, Func<T, TResult> function)
    {
        return function(input);
    }
}

// Good - LINQ over imperative loops
public static decimal CalculateOrderTotal(IEnumerable<OrderItem> items)
{
    return items
        .Where(item => item.IsActive)
        .Sum(item => item.Price * item.Quantity);
}

// Good - Immutable updates with records
public static PaymentStatus UpdatePaymentStatus(PaymentStatus current, PaymentEvent paymentEvent)
{
    return paymentEvent switch
    {
        PaymentAuthorized => current with { Status = "Authorized", AuthorizedAt = DateTime.UtcNow },
        PaymentCaptured => current with { Status = "Captured", CapturedAt = DateTime.UtcNow },
        PaymentFailed failure => current with { Status = "Failed", FailureReason = failure.Reason },
        _ => current
    };
}
```

### Code Structure

- **No nested if/else statements** - use early returns, guard clauses, or pattern matching
- **Avoid deep nesting** in general (max 2 levels)
- Keep methods small and focused on a single responsibility
- Prefer flat, readable code over clever abstractions

### Naming Conventions

- **Methods**: `PascalCase`, verb-based (e.g., `CalculateTotal`, `ValidatePayment`)
- **Properties**: `PascalCase` (e.g., `PaymentAmount`, `CustomerDetails`)
- **Fields**: `_camelCase` for private fields
- **Constants**: `PascalCase` for public constants, `_camelCase` for private constants
- **Types**: `PascalCase` (e.g., `PaymentRequest`, `UserProfile`)
- **Files**: `PascalCase.cs` for all C# files
- **Test files**: `*.Tests.cs`

### No Comments in Code

Code should be self-documenting through clear naming and structure. Comments indicate that the code itself is not clear enough.

```csharp
// Avoid: Comments explaining what the code does
public static decimal CalculateDiscount(decimal price, Customer customer)
{
    // Check if customer is premium
    if (customer.Tier == CustomerTier.Premium)
    {
        // Apply 20% discount for premium customers
        return price * 0.8m;
    }
    // Regular customers get 10% discount
    return price * 0.9m;
}

// Good: Self-documenting code with clear names
private const decimal PremiumDiscountMultiplier = 0.8m;
private const decimal StandardDiscountMultiplier = 0.9m;

private static bool IsPremiumCustomer(Customer customer)
{
    return customer.Tier == CustomerTier.Premium;
}

public static decimal CalculateDiscount(decimal price, Customer customer)
{
    var discountMultiplier = IsPremiumCustomer(customer)
        ? PremiumDiscountMultiplier
        : StandardDiscountMultiplier;

    return price * discountMultiplier;
}
```

**Exception**: XML documentation comments for public APIs are acceptable when generating documentation, but the code should still be self-explanatory without them.

### Dependency Injection and Options Pattern

Use the built-in DI container and options pattern:

```csharp
// Configuration
public class PaymentOptions
{
    public const string SectionName = "Payment";
    
    public decimal MaxAmount { get; set; } = 10000m;
    public string[] AcceptedCurrencies { get; set; } = Array.Empty<string>();
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
}

// Service registration
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PaymentOptions>(
            configuration.GetSection(PaymentOptions.SectionName));

        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IValidator<PaymentRequest>, PaymentRequestValidator>();
        
        services.AddHttpClient<IPaymentGateway, PaymentGateway>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PaymentOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
        });

        return services;
    }
}

// Service implementation
public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IValidator<PaymentRequest> _validator;
    private readonly ILogger<PaymentService> _logger;
    private readonly PaymentOptions _options;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IValidator<PaymentRequest> validator,
        ILogger<PaymentService> logger,
        IOptions<PaymentOptions> options)
    {
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<Result<Payment>> ProcessPaymentAsync(
        PaymentRequest request, 
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            return ResultExtensions.Failure<Payment>($"Validation failed: {errors}");
        }

        if (request.Amount > _options.MaxAmount)
        {
            return ResultExtensions.Failure<Payment>($"Amount exceeds maximum allowed: {_options.MaxAmount}");
        }

        // Process payment...
        return ResultExtensions.Success(new Payment(PaymentId.New(), request.Amount, request.Currency));
    }
}
```

## Development Workflow

### TDD Process - THE FUNDAMENTAL PRACTICE

**CRITICAL**: TDD is not optional. Every feature, every bug fix, every change MUST follow this process:

Follow Red-Green-Refactor strictly:

1. **Red**: Write a failing test for the desired behavior. NO PRODUCTION CODE until you have a failing test.
2. **Green**: Write the MINIMUM code to make the test pass. Resist the urge to write more than needed.
3. **Refactor**: Assess the code for improvement opportunities. If refactoring would add value, clean up the code while keeping tests green. If the code is already clean and expressive, move on.

#### TDD Example Workflow

```csharp
// Step 1: Red - Start with the simplest behavior
[Fact]
public void ProcessOrder_ShouldCalculateTotal_WithShippingCost()
{
    // Arrange
    var order = new OrderBuilder()
        .WithItems(new OrderItem(30m, 1))
        .WithShippingCost(5.99m)
        .Build();

    // Act
    var processed = ProcessOrder(order);

    // Assert
    processed.Total.Should().Be(35.99m);
    processed.ShippingCost.Should().Be(5.99m);
}

// Step 2: Green - Minimal implementation
public static ProcessedOrder ProcessOrder(Order order)
{
    var itemsTotal = order.Items.Sum(item => item.Price * item.Quantity);
    
    return new ProcessedOrder
    {
        Items = order.Items,
        ShippingCost = order.ShippingCost,
        Total = itemsTotal + order.ShippingCost
    };
}

// Step 3: Red - Add test for free shipping behavior
[Fact]
public void ProcessOrder_ShouldApplyFreeShipping_ForOrdersOver50()
{
    // Arrange
    var order = new OrderBuilder()
        .WithItems(new OrderItem(60m, 1))
        .WithShippingCost(5.99m)
        .Build();

    // Act
    var processed = ProcessOrder(order);

    // Assert
    processed.ShippingCost.Should().Be(0m);
    processed.Total.Should().Be(60m);
}

// Step 4: Green - NOW we can add the conditional because both paths are tested
public static ProcessedOrder ProcessOrder(Order order)
{
    var itemsTotal = order.Items.Sum(item => item.Price * item.Quantity);
    var shippingCost = itemsTotal > 50m ? 0m : order.ShippingCost;

    return new ProcessedOrder
    {
        Items = order.Items,
        ShippingCost = shippingCost,
        Total = itemsTotal + shippingCost
    };
}

// Step 5: Refactor - Extract constants and improve readability
private const decimal FreeShippingThreshold = 50m;

private static decimal CalculateItemsTotal(IEnumerable<OrderItem> items)
{
    return items.Sum(item => item.Price * item.Quantity);
}

private static bool QualifiesForFreeShipping(decimal itemsTotal)
{
    return itemsTotal > FreeShippingThreshold;
}

public static ProcessedOrder ProcessOrder(Order order)
{
    var itemsTotal = CalculateItemsTotal(order.Items);
    var shippingCost = QualifiesForFreeShipping(itemsTotal) ? 0m : order.ShippingCost;

    return new ProcessedOrder
    {
        Items = order.Items,
        ShippingCost = shippingCost,
        Total = itemsTotal + shippingCost
    };
}
```

### Testing Patterns

#### xUnit and FluentAssertions

```csharp
public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepositoryMock;
    private readonly Mock<ILogger<PaymentService>> _loggerMock;
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _paymentRepositoryMock = new Mock<IPaymentRepository>();
        _loggerMock = new Mock<ILogger<PaymentService>>();
        
        var options = Options.Create(new PaymentOptions
        {
            MaxAmount = 10000m,
            AcceptedCurrencies = new[] { "GBP", "USD", "EUR" }
        });

        _paymentService = new PaymentService(
            _paymentRepositoryMock.Object,
            new PaymentRequestValidator(),
            _loggerMock.Object,
            options);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldReturnFailure_WhenAmountIsNegative()
    {
        // Arrange
        var request = new PaymentRequestBuilder()
            .WithAmount(-100m)
            .Build();

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Should().BeOfType<Failure<Payment>>();
        
        var failure = result as Failure<Payment>;
        failure!.Error.Should().Contain("Amount must be greater than zero");
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public async Task ProcessPaymentAsync_ShouldReturnSuccess_WhenRequestIsValid(decimal amount)
    {
        // Arrange
        var request = new PaymentRequestBuilder()
            .WithAmount(amount)
            .Build();

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Should().BeOfType<Success<Payment>>();
        
        var success = result as Success<Payment>;
        success!.Value.Amount.Should().Be(amount);
    }
}
```

#### Integration Testing with Testcontainers (Completion)

```csharp
public class PaymentApiTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly PostgreSqlContainer _postgres;

    public PaymentApiTests()
    {
        _postgres = new PostgreSqlBuilder()
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpass")
            .Build();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Replace the database connection
                    services.RemoveAll<DbContextOptions<PaymentDbContext>>();
                    services.AddDbContext<PaymentDbContext>(options =>
                        options.UseNpgsql(_postgres.GetConnectionString()));
                });
            });

        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        
        // Run migrations
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _factory.DisposeAsync();
        _client.Dispose();
    }

    [Fact]
    public async Task PostPayment_ShouldReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var paymentRequest = new PaymentRequestBuilder()
            .WithAmount(150m)
            .WithCurrency("GBP")
            .Build();

        var json = JsonSerializer.Serialize(paymentRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/payments", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var createdPayment = JsonSerializer.Deserialize<PaymentResponse>(responseContent);
        
        createdPayment!.Amount.Should().Be(150m);
        createdPayment.Currency.Should().Be("GBP");
        createdPayment.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task PostPayment_ShouldReturnBadRequest_WhenAmountIsInvalid()
    {
        // Arrange
        var paymentRequest = new PaymentRequestBuilder()
            .WithAmount(-50m)
            .Build();

        var json = JsonSerializer.Serialize(paymentRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/payments", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Amount must be greater than zero");
    }
}
```

#### Testing with Redis and Message Queues

```csharp
public class PaymentEventProcessorTests : IAsyncLifetime
{
    private readonly RedisContainer _redis;
    private readonly RabbitMqContainer _rabbitMq;
    private readonly IServiceCollection _services;
    private readonly ServiceProvider _serviceProvider;

    public PaymentEventProcessorTests()
    {
        _redis = new RedisBuilder().Build();
        _rabbitMq = new RabbitMqBuilder().Build();

        _services = new ServiceCollection();
        ConfigureServices(_services);
        _serviceProvider = _services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging();
        services.AddSingleton<IConnectionMultiplexer>(provider =>
            ConnectionMultiplexer.Connect(_redis.GetConnectionString()));
        
        services.AddSingleton<IConnection>(provider =>
        {
            var factory = new ConnectionFactory { Uri = new Uri(_rabbitMq.GetConnectionString()) };
            return factory.CreateConnection();
        });

        services.AddScoped<IPaymentEventProcessor, PaymentEventProcessor>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
    }

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();
        await _rabbitMq.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await _redis.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    [Fact]
    public async Task ProcessPaymentEvent_ShouldUpdateCache_WhenPaymentCompleted()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IPaymentEventProcessor>();
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        
        var paymentId = PaymentId.New();
        var paymentEvent = new PaymentCompletedEvent(paymentId, 100m, "GBP", DateTime.UtcNow);

        // Act
        await processor.ProcessAsync(paymentEvent);

        // Assert
        var database = redis.GetDatabase();
        var cachedPayment = await database.StringGetAsync($"payment:{paymentId}");
        
        cachedPayment.HasValue.Should().BeTrue();
        var payment = JsonSerializer.Deserialize<PaymentCache>(cachedPayment!);
        payment!.Status.Should().Be("Completed");
    }
}
```

## API Development and Controllers

### Minimal APIs (Preferred for Simple Endpoints)

Use Minimal APIs for straightforward CRUD operations and simple endpoints:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddPaymentServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Payment endpoints
app.MapPost("/api/payments", async (
    PaymentRequest request,
    IPaymentService paymentService,
    IValidator<PaymentRequest> validator,
    CancellationToken cancellationToken) =>
{
    var validationResult = await validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(validationResult.Errors);
    }

    var result = await paymentService.ProcessPaymentAsync(request, cancellationToken);
    
    return result switch
    {
        Success<Payment> success => Results.Created($"/api/payments/{success.Value.Id}", success.Value),
        Failure<Payment> failure => Results.BadRequest(new { Error = failure.Error }),
        _ => Results.Problem("An unexpected error occurred")
    };
})
.WithName("CreatePayment")
.WithOpenApi();

app.MapGet("/api/payments/{id:guid}", async (
    Guid id,
    IPaymentService paymentService,
    CancellationToken cancellationToken) =>
{
    var paymentId = new PaymentId(id);
    var payment = await paymentService.GetPaymentAsync(paymentId, cancellationToken);
    
    return payment is not null 
        ? Results.Ok(payment) 
        : Results.NotFound();
})
.WithName("GetPayment")
.WithOpenApi();

app.MapGet("/api/payments", async (
    IPaymentService paymentService,
    int page = 1,
    int pageSize = 20,
    CancellationToken cancellationToken = default) =>
{
    var payments = await paymentService.GetPaymentsAsync(page, pageSize, cancellationToken);
    return Results.Ok(payments);
})
.WithName("GetPayments")
.WithOpenApi();

app.Run();
```

### Controller-Based APIs (For Complex Logic)

Use controllers when you need more complex routing, filters, or action-specific behavior:

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IValidator<PaymentRequest> _validator;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        IValidator<PaymentRequest> validator,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new payment
    /// </summary>
    /// <param name="request">Payment details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created payment</returns>
    [HttpPost]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentResponse>> CreatePayment(
        [FromBody] PaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var problemDetails = new ValidationProblemDetails();
            foreach (var error in validationResult.Errors)
            {
                problemDetails.Errors.Add(error.PropertyName, new[] { error.ErrorMessage });
            }
            return BadRequest(problemDetails);
        }

        var result = await _paymentService.ProcessPaymentAsync(request, cancellationToken);

        return result switch
        {
            Success<Payment> success => CreatedAtAction(
                nameof(GetPayment),
                new { id = success.Value.Id.Value },
                PaymentResponse.FromPayment(success.Value)),
            Failure<Payment> failure => BadRequest(new ProblemDetails
            {
                Title = "Payment processing failed",
                Detail = failure.Error,
                Status = StatusCodes.Status400BadRequest
            }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    /// <summary>
    /// Gets a payment by ID
    /// </summary>
    /// <param name="id">Payment ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentResponse>> GetPayment(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var paymentId = new PaymentId(id);
        var payment = await _paymentService.GetPaymentAsync(paymentId, cancellationToken);

        if (payment is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Payment not found",
                Detail = $"Payment with ID {id} was not found",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(PaymentResponse.FromPayment(payment));
    }

    /// <summary>
    /// Gets a paginated list of payments
    /// </summary>
    /// <param name="request">Pagination and filter parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated payment list</returns>
    [HttpGet]
    [ProducesResponseType<PagedResponse<PaymentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<PaymentResponse>>> GetPayments(
        [FromQuery] GetPaymentsRequest request,
        CancellationToken cancellationToken = default)
    {
        var validator = new GetPaymentsRequestValidator();
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            var problemDetails = new ValidationProblemDetails();
            foreach (var error in validationResult.Errors)
            {
                problemDetails.Errors.Add(error.PropertyName, new[] { error.ErrorMessage });
            }
            return BadRequest(problemDetails);
        }

        var payments = await _paymentService.GetPaymentsAsync(
            request.Page, 
            request.PageSize, 
            request.Status,
            request.FromDate,
            request.ToDate,
            cancellationToken);

        var response = new PagedResponse<PaymentResponse>
        {
            Data = payments.Data.Select(PaymentResponse.FromPayment).ToList(),
            Page = payments.Page,
            PageSize = payments.PageSize,
            TotalCount = payments.TotalCount,
            TotalPages = payments.TotalPages
        };

        return Ok(response);
    }
}
```

### Response Models and DTOs

Create dedicated response models that don't expose internal domain structure:

```csharp
public record PaymentResponse
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public string? Description { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }

    public static PaymentResponse FromPayment(Payment payment)
    {
        return new PaymentResponse
        {
            Id = payment.Id.Value,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status.ToString(),
            CreatedAt = payment.CreatedAt,
            ProcessedAt = payment.ProcessedAt,
            Description = payment.Description,
            Metadata = payment.Metadata
        };
    }
}

public record GetPaymentsRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public record PagedResponse<T>
{
    public IReadOnlyList<T> Data { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public class GetPaymentsRequestValidator : AbstractValidator<GetPaymentsRequest>
{
    public GetPaymentsRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(100)
            .WithMessage("Page size must be between 1 and 100");

        RuleFor(x => x.FromDate)
            .LessThanOrEqualTo(x => x.ToDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue)
            .WithMessage("From date cannot be later than to date");

        RuleFor(x => x.Status)
            .Must(BeValidStatus)
            .When(x => !string.IsNullOrEmpty(x.Status))
            .WithMessage("Status must be one of: Pending, Processing, Completed, Failed");
    }

    private static bool BeValidStatus(string? status)
    {
        if (string.IsNullOrEmpty(status))
            return true;

        var validStatuses = new[] { "Pending", "Processing", "Completed", "Failed" };
        return validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
    }
}
```

### Global Error Handling

Implement consistent error handling across the API:

```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing the request");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var problemDetails = exception switch
        {
            ArgumentException argEx => new ProblemDetails
            {
                Title = "Invalid argument",
                Detail = argEx.Message,
                Status = StatusCodes.Status400BadRequest
            },
            InvalidOperationException invalidOpEx => new ProblemDetails
            {
                Title = "Invalid operation",
                Detail = invalidOpEx.Message,
                Status = StatusCodes.Status409Conflict
            },
            UnauthorizedAccessException => new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = "You are not authorized to access this resource",
                Status = StatusCodes.Status401Unauthorized
            },
            _ => new ProblemDetails
            {
                Title = "An error occurred",
                Detail = _environment.IsDevelopment() ? exception.Message : "An unexpected error occurred",
                Status = StatusCodes.Status500InternalServerError
            }
        };

        problemDetails.Instance = context.Request.Path;
        problemDetails.Extensions.Add("traceId", context.TraceIdentifier);

        if (_environment.IsDevelopment() && exception is not ArgumentException)
        {
            problemDetails.Extensions.Add("stackTrace", exception.StackTrace);
        }

        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        
        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

// Register in Program.cs
app.UseMiddleware<GlobalExceptionMiddleware>();
```

## Data Access and Entity Framework

### Entity Configuration

Use explicit configuration over conventions for clarity and maintainability:

```csharp
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Id)
            .HasConversion(
                id => id.Value,
                value => new PaymentId(value))
            .ValueGeneratedNever();

        builder.Property(p => p.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.ProcessedAt)
            .IsRequired(false);

        builder.Property(p => p.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(p => p.Metadata)
            .HasConversion(
                metadata => metadata != null ? JsonSerializer.Serialize(metadata, (JsonSerializerOptions?)null) : null,
                json => json != null ? JsonSerializer.Deserialize<Dictionary<string, object>>(json, (JsonSerializerOptions?)null) : null)
            .HasColumnType("jsonb")
            .IsRequired(false);

        builder.Property(p => p.CustomerId)
            .HasConversion(
                id => id.Value,
                value => new CustomerId(value))
            .IsRequired();

        builder.Property(p => p.CardId)
            .HasMaxLength(50)
            .IsRequired();

        // Indexes
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.CustomerId);
        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => new { p.CustomerId, p.CreatedAt });
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion(
                id => id.Value,
                value => new CustomerId(value))
            .ValueGeneratedNever();

        builder.Property(c => c.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(c => c.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.OwnsOne(c => c.Address, addressBuilder =>
        {
            addressBuilder.Property(a => a.HouseNumber)
                .HasColumnName("address_house_number")
                .HasMaxLength(20)
                .IsRequired();

            addressBuilder.Property(a => a.HouseName)
                .HasColumnName("address_house_name")
                .HasMaxLength(100)
                .IsRequired(false);

            addressBuilder.Property(a => a.AddressLine1)
                .HasColumnName("address_line_1")
                .HasMaxLength(200)
                .IsRequired();

            addressBuilder.Property(a => a.AddressLine2)
                .HasColumnName("address_line_2")
                .HasMaxLength(200)
                .IsRequired(false);

            addressBuilder.Property(a => a.City)
                .HasColumnName("address_city")
                .HasMaxLength(100)
                .IsRequired();

            addressBuilder.Property(a => a.Postcode)
                .HasColumnName("address_postcode")
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(c => c.Email)
            .IsUnique();
        builder.HasIndex(c => c.CreatedAt);
    }
}
```

### DbContext Configuration

```csharp
public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations in the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);

        // Global query filters
        modelBuilder.Entity<Payment>()
            .HasQueryFilter(p => !p.IsDeleted);

        modelBuilder.Entity<Customer>()
            .HasQueryFilter(c => !c.IsDeleted);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // This should only be used for design-time operations
            optionsBuilder.UseNpgsql("Host=localhost;Database=payments_dev;Username=dev;Password=dev");
        }

        // Enable sensitive data logging only in development
        optionsBuilder.EnableSensitiveDataLogging(false);
        optionsBuilder.EnableDetailedErrors(true);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatically set audit fields
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditable && e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in entries)
        {
            var auditable = (IAuditable)entry.Entity;
            
            if (entry.State == EntityState.Added)
            {
                auditable.CreatedAt = DateTime.UtcNow;
            }
            
            auditable.UpdatedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

### Repository Pattern

Implement repository pattern for complex queries and data access abstraction:

```csharp
public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default);
    Task<PagedResult<Payment>> GetPagedAsync(int page, int pageSize, PaymentStatus? status = null, 
        DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
    Task DeleteAsync(PaymentId id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(PaymentId id, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalAmountByCustomerAsync(CustomerId customerId, DateTime fromDate, DateTime toDate, 
        CancellationToken cancellationToken = default);
}

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<PaymentRepository> _logger;

    public PaymentRepository(PaymentDbContext context, ILogger<PaymentRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Payment>> GetPagedAsync(
        int page, 
        int pageSize, 
        PaymentStatus? status = null,
        DateTime? fromDate = null, 
        DateTime? toDate = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Payments.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        
        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Payment>
        {
            Data = payments,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payment);
        
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Payment {PaymentId} added successfully", payment.Id);
    }

    public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payment);
        
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Payment {PaymentId} updated successfully", payment.Id);
    }

    public async Task DeleteAsync(PaymentId id, CancellationToken cancellationToken = default)
    {
        var payment = await GetByIdAsync(id, cancellationToken);
        if (payment is not null)
        {
            payment.MarkAsDeleted();
            await UpdateAsync(payment, cancellationToken);
            
            _logger.LogInformation("Payment {PaymentId} marked as deleted", id);
        }
    }

    public async Task<bool> ExistsAsync(PaymentId id, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .AnyAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<decimal> GetTotalAmountByCustomerAsync(
        CustomerId customerId, 
        DateTime fromDate, 
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Where(p => p.CustomerId == customerId)
            .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate)
            .Where(p => p.Status == PaymentStatus.Completed)
            .SumAsync(p => p.Amount, cancellationToken);
    }
}
```

### Database Migrations and Seeding

```csharp
// Migration helper for consistent database setup
public static class DatabaseExtensions
{
    public static async Task<WebApplication> MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Starting database migration");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migration completed successfully");

            if (app.Environment.IsDevelopment())
            {
                await SeedDevelopmentDataAsync(context, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }

        return app;
    }

    private static async Task SeedDevelopmentDataAsync(PaymentDbContext context, ILogger logger)
    {
        logger.LogInformation("Seeding development data");

        // Check if data already exists
        if (await context.Customers.AnyAsync())
        {
            logger.LogInformation("Development data already exists, skipping seed");
            return;
        }

        // Seed customers
        var customers = new[]
        {
            new Customer(
                CustomerId.New(),
                "john.doe@example.com",
                "John",
                "Doe",
                new AddressDetails("123", "Test House", "Test Street", null, "London", "SW1A 1AA")),
            new Customer(
                CustomerId.New(),
                "jane.smith@example.com",
                "Jane",
                "Smith",
                new AddressDetails("456", null, "Another Street", "Apt 2", "Manchester", "M1 1AA"))
        };

        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        // Seed payments
        var payments = customers.SelectMany(customer => new[]
        {
            new Payment(PaymentId.New(), 100m, "GBP", customer.Id, "card_123"),
            new Payment(PaymentId.New(), 250m, "USD", customer.Id, "card_456"),
            new Payment(PaymentId.New(), 75m, "EUR", customer.Id, "card_789")
        }).ToArray();

        context.Payments.AddRange(payments);
        await context.SaveChangesAsync();

        logger.LogInformation("Development data seeded successfully");
    }
}

// Usage in Program.cs
await app.MigrateDatabaseAsync();
```

### Unit of Work Pattern (Optional)

```csharp
public interface IUnitOfWork : IDisposable
{
    IPaymentRepository Payments { get; }
    ICustomerRepository Customers { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly PaymentDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(PaymentDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Payments = new PaymentRepository(_context, serviceProvider.GetRequiredService<ILogger<PaymentRepository>>());
        Customers = new CustomerRepository(_context, serviceProvider.GetRequiredService<ILogger<CustomerRepository>>());
    }

    public IPaymentRepository Payments { get; }
    public ICustomerRepository Customers { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
```