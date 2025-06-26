using ContactManager.Core.ContactRegistration;
using ContactManager.Core.Shared;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ContactManager.Core.Tests;

public class ContactServiceTests
{
    private readonly Mock<IContactRepository> _repositoryMock;
    private readonly Mock<IValidator<ContactRequest>> _validatorMock;
    private readonly Mock<ILogger<ContactService>> _loggerMock;
    private readonly ContactService _contactService;

    public ContactServiceTests()
    {
        _repositoryMock = new Mock<IContactRepository>();
        _validatorMock = new Mock<IValidator<ContactRequest>>();
        _loggerMock = new Mock<ILogger<ContactService>>();
        
        _contactService = new ContactService(
            _repositoryMock.Object,
            _validatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RegisterContactAsync_ShouldReturnSuccess_WhenRequestIsValid()
    {
        // Arrange
        var request = new ContactRequest("John Doe", "john.doe@example.com");
        var validationResult = new ValidationResult();
        
        _validatorMock
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _contactService.RegisterContactAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Should().BeOfType<Success<ContactResponse>>();
        
        var success = result as Success<ContactResponse>;
        success!.Value.Name.Should().Be(request.Name);
        success.Value.Email.Should().Be(request.Email);
        success.Value.Id.Should().NotBeEmpty();

        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterContactAsync_ShouldReturnFailure_WhenValidationFails()
    {
        // Arrange
        var request = new ContactRequest("", "invalid-email");
        var validationFailure = new ValidationFailure("Email", "Email is invalid");
        var validationResult = new ValidationResult(new[] { validationFailure });
        
        _validatorMock
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _contactService.RegisterContactAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Should().BeOfType<Failure<ContactResponse>>();
        
        var failure = result as Failure<ContactResponse>;
        failure!.Error.Should().Contain("Validation failed");
        failure.Error.Should().Contain("Email is invalid");

        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterContactAsync_ShouldReturnFailure_WhenRepositoryThrows()
    {
        // Arrange
        var request = new ContactRequest("John Doe", "john.doe@example.com");
        var validationResult = new ValidationResult();
        var exception = new InvalidOperationException("Database error");
        
        _validatorMock
            .Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _contactService.RegisterContactAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Should().BeOfType<Failure<ContactResponse>>();
        
        var failure = result as Failure<ContactResponse>;
        failure!.Error.Should().Be("Failed to register contact");
        failure.Exception.Should().Be(exception);
    }

    [Fact]
    public async Task RegisterContactAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Act & Assert
        var act = async () => await _contactService.RegisterContactAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RegisterContactAsync_ShouldUseCancellationToken()
    {
        // Arrange
        var request = new ContactRequest("John Doe", "john.doe@example.com");
        var validationResult = new ValidationResult();
        var cancellationToken = new CancellationToken();
        
        _validatorMock
            .Setup(x => x.ValidateAsync(request, cancellationToken))
            .ReturnsAsync(validationResult);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Contact>(), cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _contactService.RegisterContactAsync(request, cancellationToken);

        // Assert
        _validatorMock.Verify(x => x.ValidateAsync(request, cancellationToken), Times.Once);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Contact>(), cancellationToken), Times.Once);
    }
}