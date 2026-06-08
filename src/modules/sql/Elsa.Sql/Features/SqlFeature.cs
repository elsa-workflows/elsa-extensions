using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Sql.Activities;
using Elsa.Sql.Contracts;
using Elsa.Sql.Factory;
using Elsa.Sql.Handlers;
using Elsa.Sql.Providers;
using Elsa.Sql.Services;
using Elsa.Sql.UIHints;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Sql.Features;

/// <summary>
/// Setup SQL client features
/// </summary>
public class SqlFeature : FeatureBase
{
    /// <summary>
    /// Set a callback to configure <see cref="ClientStore"/>.
    /// </summary>
    public Action<ClientStore> Clients { get; set; } = _ => { };

    public Action<ClientStore> ResultTypes { get; set; } = _ => { };

    /// <summary>
    ///  <inheritdoc/>
    /// </summary>
    /// <param name="module"></param>
    public SqlFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override void Configure()
    {
        Module.AddActivitiesFrom<SqlFeature>();
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override void Apply()
    {
        Services
            // Services
            .AddSingleton(provider =>
            {
                ClientStore clientRegistry = new();

                Clients.Invoke(clientRegistry);
                ResultTypes.Invoke(clientRegistry);

                if (clientRegistry.ResultTypes.Count == 0)
                    clientRegistry.RegisterResultHandler<SqlClientRecordSetResultHandler>(SqlClientRecordSetResultHandler.Name);

                if (!clientRegistry.ResultTypes.ContainsKey(SqlClientDataSetResultHandler.Name))
                    clientRegistry.RegisterResultHandler<SqlClientDataSetResultHandler>(SqlClientDataSetResultHandler.Name);

                return clientRegistry;
            })
            .AddSingleton<ISqlClientFactory, SqlClientFactory>()
            .AddSingleton<ISqlClientResultTypeFactory, SqlClientResultTypeFactory>()
            .AddScoped<ISqlEvaluator, SqlEvaluator>()

            // Providers
            .AddExpressionDescriptorProvider<SqlExpressionDescriptorProvider>()
            .AddScoped<IPropertyUIHandler, SqlCodeOptionsProvider>()
            .AddScoped<IPropertyUIHandler, SqlClientsDropDownProvider>()
            .AddScoped<IPropertyUIHandler, SqlClientResultTypesDropDownProvider>()
            .AddScoped<ISqlClientNamesProvider, SqlClientNamesProvider>()
            .AddScoped<ISqlClientResultTypesProvider, SqlClientResultTypesProvider>()
            .AddScoped<ISqlClientResultTypeHandler, SqlClientDataSetResultHandler>();

        


    }
}