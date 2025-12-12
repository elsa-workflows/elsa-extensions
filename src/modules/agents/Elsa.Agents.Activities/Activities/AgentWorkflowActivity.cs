using System.ComponentModel;
using System.Dynamic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Elsa.Agents;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Agents.Activities.ActivityProviders;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Serialization.Converters;

namespace Elsa.Agents.Activities;

/// <summary>
/// Deprecated: use <see cref="AgentActivity"/> instead. AgentActivity now supports
/// multi-agent workflows via IAgentResolver, so this type is kept only for
/// backward compatibility and should not be used in new code.
/// </summary>
[Browsable(false)]
[Obsolete("Use AgentActivity instead. AgentActivity resolves both single agents and workflows via IAgentResolver.")]
public class AgentWorkflowActivity : CodeActivity
{
    /// <inheritdoc />
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context) =>
        throw new NotSupportedException("AgentWorkflowActivity is deprecated. Use AgentActivity instead.");
}
