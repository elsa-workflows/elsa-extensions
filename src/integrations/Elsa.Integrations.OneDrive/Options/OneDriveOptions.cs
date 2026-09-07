using System;

namespace Elsa.Integrations.OneDrive.Options;

/// <summary>
/// Options for configuring OneDrive integration.
/// </summary>
public class OneDriveOptions
{
    /// <summary>
    /// The Azure AD tenant ID (directory ID).
    /// </summary>
    public string? TenantId { get; set; }
    
    /// <summary>
    /// The Azure AD application (client) ID.
    /// </summary>
    public string? ClientId { get; set; }
    
    /// <summary>
    /// The client secret of the Azure AD application.
    /// </summary>
    public string? ClientSecret { get; set; }
    
    /// <summary>
    /// The scopes required for OneDrive API access.
    /// </summary>
    public string[] Scopes { get; set; } = { "https://graph.microsoft.com/.default" };
}