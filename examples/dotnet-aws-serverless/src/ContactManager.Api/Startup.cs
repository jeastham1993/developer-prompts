using Amazon.DynamoDBv2;
using Amazon.Lambda.Annotations;
using ContactManager.Adapters.Data;
using ContactManager.Core.ContactRegistration;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ContactManager.Api;

[LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        services.AddSingleton<AmazonDynamoDBClient>();
        
        services.AddScoped<IContactRepository>(provider =>
        {
            var dynamoClient = provider.GetRequiredService<AmazonDynamoDBClient>();
            var logger = provider.GetRequiredService<ILogger<DynamoDbContactRepository>>();
            var tableName = configuration["ContactsTableName"] ?? "Contacts";
            return new DynamoDbContactRepository(dynamoClient, tableName, logger);
        });

        services.AddScoped<IValidator<ContactRequest>, ContactRequestValidator>();
        services.AddScoped<IContactService, ContactService>();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddLambdaLogger();
        });
    }
}