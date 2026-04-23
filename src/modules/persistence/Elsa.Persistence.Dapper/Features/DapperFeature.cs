using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Persistence.Dapper.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Dapper.Features;

/// <summary>
/// Configures common Dapper features.
/// </summary>
public class DapperFeature : FeatureBase
{
    /// <inheritdoc />
    public DapperFeature(IModule module) : base(module)
    {
    }
    
    /// <summary>
    /// Gets or sets a factory that provides an <see cref="IDbConnectionProvider"/> instance.
    /// </summary>
    public Func<IServiceProvider, IDbConnectionProvider> DbConnectionProvider { get; set; }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton(DbConnectionProvider);
    }
}