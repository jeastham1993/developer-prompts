using ContactManager.Core.ContactRegistration;
using FluentAssertions;
using Xunit;

namespace ContactManager.Core.Tests;

public class ContactIdTests
{
    [Fact]
    public void New_ShouldCreateNewContactId_WithUniqueValue()
    {
        // Act
        var contactId = ContactId.New();

        // Assert
        contactId.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void New_ShouldCreateDifferentIds_WhenCalledMultipleTimes()
    {
        // Act
        var contactId1 = ContactId.New();
        var contactId2 = ContactId.New();

        // Assert
        contactId1.Should().NotBe(contactId2);
        contactId1.Value.Should().NotBe(contactId2.Value);
    }

    [Fact]
    public void Parse_ShouldCreateContactId_FromValidGuidString()
    {
        // Arrange
        var guidString = Guid.NewGuid().ToString();

        // Act
        var contactId = ContactId.Parse(guidString);

        // Assert
        contactId.Value.Should().Be(Guid.Parse(guidString));
    }

    [Fact]
    public void ToString_ShouldReturnGuidString()
    {
        // Arrange
        var originalGuid = Guid.NewGuid();
        var contactId = new ContactId(originalGuid);

        // Act
        var result = contactId.ToString();

        // Assert
        result.Should().Be(originalGuid.ToString());
    }

    [Fact]
    public void Parse_ShouldThrowFormatException_WhenGuidStringIsInvalid()
    {
        // Arrange
        var invalidGuidString = "not-a-guid";

        // Act & Assert
        var act = () => ContactId.Parse(invalidGuidString);
        act.Should().Throw<FormatException>();
    }
}