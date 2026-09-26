using Elsa.Bookmarks.Ui.Providers;
using Elsa.Bookmarks.Ui.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Bookmarks.Ui.Extensions;

/// <summary>Registers the bookmark UI mapper and its providers.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the mapper and the generic providers. Safe to call more than once, so every feature that needs the mapper
    /// can ask for it without knowing whether another already did.
    /// </summary>
    /// <remarks>
    /// <see cref="FallbackBookmarkUiProvider"/> is not among them. It is the mapper's own last resort rather than a
    /// provider in the chain, so that the mapper still gets to decide that a trigger bookmark is shown as nothing at
    /// all.
    /// </remarks>
    public static IServiceCollection AddBookmarkUi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IBookmarkUiMapper, BookmarkUiMapper>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IBookmarkUiProvider, DelayBookmarkUiProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IBookmarkUiProvider, EventBookmarkUiProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IBookmarkUiProvider, RunTaskBookmarkUiProvider>());

        return services;
    }

    /// <summary>Adds one provider, and the mapper it needs.</summary>
    public static IServiceCollection AddBookmarkUiProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, IBookmarkUiProvider
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBookmarkUi();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IBookmarkUiProvider, TProvider>());

        return services;
    }
}
