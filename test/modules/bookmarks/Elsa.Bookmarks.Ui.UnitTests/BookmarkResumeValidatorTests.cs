using Elsa.Bookmarks.Ui.Models;
using Elsa.Bookmarks.Ui.Services;

namespace Elsa.Bookmarks.Ui.UnitTests;

public class BookmarkResumeValidatorTests
{
    [Fact]
    public void ReportsAMissingRequiredField()
    {
        BookmarkResumeSchema schema = new("Ask whether it can be submitted.",
            [new BookmarkResumeField("confirmed", BookmarkFieldType.Boolean, "Akkoord", Required: true)]);

        BookmarkResumeValidationResult result = BookmarkResumeValidator.Validate(schema, "{}");

        // The sentence is what the model reads and acts on, so it has to name the field rather than merely say no.
        Assert.False(result.IsValid);
        Assert.Contains(result.Messages, message => message.Contains("confirmed", StringComparison.Ordinal));
    }

    [Fact]
    public void ReportsAValueThatWillNotConvert()
    {
        BookmarkResumeSchema schema = new("Ask for the number.",
            [new BookmarkResumeField("amount", BookmarkFieldType.Number, "Aantal", Required: true)]);

        BookmarkResumeValidationResult result = BookmarkResumeValidator.Validate(schema, """{"amount":"veel"}""");

        // Passing "veel" on to a workflow expecting a number turns a fixable mistake into a workflow fault.
        Assert.False(result.IsValid);
        Assert.Contains(result.Messages, message => message.Contains("amount", StringComparison.Ordinal));
    }

    [Fact]
    public void ReportsAChoiceOutsideItsOptions()
    {
        BookmarkResumeSchema schema = new("Ask which one.",
            [new BookmarkResumeField("pick", BookmarkFieldType.Choice, "Keuze", Required: true,
                Options: [new BookmarkFieldOption("a", "Eerste"), new BookmarkFieldOption("b", "Tweede")])]);

        BookmarkResumeValidationResult result = BookmarkResumeValidator.Validate(schema, """{"pick":"c"}""");

        // A model that invents an option should be told what the options are, not silently resume the workflow with one
        // that does not exist.
        Assert.False(result.IsValid);
        Assert.Contains(result.Messages, message => message.Contains("pick", StringComparison.Ordinal));
    }

    [Fact]
    public void DropsAnUnknownFieldAndSaysSo()
    {
        BookmarkResumeSchema schema = new("Ask whether it can be submitted.",
            [new BookmarkResumeField("confirmed", BookmarkFieldType.Boolean, "Akkoord", Required: true)]);

        BookmarkResumeValidationResult result = BookmarkResumeValidator.Validate(schema, """{"confirmed":true,"extra":"x"}""");

        // The answer is still usable, so this must not fail the resume - but a workflow that receives a key it never
        // asked for has no way to report the mistake, so the key is dropped and the sentence carries the warning.
        Assert.True(result.IsValid);
        Assert.False(result.Values.ContainsKey("extra"));
        Assert.Contains(result.Messages, message => message.Contains("extra", StringComparison.Ordinal));
    }

    [Fact]
    public void NormalisesAWellFormedAnswer()
    {
        BookmarkResumeSchema schema = new("Ask for both.",
        [
            new BookmarkResumeField("confirmed", BookmarkFieldType.Boolean, "Akkoord", Required: true),
            new BookmarkResumeField("amount", BookmarkFieldType.Number, "Aantal")
        ]);

        BookmarkResumeValidationResult result = BookmarkResumeValidator.Validate(schema, """{"confirmed":"true","amount":"3"}""");

        // The workflow reads these values as themselves, so a string that says true has to arrive as a bool.
        Assert.True(result.IsValid);
        Assert.Equal(true, result.Values["confirmed"]);
        Assert.Equal(3d, result.Values["amount"]);
    }

    [Fact]
    public void AcceptsAnythingWhenThereIsNoSchema()
    {
        BookmarkResumeValidationResult result = BookmarkResumeValidator.Validate(null, """{"anything":1}""");

        // A bookmark nobody described is answered exactly as well as it is today, and no worse.
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ReportsAnswersThatAreNotAJsonObject()
    {
        BookmarkResumeSchema schema = new("Ask whether it can be submitted.",
            [new BookmarkResumeField("confirmed", BookmarkFieldType.Boolean, "Akkoord", Required: true)]);

        BookmarkResumeValidationResult result = BookmarkResumeValidator.Validate(schema, "\"just a string\"");

        // The model chooses this text freely; malformed JSON is a mistake it can correct once it is told so.
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Messages);
    }

    [Fact]
    public void NormalisesADefaultValueTheSameWayAsASuppliedOne()
    {
        BookmarkResumeSchema schema = new("Ask for the number.",
            [new BookmarkResumeField("amount", BookmarkFieldType.Number, "Aantal", DefaultValue: 3)]);

        BookmarkResumeValidationResult result = BookmarkResumeValidator.Validate(schema, "{}");

        // A default that falls back untouched would reach the workflow as a boxed int while every supplied answer
        // arrives as a double - the same field must have the same shape regardless of which path filled it in.
        Assert.True(result.IsValid);
        Assert.Equal(3d, result.Values["amount"]);
    }

    [Fact]
    public void DropsADefaultValueOutsideItsOptionsAndSaysSo()
    {
        BookmarkResumeSchema schema = new("Ask which one.",
            [new BookmarkResumeField("pick", BookmarkFieldType.Choice, "Keuze", DefaultValue: "z",
                Options: [new BookmarkFieldOption("a", "Eerste"), new BookmarkFieldOption("b", "Tweede")])]);

        BookmarkResumeValidationResult result = BookmarkResumeValidator.Validate(schema, "{}");

        // The answering caller did not supply this field and cannot fix the provider's own bad default, so the
        // resume still succeeds - but the value is left out rather than silently resuming with an invalid choice.
        Assert.True(result.IsValid);
        Assert.False(result.Values.ContainsKey("pick"));
        Assert.Contains(result.Messages, message => message.Contains("pick", StringComparison.Ordinal));
    }
}
