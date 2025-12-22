using System.Net.Sockets;
using Elsa.Scheduling.Quartz.Services;

namespace Elsa.Scheduling.Quartz.UnitTests.Services;

public class DefaultTransientExceptionDetectorTests
{
    private readonly DefaultTransientExceptionDetector _detector = new();

    [Theory]
    [InlineData(typeof(HttpRequestException))]
    [InlineData(typeof(TimeoutException))]
    [InlineData(typeof(TaskCanceledException))]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(SocketException))]
    [InlineData(typeof(EndOfStreamException))]
    public void IsTransient_KnownTransientExceptionTypes_ReturnsTrue(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;
        Assert.True(_detector.IsTransient(exception));
    }

    [Theory]
    [InlineData("Operation timed out")]
    [InlineData("Connection reset by peer")]
    [InlineData("Connection refused")]
    [InlineData("Broken pipe")]
    [InlineData("Network error occurred")]
    [InlineData("End of stream reached")]
    [InlineData("Attempted to read past the end of the stream")]
    public void IsTransient_ExceptionWithTransientMessage_ReturnsTrue(string message)
    {
        var exception = new Exception(message);
        Assert.True(_detector.IsTransient(exception));
    }

    [Fact]
    public void IsTransient_NonTransientException_ReturnsFalse()
    {
        var exception = new InvalidOperationException("Not a transient error");
        Assert.False(_detector.IsTransient(exception));
    }

    [Fact]
    public void IsTransient_CaseInsensitiveMatching_ReturnsTrue()
    {
        var exception = new Exception("TIMEOUT occurred");
        Assert.True(_detector.IsTransient(exception));
    }
}
