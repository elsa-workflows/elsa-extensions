using System;
using System.Net.Http;
using Azure.Identity;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Integrations.OneDrive.Options;
using Elsa.Integrations.OneDrive.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;

namespace Elsa.Integrations.OneDrive.Features;

/// <summary>
/// A feature that provides OneDrive integration through Microsoft Graph API.
/// </summary>
public class OneDriveFeature : FeatureBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneDriveFeature"/> class.
    /// </summary>
    public OneDriveFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// The OneDrive options configuration.
    /// </summary>
    public Action<OneDriveOptions> ConfigureOneDriveOptions { get; set; } = _ => { };

    /// <summary>
    /// Applies the feature to the specified service collection.
    /// </summary>
    public override void Apply()
    {
        // Register options.
        var oneDriveOptions = new OneDriveOptions();
        ConfigureOneDriveOptions(oneDriveOptions);
        
        // Register OneDrive client factory and GraphServiceClient
        Services.AddSingleton(sp => 
        {
            if (string.IsNullOrEmpty(oneDriveOptions.TenantId) || 
                string.IsNullOrEmpty(oneDriveOptions.ClientId) || 
                string.IsNullOrEmpty(oneDriveOptions.ClientSecret))
            {
                throw new InvalidOperationException("OneDrive options must be configured with TenantId, ClientId, and ClientSecret");
            }
            
            // Create client credential using Azure Identity
            var credentials = new ClientSecretCredential(
                oneDriveOptions.TenantId, 
                oneDriveOptions.ClientId, 
                oneDriveOptions.ClientSecret);
            
            // Build the Microsoft Graph client
            return new GraphServiceClient(credentials, oneDriveOptions.Scopes);
        });
        
        // Register OneDrive client factory
        Services.AddSingleton<OneDriveClientFactory>();
    }
}