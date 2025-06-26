using ContactManager.Core.ContactRegistration;
using FluentAssertions;
using Xunit;

namespace ContactManager.Core.Tests;

public class ContactTests
{
    [Fact]
    public void Create_ShouldCreateContact_WithValidNameAndEmail()
    {
        // Arrange
        var name = "John Doe";
        var email = "john.doe@example.com";

        // Act
        var contact = Contact.Create(name, email);

        // Assert
        contact.Id.Should().NotBe(default(ContactId));
        contact.Name.Should().Be(name);
        contact.Email.Should().Be(email);
        contact.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ShouldThrowArgumentException_WhenNameIsNullOrWhitespace(string? name)
    {
        // Arrange
        var email = "john.doe@example.com";

        // Act & Assert
        var act = () => Contact.Create(name!, email);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Name cannot be null or whitespace*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_ShouldThrowArgumentException_WhenEmailIsNullOrWhitespace(string? email)
    {
        // Arrange
        var name = "John Doe";

        // Act & Assert
        var act = () => Contact.Create(name, email!);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Email cannot be null or whitespace*");
    }

    [Fact]
    public void Create_ShouldThrowArgumentException_WhenEmailIsInvalid()
    {
        // Arrange
        var name = "John Doe";
        var invalidEmail = "not-an-email";

        // Act & Assert
        var act = () => Contact.Create(name, invalidEmail);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Email must be a valid email address*");
    }

    [Fact]
    public void Constructor_ShouldCreateContact_WithProvidedValues()
    {
        // Arrange
        var id = ContactId.New();
        var name = "Jane Smith";
        var email = "jane.smith@example.com";
        var createdAt = DateTime.UtcNow.AddDays(-1);

        // Act
        var contact = new Contact(id, name, email, createdAt);

        // Assert
        contact.Id.Should().Be(id);
        contact.Name.Should().Be(name);
        contact.Email.Should().Be(email);
        contact.CreatedAt.Should().Be(createdAt);
    }
}