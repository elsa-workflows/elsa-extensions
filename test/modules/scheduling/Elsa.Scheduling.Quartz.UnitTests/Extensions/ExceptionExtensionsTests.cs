using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Extensions;
using Moq;

namespace Elsa.Scheduling.Quartz.UnitTests.Extensions;

public class ExceptionExtensionsTests
{
    [Fact]
    public void IsTransient_WithTransientException_ReturnsTrue()
    {
        var detector = new Mock<ITransientExceptionDetector>();
        detector.Setup(d => d.IsTransient(It.IsAny<TimeoutException>())).Returns(true);

        var exception = new TimeoutException();
        var result = exception.IsTransient([detector.Object]);

        Assert.True(result);
    }

    [Fact]
    public void IsTransient_WithInnerTransientException_ReturnsTrue()
    {
        var detector = new Mock<ITransientExceptionDetector>();
        detector.Setup(d => d.IsTransient(It.IsAny<TimeoutException>())).Returns(true);

        var innerException = new TimeoutException();
        var exception = new InvalidOperationException("Outer", innerException);
        var result = exception.IsTransient([detector.Object]);

        Assert.True(result);
    }

    [Fact]
    public void IsTransient_WithAggregateException_ChecksInnerExceptions()
    {
        var detector = new Mock<ITransientExceptionDetector>();
        detector.Setup(d => d.IsTransient(It.IsAny<HttpRequestException>())).Returns(true);

        var innerException = new HttpRequestException();
        var aggregateException = new AggregateException(innerException);
        var result = aggregateException.IsTransient([detector.Object]);

        Assert.True(result);
    }

    [Fact]
    public void IsTransient_WithNonTransientException_ReturnsFalse()
    {
        var detector = new Mock<ITransientExceptionDetector>();
        detector.Setup(d => d.IsTransient(It.IsAny<Exception>())).Returns(false);

        var exception = new InvalidOperationException();
        var result = exception.IsTransient([detector.Object]);

        Assert.False(result);
    }

    [Fact]
    public void IsTransient_WithMultipleDetectors_UsesAllDetectors()
    {
        var detector1 = new Mock<ITransientExceptionDetector>();
        detector1.Setup(d => d.IsTransient(It.IsAny<Exception>())).Returns(false);

        var detector2 = new Mock<ITransientExceptionDetector>();
        detector2.Setup(d => d.IsTransient(It.IsAny<TimeoutException>())).Returns(true);

        var exception = new TimeoutException();
        var result = exception.IsTransient([detector1.Object, detector2.Object]);

        Assert.True(result);
    }

    [Fact]
    public void IsTransient_WalksEntireExceptionChain_ReturnsTrue()
    {
        var detector = new Mock<ITransientExceptionDetector>();
        detector.Setup(d => d.IsTransient(It.IsAny<IOException>())).Returns(true);

        var innermost = new IOException();
        var middle = new InvalidOperationException("Middle", innermost);
        var outer = new Exception("Outer", middle);

        var result = outer.IsTransient([detector.Object]);

        Assert.True(result);
    }
}
