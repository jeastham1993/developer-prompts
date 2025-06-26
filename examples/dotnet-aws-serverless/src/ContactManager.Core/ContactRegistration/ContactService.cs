using ContactManager.Core.Shared;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ContactManager.Core.ContactRegistration;

public class ContactService : IContactService
{
    private readonly IContactRepository _contactRepository;
    private readonly IValidator<ContactRequest> _validator;
    private readonly ILogger<ContactService> _logger;

    public ContactService(
        IContactRepository contactRepository,
        IValidator<ContactRequest> validator,
        ILogger<ContactService> logger)
    {
        _contactRepository = contactRepository ?? throw new ArgumentNullException(nameof(contactRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ContactResponse>> RegisterContactAsync(
        ContactRequest request, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<ContactResponse>.Failure($"Validation failed: {errors}");
            }

            var contact = Contact.Create(request.Name, request.Email);
            await _contactRepository.AddAsync(contact, cancellationToken);

            _logger.LogInformation("Contact {ContactId} registered successfully", contact.Id);
            
            return Result<ContactResponse>.Success(ContactResponse.FromContact(contact));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register contact");
            return Result<ContactResponse>.Failure("Failed to register contact", ex);
        }
    }
}