using ContactManager.Core.ContactRegistration;
using FluentAssertions;
using Xunit;

namespace ContactManager.Core.Tests;

public class ContactResponseTests
{
    [Fact]
    public void ContactResponse_ShouldHaveCorrectProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "John Doe";
        var email = "john.doe@example.com";
        var createdAt = DateTime.UtcNow;

        // Act
        var response = new ContactResponse(id, name, email, createdAt);

        // Assert
        response.Id.Should().Be(id);
        response.Name.Should().Be(name);
        response.Email.Should().Be(email);
        response.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void FromContact_ShouldCreateContactResponse_FromContactEntity()
    {
        // Arrange
        var contact = Contact.Create("Jane Smith", "jane.smith@example.com");

        // Act
        var response = ContactResponse.FromContact(contact);

        // Assert
        response.Id.Should().Be(contact.Id.Value);
        response.Name.Should().Be(contact.Name);
        response.Email.Should().Be(contact.Email);
        response.CreatedAt.Should().Be(contact.CreatedAt);
    }

    [Fact]
    public void FromContact_ShouldPreserveAllContactData()
    {
        // Arrange
        var contactId = ContactId.New();
        var name = "Bob Wilson";
        var email = "bob.wilson@example.com";
        var createdAt = DateTime.UtcNow.AddDays(-5);
        var contact = new Contact(contactId, name, email, createdAt);

        // Act
        var response = ContactResponse.FromContact(contact);

        // Assert
        response.Id.Should().Be(contactId.Value);
        response.Name.Should().Be(name);
        response.Email.Should().Be(email);
        response.CreatedAt.Should().Be(createdAt);
    }
}