namespace ContactManager.Core.ContactRegistration;

public interface IContactRepository
{
    Task AddAsync(Contact contact, CancellationToken cancellationToken = default);
    Task<Contact?> GetByIdAsync(ContactId id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(ContactId id, CancellationToken cancellationToken = default);
}