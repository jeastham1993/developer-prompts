using System.Text.RegularExpressions;

namespace ContactManager.Core.ContactRegistration;

public class Contact
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ContactId Id { get; }
    public string Name { get; }
    public string Email { get; }
    public DateTime CreatedAt { get; }

    public Contact(ContactId id, string name, string email, DateTime createdAt)
    {
        Id = id;
        Name = name;
        Email = email;
        CreatedAt = createdAt;
    }

    public static Contact Create(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or whitespace", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or whitespace", nameof(email));
        }

        if (!EmailRegex.IsMatch(email))
        {
            throw new ArgumentException("Email must be a valid email address", nameof(email));
        }

        return new Contact(ContactId.New(), name, email, DateTime.UtcNow);
    }
}