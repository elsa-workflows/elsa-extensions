using Elsa.Studio.Http.Webhooks.Client;
using FluentValidation;

namespace Elsa.Studio.Http.Webhooks.UI.Validators;

public class WebhookSinkInputValidator : AbstractValidator<WebhookSinkEditorModel>
{
    public WebhookSinkInputValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Please enter a sink name.");

        RuleFor(x => x.TargetUrl)
            .NotEmpty()
            .WithMessage("Please enter a target URL.")
            .Must(BeValidUrl)
            .WithMessage("Please enter a valid absolute URL.");
    }

    private static bool BeValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}
