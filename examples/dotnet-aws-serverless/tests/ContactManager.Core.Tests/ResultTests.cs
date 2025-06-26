using ContactManager.Core.Shared;
using FluentAssertions;
using Xunit;

namespace ContactManager.Core.Tests;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessResult_WithValue()
    {
        // Arrange
        var value = "test value";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Should().BeOfType<Success<string>>();
        
        var success = result as Success<string>;
        success!.Value.Should().Be(value);
    }

    [Fact]
    public void Failure_ShouldCreateFailureResult_WithError()
    {
        // Arrange
        var error = "Something went wrong";

        // Act
        var result = Result<string>.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Should().BeOfType<Failure<string>>();
        
        var failure = result as Failure<string>;
        failure!.Error.Should().Be(error);
        failure.Exception.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldCreateFailureResult_WithErrorAndException()
    {
        // Arrange
        var error = "Something went wrong";
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = Result<string>.Failure(error, exception);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Should().BeOfType<Failure<string>>();
        
        var failure = result as Failure<string>;
        failure!.Error.Should().Be(error);
        failure.Exception.Should().Be(exception);
    }

    [Fact]
    public void Map_ShouldTransformSuccessValue_WhenResultIsSuccess()
    {
        // Arrange
        var successResult = Result<int>.Success(42);

        // Act
        var mappedResult = successResult.Map(x => x.ToString());

        // Assert
        mappedResult.IsSuccess.Should().BeTrue();
        mappedResult.Should().BeOfType<Success<string>>();
        
        var success = mappedResult as Success<string>;
        success!.Value.Should().Be("42");
    }

    [Fact]
    public void Map_ShouldReturnFailure_WhenResultIsFailure()
    {
        // Arrange
        var failureResult = Result<int>.Failure("Error occurred");

        // Act
        var mappedResult = failureResult.Map(x => x.ToString());

        // Assert
        mappedResult.IsSuccess.Should().BeFalse();
        mappedResult.Should().BeOfType<Failure<string>>();
        
        var failure = mappedResult as Failure<string>;
        failure!.Error.Should().Be("Error occurred");
    }

    [Fact]
    public async Task MapAsync_ShouldTransformSuccessValue_WhenResultIsSuccess()
    {
        // Arrange
        var successResult = Result<int>.Success(42);

        // Act
        var mappedResult = await successResult.MapAsync(async x => 
        {
            await Task.Delay(1);
            return x.ToString();
        });

        // Assert
        mappedResult.IsSuccess.Should().BeTrue();
        mappedResult.Should().BeOfType<Success<string>>();
        
        var success = mappedResult as Success<string>;
        success!.Value.Should().Be("42");
    }

    [Fact]
    public async Task MapAsync_ShouldReturnFailure_WhenResultIsFailure()
    {
        // Arrange
        var failureResult = Result<int>.Failure("Error occurred");

        // Act
        var mappedResult = await failureResult.MapAsync(async x => 
        {
            await Task.Delay(1);
            return x.ToString();
        });

        // Assert
        mappedResult.IsSuccess.Should().BeFalse();
        mappedResult.Should().BeOfType<Failure<string>>();
        
        var failure = mappedResult as Failure<string>;
        failure!.Error.Should().Be("Error occurred");
    }

    [Fact]
    public void FlatMap_ShouldTransformSuccessValue_WhenResultIsSuccess()
    {
        // Arrange
        var successResult = Result<int>.Success(42);

        // Act
        var mappedResult = successResult.FlatMap(x => Result<string>.Success(x.ToString()));

        // Assert
        mappedResult.IsSuccess.Should().BeTrue();
        mappedResult.Should().BeOfType<Success<string>>();
        
        var success = mappedResult as Success<string>;
        success!.Value.Should().Be("42");
    }

    [Fact]
    public void FlatMap_ShouldReturnMappedFailure_WhenResultIsSuccessButMapperReturnsFailure()
    {
        // Arrange
        var successResult = Result<int>.Success(42);

        // Act
        var mappedResult = successResult.FlatMap(x => Result<string>.Failure("Mapping failed"));

        // Assert
        mappedResult.IsSuccess.Should().BeFalse();
        mappedResult.Should().BeOfType<Failure<string>>();
        
        var failure = mappedResult as Failure<string>;
        failure!.Error.Should().Be("Mapping failed");
    }

    [Fact]
    public void FlatMap_ShouldReturnOriginalFailure_WhenResultIsFailure()
    {
        // Arrange
        var failureResult = Result<int>.Failure("Original error");

        // Act
        var mappedResult = failureResult.FlatMap(x => Result<string>.Success(x.ToString()));

        // Assert
        mappedResult.IsSuccess.Should().BeFalse();
        mappedResult.Should().BeOfType<Failure<string>>();
        
        var failure = mappedResult as Failure<string>;
        failure!.Error.Should().Be("Original error");
    }
}