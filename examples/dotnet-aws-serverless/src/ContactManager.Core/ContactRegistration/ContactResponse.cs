namespace ContactManager.Core.ContactRegistration;

public record ContactResponse(Guid Id, string Name, string Email, DateTime CreatedAt)
{
    public static ContactResponse FromContact(Contact contact)
    {
        return new ContactResponse(
            contact.Id.Value,
            contact.Name,
            contact.Email,
            contact.CreatedAt);
    }
}