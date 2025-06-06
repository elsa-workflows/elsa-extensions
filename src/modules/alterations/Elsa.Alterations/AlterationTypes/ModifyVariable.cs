using Elsa.Alterations.Core.Abstractions;
using JetBrains.Annotations;

namespace Elsa.Alterations.AlterationTypes;

/// <summary>
/// Modifies a variable.
/// </summary>
[UsedImplicitly]
public class ModifyVariable : AlterationBase
{
    /// <summary>
    /// The ID of the variable to modify.
    /// </summary>
    public string VariableId { get; set; } = null!;

    /// <summary>
    ///  The new value of the variable.
    /// </summary>
    public object? Value { get; set; }

}