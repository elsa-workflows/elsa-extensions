using Elsa.Persistence.EFCore;
using Elsa.Testing.Core.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Testing.Persistence.EFCore;

/// <summary>
/// DB context for the testing module.
/// </summary>
[UsedImplicitly]
public class TestingDbContext : ElsaDbContextBase
{
    /// <inheritdoc />
    public TestingDbContext(DbContextOptions<TestingDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }
    
    /// <summary>
    /// The TestScenarios DB set.
    /// </summary>
    public DbSet<TestScenario> TestScenarios { get; set; } = null!;
    /// <summary>
    /// The TestSuites DB set.
    /// </summary>
    public DbSet<TestSuite> TestSuites { get; set; } = null!;
    /// <summary>
    /// The TestSuiteRuns DB set.
    /// </summary>
    public DbSet<TestSuiteRun> TestSuiteRuns { get; set; } = null!;
    /// <summary>
    /// The TestRuns DB set.
    /// </summary>
    public DbSet<TestRun> TestRuns { get; set; } = null!;
    
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TestScenarioConfigurations());
        modelBuilder.ApplyConfiguration(new TestSuiteConfiguration());
        modelBuilder.ApplyConfiguration(new TestSuiteRunConfiguration());
        modelBuilder.ApplyConfiguration(new TestRunConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}