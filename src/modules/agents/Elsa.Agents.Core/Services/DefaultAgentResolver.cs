namespace Elsa.Agents;

/// <summary>
/// Default implementation of <see cref="IAgentResolver"/> that queries a
/// collection of <see cref="IAgentProvider"/> instances to resolve agents by
/// name. The first provider that reports it can handle the name is used.
/// </summary>
public class DefaultAgentResolver(IEnumerable<IAgentProvider> providers) : IAgentResolver
{
    public async Task<IElsaAgent> ResolveAsync(string name, CancellationToken cancellationToken = default)
    {
        foreach (var provider in providers)
        {
            if (!await provider.CanProvideAsync(name, cancellationToken))
                continue;

            return await provider.CreateAsync(name, cancellationToken);
        }

        throw new InvalidOperationException($"No agent provider could resolve an agent named '{name}'.");
    }
}

