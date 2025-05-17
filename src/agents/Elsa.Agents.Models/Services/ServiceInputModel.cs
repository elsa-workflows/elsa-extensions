using System.ComponentModel.DataAnnotations;

namespace Elsa.Agents;

public class ServiceInputModel
{
    [Required] public string Name { get; set; } = null!;
    [Required] public string Type { get; set; } = null!;
    public IDictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
}