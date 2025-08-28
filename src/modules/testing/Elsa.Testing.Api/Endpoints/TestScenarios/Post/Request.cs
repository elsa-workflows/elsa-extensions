using System.ComponentModel.DataAnnotations;
using Elsa.Testing.Core.Abstractions;

namespace Elsa.Testing.Api.Endpoints.TestScenarios.Post;

public class Request
{
    public string? Id { get; set; }
    [Required] public string Name { get; set; } = null!;
    [Required] public string WorkflowDefinitionId { get; set; } = null!;
    public string? Description { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public IDictionary<string, object>? Variables { get; set; }
    public ICollection<Assertion> Assertions { get; set; } = new List<Assertion>();
}