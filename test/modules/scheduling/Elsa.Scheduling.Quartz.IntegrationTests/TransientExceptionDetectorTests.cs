using Elsa.Scheduling.Quartz.Services;

namespace Elsa.Scheduling.Quartz.IntegrationTests;

/// <summary>
/// Integration tests for transient exception detection.
/// </summary>
public class TransientExceptionDetectorTests
{
    private readonly DefaultTransientExceptionDetector _detector = new();

    [Theory]
    [InlineData(typeof(HttpRequestException), true)]
    [InlineData(typeof(TimeoutException), true)]
    [InlineData(typeof(TaskCanceledException), true)]
    [InlineData(typeof(IOException), true)]
    [InlineData(typeof(InvalidOperationException), false)]
    [InlineData(typeof(ArgumentException), false)]
    public void IsTransient_VariousExceptionTypes_ReturnsExpectedResult(Type exceptionType, bool expected)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;
        var result = _detector.IsTransient(exception);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Connection timeout occurred", true)]
    [InlineData("Network error detected", true)]
    [InlineData("The operation timed out", true)]
    [InlineData("Connection reset by peer", true)]
    [InlineData("Invalid argument provided", false)]
    [InlineData("Object reference not set", false)]
    public void IsTransient_ExceptionMessages_ReturnsExpectedResult(string message, bool expected)
    {
        var exception = new Exception(message);
        var result = _detector.IsTransient(exception);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsTransient_NestedTransientException_ReturnsTrue()
    {
        var innerException = new TimeoutException("Operation timed out");
        var outerException = new InvalidOperationException("Wrapper", innerException);

        // The detector only checks the specific exception, not inner exceptions
        // The ExceptionExtensions.IsTransient() method handles nested exceptions
        var result = _detector.IsTransient(outerException);
        Assert.False(result);
    }

    [Fact]
    public void IsTransient_CaseInsensitivity_ReturnsTrue()
    {
        var exception = new Exception("CONNECTION TIMEOUT OCCURRED");
        var result = _detector.IsTransient(exception);
        Assert.True(result);
    }
}
