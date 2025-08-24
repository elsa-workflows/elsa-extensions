using Elsa.Testing.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elsa.Testing.Persistence.EFCore;

public class TestScenarioConfigurations : IEntityTypeConfiguration<TestScenario>
{
    public void Configure(EntityTypeBuilder<TestScenario> builder)
    {
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(TestScenario)}_{nameof(TestScenario.Name)}");
        builder.HasIndex(x => x.TenantId).HasDatabaseName($"IX_{nameof(TestScenario)}_{nameof(TestScenario.TenantId)}");
        builder.HasIndex(x => x.WorkflowDefinitionId).HasDatabaseName($"IX_{nameof(TestScenario)}_{nameof(TestScenario.WorkflowDefinitionId)}");
        builder.Ignore(x => x.Assertions);
        builder.Ignore(x => x.Variables);
        builder.Ignore(x => x.Input);
        builder.Property<string>("SerializedAssertions");
    }
}

public class TestSuiteConfiguration : IEntityTypeConfiguration<TestSuite>
{
    public void Configure(EntityTypeBuilder<TestSuite> builder)
    {
        builder.HasIndex(x => x.Name).HasDatabaseName($"IX_{nameof(TestSuite)}_{nameof(TestSuite.Name)}");
        builder.HasIndex(x => x.TenantId).HasDatabaseName($"IX_{nameof(TestSuite)}_{nameof(TestSuite.TenantId)}");
    }
}

public class TestSuiteRunConfiguration : IEntityTypeConfiguration<TestSuiteRun>
{
    public void Configure(EntityTypeBuilder<TestSuiteRun> builder)
    {
        builder.HasIndex(x => x.TestSuiteId).HasDatabaseName($"IX_{nameof(TestSuiteRun)}_{nameof(TestSuiteRun.TestSuiteId)}");
        builder.HasIndex(x => x.Status).HasDatabaseName($"IX_{nameof(TestSuiteRun)}_{nameof(TestSuiteRun.Status)}");
        builder.HasIndex(x => x.StartedAt).HasDatabaseName($"IX_{nameof(TestSuiteRun)}_{nameof(TestSuiteRun.StartedAt)}");
        builder.HasIndex(x => x.TenantId).HasDatabaseName($"IX_{nameof(TestSuiteRun)}_{nameof(TestSuiteRun.TenantId)}");
    }
}

public class TestRunConfiguration : IEntityTypeConfiguration<TestRun>
{
    public void Configure(EntityTypeBuilder<TestRun> builder)
    {
        builder.HasIndex(x => x.TestScenarioId).HasDatabaseName($"IX_{nameof(TestRun)}_{nameof(TestRun.TestScenarioId)}");
        builder.HasIndex(x => x.TestSuiteRunId).HasDatabaseName($"IX_{nameof(TestRun)}_{nameof(TestRun.TestSuiteRunId)}");
        builder.HasIndex(x => x.WorkflowInstanceId).HasDatabaseName($"IX_{nameof(TestRun)}_{nameof(TestRun.WorkflowInstanceId)}");
        builder.HasIndex(x => x.Status).HasDatabaseName($"IX_{nameof(TestRun)}_{nameof(TestRun.Status)}");
        builder.HasIndex(x => x.StartedAt).HasDatabaseName($"IX_{nameof(TestRun)}_{nameof(TestRun.StartedAt)}");
        builder.HasIndex(x => x.TenantId).HasDatabaseName($"IX_{nameof(TestRun)}_{nameof(TestRun.TenantId)}");
    }
}