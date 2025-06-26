using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ContactManager.Adapters.Data;
using ContactManager.Core.ContactRegistration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.DynamoDb;
using Xunit;

namespace ContactManager.Adapters.Tests;

public class DynamoDbContactRepositoryTests : IAsyncLifetime
{
    private readonly DynamoDbContainer _dynamoContainer;
    private AmazonDynamoDBClient _dynamoClient = default!;
    private DynamoDbContactRepository _repository = default!;
    private const string TableName = "ContactsTest";

    public DynamoDbContactRepositoryTests()
    {
        _dynamoContainer = new DynamoDbBuilder()
            .WithImage("amazon/dynamodb-local:latest")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _dynamoContainer.StartAsync();
        
        _dynamoClient = new AmazonDynamoDBClient(new AmazonDynamoDBConfig
        {
            ServiceURL = _dynamoContainer.GetConnectionString()
        });

        await CreateTestTableAsync();
        
        var loggerMock = new Mock<ILogger<DynamoDbContactRepository>>();
        _repository = new DynamoDbContactRepository(_dynamoClient, TableName, loggerMock.Object);
    }

    public async Task DisposeAsync()
    {
        _dynamoClient?.Dispose();
        await _dynamoContainer.DisposeAsync();
    }

    private async Task CreateTestTableAsync()
    {
        var createTableRequest = new CreateTableRequest
        {
            TableName = TableName,
            KeySchema = new List<KeySchemaElement>
            {
                new() { AttributeName = "PK", KeyType = KeyType.HASH },
                new() { AttributeName = "SK", KeyType = KeyType.RANGE }
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new() { AttributeName = "PK", AttributeType = ScalarAttributeType.S },
                new() { AttributeName = "SK", AttributeType = ScalarAttributeType.S }
            },
            BillingMode = BillingMode.PAY_PER_REQUEST
        };

        await _dynamoClient.CreateTableAsync(createTableRequest);
        
        await WaitForTableToBeActiveAsync();
    }

    private async Task WaitForTableToBeActiveAsync()
    {
        var maxRetries = 30;
        var delay = TimeSpan.FromSeconds(1);
        
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await _dynamoClient.DescribeTableAsync(TableName);
                if (response.Table.TableStatus == TableStatus.ACTIVE)
                    return;
            }
            catch (ResourceNotFoundException)
            {
                // Table not found yet
            }
            
            await Task.Delay(delay);
        }
        
        throw new TimeoutException("Table did not become active within the expected time");
    }

    [Fact]
    public async Task AddAsync_ShouldStoreContact_InDynamoDB()
    {
        // Arrange
        var contact = Contact.Create("John Doe", "john.doe@example.com");

        // Act
        await _repository.AddAsync(contact);

        // Assert
        var storedContact = await _repository.GetByIdAsync(contact.Id);
        storedContact.Should().NotBeNull();
        storedContact!.Id.Should().Be(contact.Id);
        storedContact.Name.Should().Be(contact.Name);
        storedContact.Email.Should().Be(contact.Email);
        storedContact.CreatedAt.Should().BeCloseTo(contact.CreatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenContactDoesNotExist()
    {
        // Arrange
        var nonExistentId = ContactId.New();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenContactExists()
    {
        // Arrange
        var contact = Contact.Create("Jane Smith", "jane.smith@example.com");
        await _repository.AddAsync(contact);

        // Act
        var exists = await _repository.ExistsAsync(contact.Id);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenContactDoesNotExist()
    {
        // Arrange
        var nonExistentId = ContactId.New();

        // Act
        var exists = await _repository.ExistsAsync(nonExistentId);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_ShouldThrowException_WhenContactAlreadyExists()
    {
        // Arrange
        var contact = Contact.Create("Bob Wilson", "bob.wilson@example.com");
        await _repository.AddAsync(contact);

        var duplicateContact = new Contact(contact.Id, "Different Name", "different@email.com", DateTime.UtcNow);

        // Act & Assert
        var act = async () => await _repository.AddAsync(duplicateContact);
        await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*already exists*");
    }

    [Fact]
    public async Task AddAsync_ShouldUseCancellationToken()
    {
        // Arrange
        var contact = Contact.Create("Alice Brown", "alice.brown@example.com");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await _repository.AddAsync(contact, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldUseCancellationToken()
    {
        // Arrange
        var contactId = ContactId.New();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await _repository.GetByIdAsync(contactId, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExistsAsync_ShouldUseCancellationToken()
    {
        // Arrange
        var contactId = ContactId.New();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await _repository.ExistsAsync(contactId, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}