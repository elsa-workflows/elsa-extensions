using System.Globalization;
using System.Text;

namespace Elsa.Mcp.Abstractions;

/// <summary>
/// The name an MCP tool derived from a workflow answers to.
/// </summary>
/// <remarks>
/// This lives apart from the server that serves the tool because an editor has to show the author the same name the
/// model will see. A workflow drawn in the designer has a generated definition id, and naming a tool after that put
/// two unrelated tools side by side as indistinguishable hex strings — which is how a request to start a PR review
/// ended up starting something else.
/// </remarks>
public static class McpToolName
{
    /// <summary>
    /// The longest tool name a client accepts. Claude constrains a tool name to <c>^[a-zA-Z0-9_-]{1,64}$</c>, which
    /// is the tightest limit among the clients this is used from, so it is the one applied to all of them.
    /// </summary>
    public const int MaxLength = 64;

    /// <summary>
    /// Reduces a workflow name to a tool name: accents folded, everything a client will not accept turned into a
    /// separator. Returns <c>null</c> when nothing usable is left, which a name written entirely in symbols does.
    /// </summary>
    public static string? FromWorkflowName(string? name, int maxLength = MaxLength)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        // Decomposing first turns "é" into "e" plus a combining accent, so folding is a matter of dropping the
        // accent. Without it the whole character is unacceptable and "één" would be reduced to "n" — the wrong word.
        string decomposed = name.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new(Math.Min(decomposed.Length, maxLength));
        bool separatorPending = false;

        foreach (char character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            if (!IsToolNameCharacter(character))
            {
                separatorPending = true;
                continue;
            }

            if (separatorPending && builder.Length > 0)
                builder.Append('-');

            separatorPending = false;
            builder.Append(char.ToLowerInvariant(character));

            if (builder.Length >= maxLength)
                break;
        }

        return builder.Length > 0 ? builder.ToString() : null;
    }

    /// <summary>
    /// Whether a name written by an author is one a client will call: letters, digits, underscores and dashes, within
    /// <see cref="MaxLength"/>.
    /// </summary>
    public static bool IsAcceptable(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return name.Length is > 0 and <= MaxLength
            && name.All(character => IsToolNameCharacter(character) || character is '_' or '-');
    }

    /// <summary>
    /// Appends the definition id to a name shared by more than one workflow, trimming the name rather than the id:
    /// the id is what makes the result unique, so it is the half that cannot be cut.
    /// </summary>
    public static string Disambiguate(string preferred, string definitionId)
    {
        ArgumentNullException.ThrowIfNull(preferred);
        ArgumentNullException.ThrowIfNull(definitionId);

        int room = MaxLength - definitionId.Length - 1;

        if (room <= 0)
            return definitionId;

        string trimmed = preferred.Length <= room ? preferred : preferred[..room].TrimEnd('-');

        return trimmed.Length > 0 ? $"{trimmed}-{definitionId}" : definitionId;
    }

    private static bool IsToolNameCharacter(char character) =>
        character is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9';
}
