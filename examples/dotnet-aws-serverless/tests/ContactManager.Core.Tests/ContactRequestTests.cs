using ContactManager.Core.ContactRegistration;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace ContactManager.Core.Tests;

public class ContactRequestTests
{
    private readonly ContactRequestValidator _validator;

    public ContactRequestTests()
    {
        _validator = new ContactRequestValidator();
    }

    [Fact]
    public void ContactRequest_ShouldHaveValidProperties()
    {
        // Arrange
        var name = "John Doe";
        var email = "john.doe@example.com";

        // Act
        var request = new ContactRequest(name, email);

        // Assert
        request.Name.Should().Be(name);
        request.Email.Should().Be(email);
    }

    [Fact]
    public void Validator_ShouldNotHaveError_WhenNameAndEmailAreValid()
    {
        // Arrange
        var request = new ContactRequest("John Doe", "john.doe@example.com");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validator_ShouldHaveError_WhenNameIsNullOrWhitespace(string? name)
    {
        // Arrange
        var request = new ContactRequest(name!, "john.doe@example.com");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validator_ShouldHaveError_WhenEmailIsNullOrWhitespace(string? email)
    {
        // Arrange
        var request = new ContactRequest("John Doe", email!);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email is required");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("invalid@")]
    [InlineData("@invalid.com")]
    [InlineData("invalid.com")]
    public void Validator_ShouldHaveError_WhenEmailIsInvalid(string email)
    {
        // Arrange
        var request = new ContactRequest("John Doe", email);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email must be a valid email address");
    }

    [Fact]
    public void Validator_ShouldHaveError_WhenNameIsTooLong()
    {
        // Arrange
        var longName = new string('a', 101);
        var request = new ContactRequest(longName, "john.doe@example.com");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name cannot exceed 100 characters");
    }

    [Fact]
    public void Validator_ShouldHaveError_WhenEmailIsTooLong()
    {
        // Arrange
        var longLocalPart = new string('a', 310);
        var longEmail = $"{longLocalPart}@example.com";
        var request = new ContactRequest("John Doe", longEmail);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email cannot exceed 320 characters");
    }
}