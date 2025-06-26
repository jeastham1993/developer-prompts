using ContactManager.Core.Shared;

namespace ContactManager.Core.ContactRegistration;

public interface IContactService
{
    Task<Result<ContactResponse>> RegisterContactAsync(
        ContactRequest request, 
        CancellationToken cancellationToken = default);
}