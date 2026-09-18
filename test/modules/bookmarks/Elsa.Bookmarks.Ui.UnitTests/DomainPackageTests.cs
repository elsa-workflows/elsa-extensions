using System.Reflection;
using Elsa.Bookmarks.Ui.Models;
using Elsa.Bookmarks.Ui.Services;

namespace Elsa.Bookmarks.Ui.UnitTests;

/// <summary>
/// The domain half has to stay Elsa-free, because that is what the package exists for: a Blazor host that only
/// needs to know the shape of a view must not have to pull in the Elsa engine. One stray using silently undoes
/// that split, and nothing but this test would notice.
/// </summary>
public class DomainPackageTests
{
    [Fact]
    public void TheDomainPackageReferencesNoElsaAssembly()
    {
        Assembly domain = typeof(BookmarkUiView).Assembly;

        Assert.Equal("Elsa.Bookmarks.Ui.Domain", domain.GetName().Name);

        string[] elsaReferences =
        [
            .. domain.GetReferencedAssemblies()
                .Select(reference => reference.Name!)
                .Where(name => name.StartsWith("Elsa.", StringComparison.Ordinal))
        ];

        Assert.Empty(elsaReferences);
    }

    [Fact]
    public void TheInterfacesTravelWithTheDomainPackage()
    {
        // Not just the models: a consumer that reads a view reads it through these interfaces. Leave them in the
        // Elsa half and the domain package is unusable in practice.
        Assembly domain = typeof(BookmarkUiView).Assembly;

        Assert.Same(domain, typeof(IBookmarkUiMapper).Assembly);
        Assert.Same(domain, typeof(IBookmarkUiProvider).Assembly);
        Assert.Same(domain, typeof(IBookmarkResumeWriter).Assembly);
    }
}
