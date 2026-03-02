using Elsa.DevOps.AzureDevOps.Activities;

namespace Elsa.DevOps.AzureDevOps.UnitTests.Activities;

public class ActivityInputValidationTests
{
    public class TryValidateRequiredTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        public void Returns_Invalid_For_Null_Or_Whitespace(string? value)
        {
            var (valid, error) = ActivityInputValidation.TryValidateRequired(value, "Param");

            Assert.False(valid);
            Assert.Contains("Param", error);
        }

        [Theory]
        [InlineData("hello")]
        [InlineData("some value")]
        [InlineData("  a  ")]
        public void Returns_Valid_For_Non_Empty_Strings(string value)
        {
            var (valid, error) = ActivityInputValidation.TryValidateRequired(value, "Param");

            Assert.True(valid);
            Assert.Null(error);
        }
    }

    public class TryValidateUriTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Returns_Invalid_For_Null_Or_Empty(string? value)
        {
            var (valid, error) = ActivityInputValidation.TryValidateUri(value, "Url");

            Assert.False(valid);
            Assert.Contains("Url", error);
        }

        [Theory]
        [InlineData("not-a-url")]
        [InlineData("ftp://example.com")]
        [InlineData("file:///local")]
        [InlineData("just some text")]
        public void Returns_Invalid_For_Non_Http_Uris(string value)
        {
            var (valid, error) = ActivityInputValidation.TryValidateUri(value, "Url");

            Assert.False(valid);
            Assert.Contains("valid HTTP or HTTPS URL", error);
        }

        [Theory]
        [InlineData("http://example.com")]
        [InlineData("https://dev.azure.com/myorg")]
        [InlineData("  https://example.com  ")]
        public void Returns_Valid_For_Http_And_Https_Uris(string value)
        {
            var (valid, error) = ActivityInputValidation.TryValidateUri(value, "Url");

            Assert.True(valid);
            Assert.Null(error);
        }
    }

    public class TryValidateNonNegativeTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        [InlineData(int.MinValue)]
        public void Returns_Invalid_For_Negative_Values(int value)
        {
            var (valid, error) = ActivityInputValidation.TryValidateNonNegative(value, "Count");

            Assert.False(valid);
            Assert.Contains("Count", error);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(int.MaxValue)]
        public void Returns_Valid_For_Zero_Or_Positive(int value)
        {
            var (valid, error) = ActivityInputValidation.TryValidateNonNegative(value, "Count");

            Assert.True(valid);
            Assert.Null(error);
        }
    }

    public class TryValidatePositiveTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void Returns_Invalid_For_Zero_Or_Negative(int value)
        {
            var (valid, error) = ActivityInputValidation.TryValidatePositive(value, "Id");

            Assert.False(valid);
            Assert.Contains("Id", error);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(42)]
        [InlineData(int.MaxValue)]
        public void Returns_Valid_For_Positive_Values(int value)
        {
            var (valid, error) = ActivityInputValidation.TryValidatePositive(value, "Id");

            Assert.True(valid);
            Assert.Null(error);
        }
    }

    public class ThrowIfNullOrEmptyTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Throws_For_Null_Or_Whitespace(string? value)
        {
            var ex = Assert.Throws<ArgumentException>(
                () => ActivityInputValidation.ThrowIfNullOrEmpty(value, "Token"));

            Assert.Equal("Token", ex.ParamName);
        }

        [Fact]
        public void Does_Not_Throw_For_Valid_Value()
        {
            var ex = Record.Exception(
                () => ActivityInputValidation.ThrowIfNullOrEmpty("valid", "Token"));

            Assert.Null(ex);
        }
    }

    public class ThrowIfInvalidUriTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Throws_For_Null_Or_Empty(string? value)
        {
            Assert.Throws<ArgumentException>(
                () => ActivityInputValidation.ThrowIfInvalidUri(value, "Url"));
        }

        [Theory]
        [InlineData("not-a-url")]
        [InlineData("ftp://example.com")]
        public void Throws_For_Non_Http_Uris(string value)
        {
            var ex = Assert.Throws<ArgumentException>(
                () => ActivityInputValidation.ThrowIfInvalidUri(value, "Url"));

            Assert.Equal("Url", ex.ParamName);
        }

        [Theory]
        [InlineData("http://example.com")]
        [InlineData("https://dev.azure.com/org")]
        public void Does_Not_Throw_For_Valid_Http_Uris(string value)
        {
            var ex = Record.Exception(
                () => ActivityInputValidation.ThrowIfInvalidUri(value, "Url"));

            Assert.Null(ex);
        }
    }

    public class ThrowIfNegativeTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Throws_For_Negative_Values(int value)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => ActivityInputValidation.ThrowIfNegative(value, "Index"));

            Assert.Equal("Index", ex.ParamName);
            Assert.Equal(value, ex.ActualValue);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void Does_Not_Throw_For_Zero_Or_Positive(int value)
        {
            var ex = Record.Exception(
                () => ActivityInputValidation.ThrowIfNegative(value, "Index"));

            Assert.Null(ex);
        }
    }

    public class ThrowIfNegativeOrZeroTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Throws_For_Zero_Or_Negative(int value)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => ActivityInputValidation.ThrowIfNegativeOrZero(value, "Id"));

            Assert.Equal("Id", ex.ParamName);
            Assert.Equal(value, ex.ActualValue);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(42)]
        public void Does_Not_Throw_For_Positive_Values(int value)
        {
            var ex = Record.Exception(
                () => ActivityInputValidation.ThrowIfNegativeOrZero(value, "Id"));

            Assert.Null(ex);
        }
    }
}
