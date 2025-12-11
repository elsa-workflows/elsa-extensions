namespace Elsa.Agents;

public class AgentsOptions
{
    public ICollection<ApiKeyConfig> ApiKeys { get; set; } = new List<ApiKeyConfig>();
    public ICollection<ServiceConfig> Services { get; set; } = new List<ServiceConfig>();
    public ICollection<AgentConfig> Agents { get; set; } = new List<AgentConfig>();
}