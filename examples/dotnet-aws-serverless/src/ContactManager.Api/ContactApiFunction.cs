using System;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Annotations.APIGateway;
using Amazon.Lambda.Core;
using ContactManager.Core.ContactRegistration;
using ContactManager.Core.Shared;
using AWS.Lambda.Powertools.Logging;
using System.Text.Json;
using System.Threading.Tasks;
using ContactManager.Api;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.SourceGeneratorLambdaJsonSerializer<CustomSerializationContext>))]
[assembly: LambdaGlobalProperties(GenerateMain = true)]

namespace ContactManager.Api;

public class ContactApiFunction
{
    private readonly IContactService _contactService;

    public ContactApiFunction(IContactService contactService)
    {
        _contactService = contactService ?? throw new ArgumentNullException(nameof(contactService));
    }

    [LambdaFunction]
    [HttpApi(LambdaHttpMethod.Post, "/contacts")]
    public async Task<IHttpResult> CreateContact([FromBody] ContactRequest request)
    {
        Logger.LogInformation("Processing contact creation request");

        try
        {
            if (request == null)
            {
                Logger.LogWarning("Request body is null or invalid");
                return HttpResults.BadRequest(new { error = "Request body is required" });
            }

            var result = await _contactService.RegisterContactAsync(request);

            return result switch
            {
                Success<ContactResponse> success => HttpResults.Created($"/contacts/{success.Value.Id}", success.Value),
                Failure<ContactResponse> failure => HandleFailure(failure),
                _ => HttpResults.InternalServerError(new { error = "Unexpected result type" })
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unhandled exception occurred while processing contact creation");
            return HttpResults.InternalServerError(new { error = "An internal error occurred" });
        }
    }

    private static IHttpResult HandleFailure(Failure<ContactResponse> failure)
    {
        Logger.LogWarning("Contact creation failed: {Error}", failure.Error);
        
        if (failure.Error.Contains("Validation failed"))
        {
            return HttpResults.BadRequest(new { error = failure.Error });
        }

        if (failure.Error.Contains("already exists"))
        {
            return HttpResults.Conflict(new { error = failure.Error });
        }

        return HttpResults.InternalServerError(new { error = "Failed to create contact" });
    }
}