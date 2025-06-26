# Development Guidelines for .NET

## Application Features

This is an application that is going to take a request from the user, that contains a name and email address and simply store that in the database.

## Core Philosophy

**TEST-DRIVEN DEVELOPMENT IS NON-NEGOTIABLE.** Every single line of production code must be written in response to a failing test. No exceptions. This is not a suggestion or a preference - it is the fundamental practice that enables all other principles in this document.

I follow Test-Driven Development (TDD) with a strong emphasis on behavior-driven testing and functional programming principles. All work should be done in small, incremental changes that maintain a working state throughout development.

I also follow 'serverless-first' thinking, choosing AWS serverless technologies as the deployment model of choice. Prioritizing AWS Lambda for compute.

## Quick Reference

**Key Principles:**

- Write tests first (TDD)
- Test behavior, not implementation
- Nullable reference types enabled
- Immutable data only
- Small, pure functions
- C# 12+ features and .NET 8
- Use real models/DTOs in tests, never redefine them

**Preferred Tools:**

- **Language**: C# 12+ (.NET 8)
- **Testing**: xUnit + FluentAssertions no higher than version 7 + Testcontainers
- **State Management**: Prefer immutable patterns and records
- **Validation**: FluentValidation
- **Serialization**: System.Text.Json
- **Deployment** Single purpose Lambda functions, with a separate function for each

## AWS Serverless Best Practices

### Lambda Function Development

#### Lambda Annotations Framework

Use the [Lambda Annotations Framework](https://github.com/aws/aws-lambda-dotnet/tree/master/Libraries/src/Amazon.Lambda.Annotations) for simplified Lambda function development:

```csharp
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PaymentProcessor.Lambda;

public class PaymentApiFunction
{
    private readonly IPaymentService _paymentService;
    
    public PaymentApiFunction(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }
    
    [LambdaFunction]
    [HttpApi(LambdaHttpMethod.Post, "/payments")]
    public async Task<IHttpResult> ProcessPayment([FromBody] PaymentRequest request)
    {
        var result = await _paymentService.ProcessPaymentAsync(request);
        
        return result.IsSuccess 
            ? HttpResults.Ok(result.Value)
            : HttpResults.BadRequest(result.Error);
    }
}
```

### Dependency Injection and Options Pattern

Use the `[LambdaStartup]` attribute to configure dependency injection and use the options pattern:

```csharp


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
[LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
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

#### Compilation Strategy

**Synchronous Functions (APIs, Real-time Processing):**
- Use Native AOT compilation for faster cold starts and better performance
- Ensure all dependencies support AOT compilation
- Test thoroughly with AOT-specific constraints
- Here is an example of how to configure native AOT with Lambda
- When building single purpose Lambda functions the Amazon.Lambda.AspNetCoreServer.Hosting is not required

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
    <AWSProjectType>Lambda</AWSProjectType>
    <AssemblyName>bootstrap</AssemblyName>
    <PublishReadyToRunComposite>true</PublishReadyToRunComposite>
    <EventSourceSupport>false</EventSourceSupport>
    <UseSystemResourceKeys>true</UseSystemResourceKeys>
    <InvariantGlobalization>true</InvariantGlobalization>
    <SelfContained>true</SelfContained>
    <PublishAot>true</PublishAot>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Amazon.Lambda.Annotations" Version="1.5.0" />
  </ItemGroup>
</Project>
```

**Asynchronous Functions (Event Processing, Background Tasks):**
- Use standard .NET compilation for better compatibility
- Cold start performance is less critical for async workloads

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <GenerateRuntimeConfigurationFiles>true</GenerateRuntimeConfigurationFiles>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Amazon.Lambda.Core" Version="2.2.0" />
    <PackageReference Include="Amazon.Lambda.SQSEvents" Version="2.2.0" />
    <PackageReference Include="Amazon.Lambda.Serialization.SystemTextJson" Version="2.4.1" />
  </ItemGroup>
</Project>
```

#### AOT-Compatible Libraries

When using Native AOT, ensure all dependencies are compatible:

**Recommended AOT-Compatible Libraries:**
- `System.Text.Json` (built-in serialization)
- `Amazon.DynamoDBv2` (low-level client)
- `Amazon.Lambda.Core`
- `Amazon.Lambda.Annotations`
- `FluentValidation` (with proper configuration)

**Avoid for AOT:**
- `Newtonsoft.Json`
- Entity Framework Core (use DynamoDB instead)
- Heavy reflection-based frameworks

### Data Access with DynamoDB

#### Low-Level API Usage

Always use the low-level DynamoDB API for serverless applications to minimize dependencies and improve performance. Avoid the `Scan` operation where possible:

```csharp
public class PaymentRepository
{
    private readonly AmazonDynamoDBClient _dynamoClient;
    private readonly string _tableName;
    
    public PaymentRepository(AmazonDynamoDBClient dynamoClient, string tableName)
    {
        _dynamoClient = dynamoClient;
        _tableName = tableName;
    }
    
    public async Task<Payment?> GetPaymentAsync(string paymentId)
    {
        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"PAYMENT#{paymentId}" },
                ["SK"] = new AttributeValue { S = $"PAYMENT#{paymentId}" }
            }
        };
        
        var response = await _dynamoClient.GetItemAsync(request);
        
        return response.IsItemSet 
            ? MapToPayment(response.Item)
            : null;
    }
    
    public async Task SavePaymentAsync(Payment payment)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"PAYMENT#{payment.Id}" },
            ["SK"] = new AttributeValue { S = $"PAYMENT#{payment.Id}" },
            ["Amount"] = new AttributeValue { N = payment.Amount.ToString() },
            ["Currency"] = new AttributeValue { S = payment.Currency },
            ["Status"] = new AttributeValue { S = payment.Status.ToString() },
            ["CreatedAt"] = new AttributeValue { S = payment.CreatedAt.ToString("O") },
            ["TTL"] = new AttributeValue { N = DateTimeOffset.UtcNow.AddDays(90).ToUnixTimeSeconds().ToString() }
        };
        
        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = item,
            ConditionExpression = "attribute_not_exists(PK)"
        };
        
        await _dynamoClient.PutItemAsync(request);
    }
}
```

#### Data Access Pattern Documentation

**Single Table Design Pattern:**
```csharp
// Primary Key Design
// PK = PAYMENT#{PaymentId} | SK = PAYMENT#{PaymentId}     // Payment entity
// PK = CUSTOMER#{CustomerId} | SK = PAYMENT#{PaymentId}   // Customer's payments (GSI)
// PK = DATE#{YYYY-MM-DD} | SK = PAYMENT#{PaymentId}       // Payments by date (GSI)

public record PaymentEntity
{
    public string PK { get; init; } = default!;  // PAYMENT#{PaymentId}
    public string SK { get; init; } = default!;  // PAYMENT#{PaymentId}
    public string GSI1PK { get; init; } = default!;  // CUSTOMER#{CustomerId}
    public string GSI1SK { get; init; } = default!;  // PAYMENT#{PaymentId}
    public string GSI2PK { get; init; } = default!;  // DATE#{YYYY-MM-DD}
    public string GSI2SK { get; init; } = default!;  // PAYMENT#{PaymentId}
    public decimal Amount { get; init; }
    public string Currency { get; init; } = default!;
    public PaymentStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public long TTL { get; init; }  // Auto-expire old records
}
```

**Query Patterns:**
1. Get payment by ID: Query PK = PAYMENT#{PaymentId}
2. Get customer's payments: Query GSI1PK = CUSTOMER#{CustomerId}
3. Get payments by date range: Query GSI2PK = DATE#{YYYY-MM-DD}

#### Repository Pattern

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
    private readonly AmazonDynamoDBClient _ddbClient;
    private readonly ILogger<PaymentRepository> _logger;

    public PaymentRepository(AmazonDynamoDBClient ddbClient, ILogger<PaymentRepository> logger)
    {
        _ddbClient = ddbClient ?? throw new ArgumentNullException(nameof(ddbClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Implementations go here.
}
```

### Event-Driven Messaging

#### CloudEvents Integration

Use CloudEvents as a wrapper for all event publishing to ensure standardization and interoperability:

```csharp
public class PaymentEventPublisher
{
    private readonly IAmazonEventBridge _eventBridge;
    private readonly string _eventBusName;
    
    public PaymentEventPublisher(IAmazonEventBridge eventBridge, string eventBusName)
    {
        _eventBridge = eventBridge;
        _eventBusName = eventBusName;
    }
    
    public async Task PublishPaymentProcessedAsync(Payment payment)
    {
        var cloudEvent = new CloudEvent
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("https://api.payment-processor.com/payments"),
            Type = "com.paymentprocessor.payment.processed.v1",
            Time = DateTimeOffset.UtcNow,
            Subject = payment.Id,
            Data = new PaymentProcessedEvent
            {
                PaymentId = payment.Id,
                CustomerId = payment.CustomerId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                ProcessedAt = payment.ProcessedAt
            }
        };
        
        var eventDetail = JsonSerializer.Serialize(cloudEvent, JsonOptions.Default);
        
        var putEventsRequest = new PutEventsRequest
        {
            Entries = new List<PutEventsRequestEntry>
            {
                new()
                {
                    Source = cloudEvent.Source.ToString(),
                    DetailType = cloudEvent.Type,
                    Detail = eventDetail,
                    EventBusName = _eventBusName,
                    Time = cloudEvent.Time?.DateTime ?? DateTime.UtcNow
                }
            }
        };
        
        await _eventBridge.PutEventsAsync(putEventsRequest);
    }
}
```

#### Message Handling with OpenTelemetry

Implement OpenTelemetry semantic conventions for messaging operations:

```csharp
public class PaymentEventHandler
{
    private static readonly ActivitySource ActivitySource = new("PaymentProcessor.Events");
    private readonly ILogger<PaymentEventHandler> _logger;
    
    public PaymentEventHandler(ILogger<PaymentEventHandler> logger)
    {
        _logger = logger;
    }
    
    [LambdaFunction]
    public async Task<string> HandlePaymentEvent(SQSEvent sqsEvent)
    {
        foreach (var record in sqsEvent.Records)
        {
            using var activity = ActivitySource.StartActivity("payment.event.process");
            
            // OpenTelemetry semantic conventions for messaging
            activity?.SetTag("messaging.system", "aws_sqs");
            activity?.SetTag("messaging.destination.name", record.EventSourceArn?.Split(':').Last());
            activity?.SetTag("messaging.operation", "process");
            activity?.SetTag("messaging.message.id", record.MessageId);
            activity?.SetTag("messaging.message.conversation_id", record.MessageId);
            activity?.SetTag("cloud.provider", "aws");
            activity?.SetTag("cloud.service.name", "sqs");
            
            try
            {
                var cloudEvent = JsonSerializer.Deserialize<CloudEvent>(record.Body, JsonOptions.Default);
                
                activity?.SetTag("messaging.message.payload_size_bytes", record.Body.Length);
                activity?.SetTag("cloudevents.event_id", cloudEvent?.Id);
                activity?.SetTag("cloudevents.event_type", cloudEvent?.Type);
                activity?.SetTag("cloudevents.event_source", cloudEvent?.Source?.ToString());
                
                await ProcessCloudEventAsync(cloudEvent!);
                
                activity?.SetStatus(ActivityStatusCode.Ok);
                _logger.LogInformation("Successfully processed payment event {EventId}", cloudEvent?.Id);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.SetTag("error.type", ex.GetType().Name);
                
                _logger.LogError(ex, "Failed to process payment event {MessageId}", record.MessageId);
                throw;
            }
        }
        
        return "Success";
    }
    
    private async Task ProcessCloudEventAsync(CloudEvent cloudEvent)
    {
        using var activity = ActivitySource.StartActivity($"payment.event.{ExtractEventAction(cloudEvent.Type)}");
        
        activity?.SetTag("cloudevents.event_id", cloudEvent.Id);
        activity?.SetTag("cloudevents.event_type", cloudEvent.Type);
        activity?.SetTag("cloudevents.event_subject", cloudEvent.Subject);
        
        switch (cloudEvent.Type)
        {
            case "com.paymentprocessor.payment.processed.v1":
                var paymentEvent = JsonSerializer.Deserialize<PaymentProcessedEvent>(
                    cloudEvent.Data?.ToString() ?? string.Empty, JsonOptions.Default);
                await HandlePaymentProcessedAsync(paymentEvent!);
                break;
                
            default:
                _logger.LogWarning("Unknown event type: {EventType}", cloudEvent.Type);
                break;
        }
    }
    
    private static string ExtractEventAction(string? eventType)
    {
        return eventType?.Split('.').LastOrDefault() ?? "unknown";
    }
}
```

#### Tracing Configuration

Configure OpenTelemetry tracing for Lambda functions:

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder.AddSource("PaymentProcessor.Events")
                       .AddAWSInstrumentation()
                       .AddHttpClientInstrumentation()
                       .SetSampler(new AlwaysOnSampler())
                       .AddOtlpExporter();
            });
            
        services.AddAWSService<IAmazonEventBridge>();
        services.AddAWSService<AmazonDynamoDBClient>();
        
        // Register application services
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<PaymentRepository>();
        services.AddScoped<PaymentEventPublisher>();
    }
}
```

### Testing Serverless Applications

#### Integration Testing with Testcontainers

```csharp
public class PaymentLambdaIntegrationTests : IAsyncLifetime
{
    private readonly DynamoDbContainer _dynamoContainer;
    private readonly LocalStackContainer _localStackContainer;
    private AmazonDynamoDBClient _dynamoClient = default!;
    private IAmazonEventBridge _eventBridgeClient = default!;
    
    public PaymentLambdaIntegrationTests()
    {
        _dynamoContainer = new DynamoDbBuilder()
            .WithImage("amazon/dynamodb-local:latest")
            .Build();
            
        _localStackContainer = new LocalStackBuilder()
            .WithServices(LocalStackContainer.Service.EventBridge)
            .Build();
    }
    
    public async Task InitializeAsync()
    {
        await _dynamoContainer.StartAsync();
        await _localStackContainer.StartAsync();
        
        _dynamoClient = new AmazonDynamoDBClient(new AmazonDynamoDBConfig
        {
            ServiceURL = _dynamoContainer.GetConnectionString()
        });
        
        _eventBridgeClient = new AmazonEventBridgeClient(new AmazonEventBridgeConfig
        {
            ServiceURL = _localStackContainer.GetConnectionString()
        });
        
        await CreateTestTableAsync();
    }
    
    [Fact]
    public async Task ProcessPayment_ShouldStoreInDynamoAndPublishEvent()
    {
        // Arrange
        var paymentRequest = new PaymentRequestBuilder()
            .WithAmount(100m)
            .WithCurrency("GBP")
            .Build();
            
        var function = new PaymentApiFunction(
            new PaymentService(
                new PaymentRepository(_dynamoClient, "test-payments"),
                new PaymentEventPublisher(_eventBridgeClient, "test-event-bus")));
        
        // Act
        var result = await function.ProcessPayment(paymentRequest);
        
        // Assert
        result.Should().BeOfType<Ok<Payment>>();
        
        // Verify DynamoDB storage
        var storedPayment = await GetPaymentFromDynamoAsync(paymentRequest.IdempotencyKey);
        storedPayment.Should().NotBeNull();
        storedPayment!.Amount.Should().Be(100m);
        
        // Verify event publication (would require LocalStack EventBridge setup)
    }
}
```

### Performance Optimization

#### Memory and Resource Management

```csharp
// Use minimal memory allocation for Lambda functions
public class OptimizedPaymentProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    // Reuse HTTP clients and AWS service clients
    private static readonly AmazonDynamoDBClient DynamoClient = new();
    private static readonly HttpClient HttpClient = new();
    
    [LambdaFunction]
    public async Task<APIGatewayProxyResponse> ProcessPayment(APIGatewayProxyRequest request)
    {
        // Use spans for memory-efficient string operations
        var body = request.Body.AsSpan();
        
        // Minimize allocations in hot paths
        using var document = JsonDocument.Parse(body);
        var paymentRequest = JsonSerializer.Deserialize<PaymentRequest>(document.RootElement, JsonOptions);
        
        // Implementation...
        
        return new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonSerializer.Serialize(result, JsonOptions),
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            }
        };
    }
}
```

## Testing Principles

### Behavior-Driven Testing

- **No "unit tests"** - this term is not helpful. Tests should verify expected behavior, treating implementation as a black box
- Test through the public API exclusively - internals should be invisible to tests
- Create tests in a separate project directory underneath the `tests` directory
- Tests that examine internal implementation details are wasteful and should be avoided
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
  PaymentProcessorApplication/
    PaymentProcessorApplication.csproj
    PaymentProcessor.cs
    PaymentValidator.cs
tests/
  PaymentProcessorApplication.Tests/
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
  YourApp.Core/             # Application layer (use cases, services), Domain models, entities, value objects, DTOs, requests, responses
  YourApp.Adapters/         # Data access, external services
  YourApp.BackgroundWorkers/         # This project is only required if the service performs asynchronous work. It is seperate to run any background workers independently from a synchronous API
tests/
  YourApp.Api.Tests/        # API integration tests
  YourApp.Application.Tests/ # Core layer tests
```

Inside the .Core library, DO NOT create folders based on technical feature (Entities, DTO's, Services). Instead, create folders based on the actual business value that grouped set of code performs. Loosely following a vertical slice architecture style. A new developer should be able to look at the files/folders inside a project and understand what is is that the application does.

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
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
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

## Refactoring - The Critical Third Step

Evaluating refactoring opportunities is not optional - it's the third step in the TDD cycle. After achieving a green state and committing your work, you MUST assess whether the code can be improved. However, only refactor if there's clear value - if the code is already clean and expresses intent well, move on to the next test.

#### What is Refactoring?

Refactoring means changing the internal structure of code without changing its external behavior. The public API remains unchanged, all tests continue to pass, but the code becomes cleaner, more maintainable, or more efficient. Remember: only refactor when it genuinely improves the code - not all code needs refactoring.

#### When to Refactor

- **Always assess after green**: Once tests pass, before moving to the next test, evaluate if refactoring would add value
- **When you see duplication**: But understand what duplication really means (see DRY below)
- **When names could be clearer**: Variable names, method names, or type names that don't clearly express intent
- **When structure could be simpler**: Complex conditional logic, deeply nested code, or long methods
- **When patterns emerge**: After implementing several similar features, useful abstractions may become apparent

**Remember**: Not all code needs refactoring. If the code is already clean, expressive, and well-structured, commit and move on. Refactoring should improve the code - don't change things just for the sake of change.

#### Refactoring Guidelines

##### 1. Commit Before Refactoring

Always commit your working code before starting any refactoring. This gives you a safe point to return to:

```bash
git add .
git commit -m "feat: add payment validation"
# Now safe to refactor
```

##### 2. Look for Useful Abstractions Based on Semantic Meaning

Create abstractions only when code shares the same semantic meaning and purpose. Don't abstract based on structural similarity alone - **duplicate code is far cheaper than the wrong abstraction**.

```csharp
// Similar structure, DIFFERENT semantic meaning - DO NOT ABSTRACT
private static bool ValidatePaymentAmount(decimal amount)
{
    return amount > 0 && amount <= 10000;
}

private static bool ValidateTransferAmount(decimal amount)
{
    return amount > 0 && amount <= 10000;
}

// These might have the same structure today, but they represent different
// business concepts that will likely evolve independently.
// Payment limits might change based on fraud rules.
// Transfer limits might change based on account type.
// Abstracting them couples unrelated business rules.

// Similar structure, SAME semantic meaning - SAFE TO ABSTRACT
private static string FormatUserDisplayName(string firstName, string lastName)
{
    return $"{firstName} {lastName}".Trim();
}

private static string FormatCustomerDisplayName(string firstName, string lastName)
{
    return $"{firstName} {lastName}".Trim();
}

private static string FormatEmployeeDisplayName(string firstName, string lastName)
{
    return $"{firstName} {lastName}".Trim();
}

// These all represent the same concept: "how we format a person's name for display"
// They share semantic meaning, not just structure
private static string FormatPersonDisplayName(string firstName, string lastName)
{
    return $"{firstName} {lastName}".Trim();
}

// Replace all call sites throughout the codebase:
// Before:
// var userLabel = FormatUserDisplayName(user.FirstName, user.LastName);
// var customerName = FormatCustomerDisplayName(customer.FirstName, customer.LastName);
// var employeeTag = FormatEmployeeDisplayName(employee.FirstName, employee.LastName);

// After:
// var userLabel = FormatPersonDisplayName(user.FirstName, user.LastName);
// var customerName = FormatPersonDisplayName(customer.FirstName, customer.LastName);
// var employeeTag = FormatPersonDisplayName(employee.FirstName, employee.LastName);

// Then remove the original methods as they're no longer needed
```

**Questions to ask before abstracting:**

- Do these code blocks represent the same concept or different concepts that happen to look similar?
- If the business rules for one change, should the others change too?
- Would a developer reading this abstraction understand why these things are grouped together?
- Am I abstracting based on what the code IS (structure) or what it MEANS (semantics)?

**Remember**: It's much easier to create an abstraction later when the semantic relationship becomes clear than to undo a bad abstraction that couples unrelated concepts.

##### 3. Understanding DRY - It's About Knowledge, Not Code

DRY (Don't Repeat Yourself) is about not duplicating **knowledge** in the system, not about eliminating all code that looks similar.

```csharp
// This is NOT a DRY violation - different knowledge despite similar code
private static bool ValidateUserAge(int age)
{
    return age >= 18 && age <= 100;
}

private static bool ValidateProductRating(int rating)
{
    return rating >= 1 && rating <= 5;
}

private static bool ValidateYearsOfExperience(int years)
{
    return years >= 0 && years <= 50;
}

// These represent different business concepts with different ranges and reasons:
// - User age: based on legal requirements and realistic human lifespan
// - Product rating: based on UI design (5-star system)
// - Years of experience: based on realistic career length
// Don't abstract them just because they look similar!

// This IS a DRY violation - same knowledge expressed multiple times
public static class PaymentLimits
{
    public static bool IsValidAmount(decimal amount) => amount > 0 && amount <= 10000;
}

public static class RefundLimits
{
    public static bool IsValidAmount(decimal amount) => amount > 0 && amount <= 10000;
}

// Both represent the same business rule: "transaction amount limits"
// This should be consolidated into a single source of truth:
public static class TransactionLimits
{
    private const decimal MinAmount = 0;
    private const decimal MaxAmount = 10000;
    
    public static bool IsValidAmount(decimal amount) => amount > MinAmount && amount <= MaxAmount;
}
```

##### 4. Extracting Methods vs. Extracting Classes

Start by extracting methods within the same class. Only extract to new classes when you have a cohesive set of related methods and data.

```csharp
// Start here: Long method doing multiple things
public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
{
    // Validation logic (20 lines)
    if (string.IsNullOrEmpty(request.CardNumber))
        return PaymentResult.Invalid("Card number is required");
    if (request.Amount <= 0)
        return PaymentResult.Invalid("Amount must be positive");
    // ... more validation

    // Authorization logic (15 lines)
    var authRequest = new AuthorizationRequest
    {
        CardNumber = request.CardNumber,
        Amount = request.Amount,
        Currency = request.Currency
    };
    var authResult = await _authorizationService.AuthorizeAsync(authRequest);
    if (!authResult.IsSuccessful)
        return PaymentResult.Failed(authResult.ErrorMessage);

    // Capture logic (10 lines)
    var captureRequest = new CaptureRequest
    {
        AuthorizationId = authResult.AuthorizationId,
        Amount = request.Amount
    };
    var captureResult = await _captureService.CaptureAsync(captureRequest);
    // ... etc
}

// Step 1: Extract methods (keep in same class)
public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
{
    var validationResult = ValidatePaymentRequest(request);
    if (!validationResult.IsValid)
        return PaymentResult.Invalid(validationResult.ErrorMessage);

    var authorizationResult = await AuthorizePaymentAsync(request);
    if (!authorizationResult.IsSuccessful)
        return PaymentResult.Failed(authorizationResult.ErrorMessage);

    var captureResult = await CapturePaymentAsync(authorizationResult.AuthorizationId, request.Amount);
    return captureResult.IsSuccessful 
        ? PaymentResult.Success(captureResult.PaymentId)
        : PaymentResult.Failed(captureResult.ErrorMessage);
}

private ValidationResult ValidatePaymentRequest(PaymentRequest request)
{
    // Validation logic extracted to method
}

private async Task<AuthorizationResult> AuthorizePaymentAsync(PaymentRequest request)
{
    // Authorization logic extracted to method
}

private async Task<CaptureResult> CapturePaymentAsync(string authorizationId, decimal amount)
{
    // Capture logic extracted to method
}

// Step 2: Only if these methods grow and need shared state, extract to class
public class PaymentProcessor
{
    // Only extract to class when methods are cohesive and share state
}
```

##### 5. When NOT to Refactor

```csharp
// Don't refactor just because code is long if it's already clear
public static PaymentValidationResult ValidatePayment(PaymentRequest request)
{
    var errors = new List<string>();
    
    if (string.IsNullOrWhiteSpace(request.CardNumber))
        errors.Add("Card number is required");
        
    if (request.CardNumber?.Length != 16)
        errors.Add("Card number must be 16 digits");
        
    if (string.IsNullOrWhiteSpace(request.ExpiryDate))
        errors.Add("Expiry date is required");
        
    if (!IsValidExpiryDate(request.ExpiryDate))
        errors.Add("Expiry date must be in MM/YY format and not expired");
        
    if (string.IsNullOrWhiteSpace(request.Cvv))
        errors.Add("CVV is required");
        
    if (request.Cvv?.Length is not (3 or 4))
        errors.Add("CVV must be 3 or 4 digits");
        
    if (request.Amount <= 0)
        errors.Add("Amount must be greater than zero");
        
    if (request.Amount > 10000)
        errors.Add("Amount cannot exceed £10,000");
    
    return errors.Count == 0 
        ? PaymentValidationResult.Valid() 
        : PaymentValidationResult.Invalid(errors);
}

// This method is long but perfectly clear - don't refactor it!
// Each line does exactly one validation check
// The structure is consistent and easy to follow
// Breaking it up would make it harder to understand
```

## Performance and Optimization

### Async/Await Best Practices

```csharp
// Good - Proper async/await usage
public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
{
    var payment = await ValidateAndCreatePaymentAsync(request, cancellationToken);
    
    var authResult = await _authorizationService.AuthorizeAsync(payment.ToAuthRequest(), cancellationToken);
    if (!authResult.IsSuccess)
    {
        return PaymentResult.Failed(authResult.Error);
    }
    
    var captureResult = await _captureService.CaptureAsync(authResult.AuthorizationId, cancellationToken);
    return PaymentResult.Success(captureResult.PaymentId);
}

// Avoid - Blocking on async code
public PaymentResult ProcessPayment(PaymentRequest request)
{
    // NEVER do this - it can cause deadlocks
    var result = ProcessPaymentAsync(request).Result;
    return result;
}

// Good - Parallel execution when operations are independent
public async Task<CustomerSummary> GetCustomerSummaryAsync(CustomerId customerId, CancellationToken cancellationToken = default)
{
    var customerTask = _customerRepository.GetByIdAsync(customerId, cancellationToken);
    var paymentsTask = _paymentRepository.GetByCustomerIdAsync(customerId, cancellationToken);
    var ordersTask = _orderRepository.GetByCustomerIdAsync(customerId, cancellationToken);

    await Task.WhenAll(customerTask, paymentsTask, ordersTask);

    var customer = await customerTask;
    var payments = await paymentsTask;
    var orders = await ordersTask;

    return new CustomerSummary(customer, payments, orders);
}

// Good - ConfigureAwait(false) in library code, but not required if building an application.
public async Task<Payment> CreatePaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
{
    var payment = Payment.Create(request);
    
    await _paymentRepository.AddAsync(payment, cancellationToken).ConfigureAwait(false);
    await _eventPublisher.PublishAsync(new PaymentCreatedEvent(payment.Id), cancellationToken).ConfigureAwait(false);
    
    return payment;
}

// Good - Proper cancellation token usage
public async Task<IReadOnlyList<Payment>> GetPaymentsAsync(
    CustomerId customerId, 
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    
    return await _context.Payments
        .Where(p => p.CustomerId == customerId)
        .ToListAsync(cancellationToken);
}
```

### Memory Management and Resource Disposal

```csharp
// Good - Proper disposal with using statements
public async Task<Stream> GeneratePaymentReportAsync(ReportRequest request, CancellationToken cancellationToken = default)
{
    using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
    using var command = connection.CreateCommand();
    
    command.CommandText = "SELECT * FROM payments WHERE created_at BETWEEN @start AND @end";
    command.Parameters.Add(new SqlParameter("@start", request.StartDate));
    command.Parameters.Add(new SqlParameter("@end", request.EndDate));
    
    using var reader = await command.ExecuteReaderAsync(cancellationToken);
    
    var memoryStream = new MemoryStream();
    await WriteReportToStreamAsync(reader, memoryStream, cancellationToken);
    
    memoryStream.Position = 0;
    return memoryStream;
}

// Good - IAsyncDisposable for async cleanup
public sealed class PaymentProcessor : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IServiceScope _scope;
    private bool _disposed;

    public PaymentProcessor(HttpClient httpClient, IServiceScope scope)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    public async Task<PaymentResult> ProcessAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        // Processing logic...
        return PaymentResult.Success();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            _httpClient?.Dispose();
            
            if (_scope is IAsyncDisposable asyncDisposableScope)
            {
                await asyncDisposableScope.DisposeAsync();
            }
            else
            {
                _scope?.Dispose();
            }
        }
        finally
        {
            _disposed = true;
        }
    }
}

// Good - Struct for small, immutable data to reduce allocations
public readonly record struct PaymentId(Guid Value)
{
    public static PaymentId New() => new(Guid.NewGuid());
    public static PaymentId Parse(ReadOnlySpan<char> value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}

// Good - Span<T> and Memory<T> for efficient string operations
public static class PaymentCardHelper
{
    public static string MaskCardNumber(ReadOnlySpan<char> cardNumber)
    {
        if (cardNumber.Length != 16)
            throw new ArgumentException("Card number must be 16 digits", nameof(cardNumber));

        Span<char> masked = stackalloc char[19]; // 16 digits + 3 spaces
        
        // Copy first 4 digits
        cardNumber[..4].CopyTo(masked);
        masked[4] = ' ';
        
        // Mask middle 8 digits
        "****".AsSpan().CopyTo(masked[5..]);
        masked[9] = ' ';
        "****".AsSpan().CopyTo(masked[10..]);
        masked[14] = ' ';
        
        // Copy last 4 digits
        cardNumber[12..].CopyTo(masked[15..]);
        
        return new string(masked);
    }
}
```

## Security Best Practices

### Input Validation and Sanitization

```csharp
// Good - Comprehensive input validation using FluentValidation
public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320)
            .WithMessage("Email must be a valid email address with maximum 320 characters");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-zA-Z\s\-'\.]+$")
            .WithMessage("First name can only contain letters, spaces, hyphens, apostrophes, and periods");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-zA-Z\s\-'\.]+$")
            .WithMessage("Last name can only contain letters, spaces, hyphens, apostrophes, and periods");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Phone number must be in international format");

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.UtcNow.AddYears(-18))
            .GreaterThan(DateTime.UtcNow.AddYears(-120))
            .WithMessage("Customer must be between 18 and 120 years old");
    }
}
```

### Sensitive Data Protection

```csharp
// Good - Data encryption for sensitive information
public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public EncryptionService(IOptions<EncryptionOptions> options)
    {
        var encryptionOptions = options.Value;
        _key = Convert.FromBase64String(encryptionOptions.Key);
        _iv = Convert.FromBase64String(encryptionOptions.IV);
    }

    public string EncryptString(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using var swEncrypt = new StreamWriter(csEncrypt);
        
        swEncrypt.Write(plainText);
        
        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    public string DecryptString(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        var decryptor = aes.CreateDecryptor();
        using var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        
        return srDecrypt.ReadToEnd();
    }
}

// Good - Secure logging that doesn't expose sensitive data
public static partial class LoggerExtensions
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Payment {PaymentId} created for customer {CustomerId} with amount {Amount:C}")]
    public static partial void LogPaymentCreated(this ILogger logger, PaymentId paymentId, CustomerId customerId, decimal amount);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Payment {PaymentId} failed validation: {ValidationErrors}")]
    public static partial void LogPaymentValidationFailed(this ILogger logger, PaymentId paymentId, string validationErrors);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "Payment processing failed for payment {PaymentId}")]
    public static partial void LogPaymentProcessingFailed(this ILogger logger, PaymentId paymentId, Exception exception);

    // Never log sensitive data like card numbers, CVV, etc.
    public static void LogPaymentAttempt(this ILogger logger, PaymentRequest request)
    {
        // Only log non-sensitive data
        logger.LogInformation(
            "Payment attempt: Amount {Amount:C}, Currency {Currency}, Customer {CustomerId}, Card ending {CardLast4}",
            request.Amount,
            request.Currency,
            request.CustomerId,
            request.CardNumber?.Length >= 4 ? request.CardNumber[^4..] : "****");
    }
}

// Good - Secure password hashing
public class PasswordService : IPasswordService
{
    private const int SaltSize = 32;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    public string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);

        var hashBytes = new byte[SaltSize + HashSize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        var hashBytes = Convert.FromBase64String(hashedPassword);
        var salt = new byte[SaltSize];
        Array.Copy(hashBytes, 0, salt, 0, SaltSize);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(HashSize);

        for (var i = 0; i < HashSize; i++)
        {
            if (hashBytes[i + SaltSize] != hash[i])
                return false;
        }

        return true;
    }
}
```

## Monitoring and Observability

IT IS VITAL THAT YOU ensure that you don't write any information to log files, or as trace attributes, that would be considered personal identifiable information or sensitive information. That explicatally covers things like names, email address, credit card information, social security or national insurance numbers.

### Structured Logging

Use [Lambda Powertools for structured logging](https://docs.powertools.aws.dev/lambda/dotnet/getting-started/logger/simple/).

```csharp
using AWS.Lambda.Powertools.Logging;

// Good - Structured logging with correlation IDs and context
public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;

    public async Task<Result<Payment>> ProcessPaymentAsync(
        PaymentRequest request, 
        CancellationToken cancellationToken = default)
    {
        Logger.AppendKey("PaymentId", PaymentId.New());
        Logger.AppendKey("PaymentId", request.CustomerId);
        Logger.AppendKey("PaymentId", request.Amount);
        Logger.AppendKey("PaymentId", request.Currency);

        Logger.LogInformation("Starting payment processing");

        try
        {
            var validationResult = await ValidatePaymentAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                Logger.LogWarning("Payment validation failed: {ValidationErrors}", 
                    string.Join(", ", validationResult.Errors));
                return Result<Payment>.Failure("Validation failed");
            }

            var payment = await CreatePaymentAsync(request, cancellationToken);
            
            Logger.LogInformation("Payment {PaymentId} processed successfully", payment.Id);
            return Result<Payment>.Success(payment);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Payment processing failed");
            return Result<Payment>.Failure("Payment processing failed");
        }
    }
}

// Good - Activity and tracing for distributed systems
public class PaymentGatewayService : IPaymentGatewayService
{
    private static readonly ActivitySource ActivitySource = new("PaymentGateway");
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentGatewayService> _logger;

    public async Task<AuthorizationResult> AuthorizePaymentAsync(
        AuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("payment.authorize");
        activity?.SetTag("payment.amount", request.Amount.ToString());
        activity?.SetTag("payment.currency", request.Currency);
        activity?.SetTag("customer.id", request.CustomerId.ToString());

        try
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/authorize")
            {
                Content = JsonContent.Create(request)
            };

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            
            activity?.SetTag("http.status_code", (int)response.StatusCode);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthorizationResult>(cancellationToken: cancellationToken);
                activity?.SetTag("authorization.result", "success");
                return result!;
            }

            activity?.SetTag("authorization.result", "failed");
            activity?.SetStatus(ActivityStatusCode.Error, "Authorization failed");
            
            return AuthorizationResult.Failed("Gateway authorization failed");
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Payment authorization failed");
            throw;
        }
    }
}
```