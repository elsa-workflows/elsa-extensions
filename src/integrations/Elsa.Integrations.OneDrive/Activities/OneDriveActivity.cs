using Elsa.Integrations.OneDrive.Services;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Microsoft.Graph;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Base class for all OneDrive activities.
/// </summary>
public abstract class OneDriveActivity : CodeActivity
{
    /// <summary>
    /// Gets a GraphServiceClient instance for interacting with OneDrive.
    /// </summary>
    /// <param name="context">The activity execution context.</param>
    /// <returns>A GraphServiceClient instance.</returns>
    protected GraphServiceClient GetGraphClient(ActivityExecutionContext context)
    {
        var factory = context.GetRequiredService<OneDriveClientFactory>();
        return factory.CreateClient();
    }
}

/// <summary>
/// Base class for OneDrive activities that return a result.
/// </summary>
/// <typeparam name="T">The type of the result.</typeparam>
public abstract class OneDriveActivity<T> : CodeActivity<T>
{
    /// <summary>
    /// Gets a GraphServiceClient instance for interacting with OneDrive.
    /// </summary>
    /// <param name="context">The activity execution context.</param>
    /// <returns>A GraphServiceClient instance.</returns>
    protected GraphServiceClient GetGraphClient(ActivityExecutionContext context)
    {
        var factory = context.GetRequiredService<OneDriveClientFactory>();
        return factory.CreateClient();
    }
}