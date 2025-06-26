using Amazon.DynamoDBv2;
using ContactManager.Adapters.Data;
using ContactManager.Core.ContactRegistration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ContactManager.Adapters;

public static class ServiceRegistration
{
    public static IServiceCollection AddAdapters(this IServiceCollection services, string tableName)
    {
        services.AddScoped<IContactRepository>(provider =>
        {
            var dynamoClient = provider.GetRequiredService<AmazonDynamoDBClient>();
            var logger = provider.GetRequiredService<ILogger<DynamoDbContactRepository>>();
            return new DynamoDbContactRepository(dynamoClient, tableName, logger);
        });

        return services;
    }
}