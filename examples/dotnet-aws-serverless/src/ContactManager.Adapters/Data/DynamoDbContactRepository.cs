using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using ContactManager.Core.ContactRegistration;
using Microsoft.Extensions.Logging;

namespace ContactManager.Adapters.Data;

public class DynamoDbContactRepository : IContactRepository
{
    private readonly AmazonDynamoDBClient _dynamoClient;
    private readonly string _tableName;
    private readonly ILogger<DynamoDbContactRepository> _logger;

    public DynamoDbContactRepository(
        AmazonDynamoDBClient dynamoClient,
        string tableName,
        ILogger<DynamoDbContactRepository> logger)
    {
        _dynamoClient = dynamoClient ?? throw new ArgumentNullException(nameof(dynamoClient));
        _tableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task AddAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contact);

        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"CONTACT#{contact.Id.Value}" },
            ["SK"] = new AttributeValue { S = $"CONTACT#{contact.Id.Value}" },
            ["Id"] = new AttributeValue { S = contact.Id.Value.ToString() },
            ["Name"] = new AttributeValue { S = contact.Name },
            ["Email"] = new AttributeValue { S = contact.Email },
            ["CreatedAt"] = new AttributeValue { S = contact.CreatedAt.ToString("O") },
            ["TTL"] = new AttributeValue { N = DateTimeOffset.UtcNow.AddYears(7).ToUnixTimeSeconds().ToString() }
        };

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item = item,
            ConditionExpression = "attribute_not_exists(PK)"
        };

        try
        {
            await _dynamoClient.PutItemAsync(request, cancellationToken);
            _logger.LogInformation("Contact {ContactId} stored successfully", contact.Id);
        }
        catch (ConditionalCheckFailedException)
        {
            _logger.LogWarning("Contact {ContactId} already exists", contact.Id);
            throw new InvalidOperationException($"Contact with ID {contact.Id} already exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store contact {ContactId}", contact.Id);
            throw;
        }
    }

    public async Task<Contact?> GetByIdAsync(ContactId id, CancellationToken cancellationToken = default)
    {
        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"CONTACT#{id.Value}" },
                ["SK"] = new AttributeValue { S = $"CONTACT#{id.Value}" }
            }
        };

        try
        {
            var response = await _dynamoClient.GetItemAsync(request, cancellationToken);
            
            if (!response.IsItemSet)
            {
                return null;
            }

            return MapToContact(response.Item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve contact {ContactId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(ContactId id, CancellationToken cancellationToken = default)
    {
        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"CONTACT#{id.Value}" },
                ["SK"] = new AttributeValue { S = $"CONTACT#{id.Value}" }
            },
            ProjectionExpression = "PK"
        };

        try
        {
            var response = await _dynamoClient.GetItemAsync(request, cancellationToken);
            return response.IsItemSet;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence of contact {ContactId}", id);
            throw;
        }
    }

    private static Contact MapToContact(Dictionary<string, AttributeValue> item)
    {
        var id = new ContactId(Guid.Parse(item["Id"].S));
        var name = item["Name"].S;
        var email = item["Email"].S;
        var createdAt = DateTime.Parse(item["CreatedAt"].S);

        return new Contact(id, name, email, createdAt);
    }
}