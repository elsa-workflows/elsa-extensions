using System.Collections.Generic;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.GitHubActions.Configuration;
using Nuke.Common.Execution;
using Nuke.Common.Utilities;
using Nuke.Components;

[CustomGitHubActions(
        "pr",
        GitHubActionsImage.UbuntuLatest,
        OnPullRequestBranches = ["main"],
        OnPullRequestIncludePaths = ["**/*"],
        PublishArtifacts = false,
        InvokedTargets = [nameof(ICompile.Compile), nameof(ITest.Test), nameof(IPack.Pack)],
        CacheKeyFiles = [],
        ConcurrencyCancelInProgress = true
    )
]
public partial class Build;

class CustomGitHubActionsAttribute : GitHubActionsAttribute
{
    public CustomGitHubActionsAttribute(string name, GitHubActionsImage image, params GitHubActionsImage[] images) : base(name, image, images)
    {
    }

    protected override GitHubActionsJob GetJobs(GitHubActionsImage image, IReadOnlyCollection<ExecutableTarget> relevantTargets)
    {
        var job = base.GetJobs(image, relevantTargets);

        var newSteps = new List<GitHubActionsStep>(job.Steps);

        // only need to list the ones that are missing from default image
        newSteps.Insert(0, new GitHubActionsSetupDotNetStep(["9.x"]));
        newSteps.Insert(2, new GitHubActionsRunStep("Interface filename convention check", "git fetch origin \"${{ github.base_ref }}\" --depth=1\npython3 .github/scripts/check_csharp_interface_filenames.py $(git diff --name-only --diff-filter=ACMRT \"origin/${{ github.base_ref }}...HEAD\")"));
        newSteps.Insert(3, new GitHubActionsRunStep("Non-nullable initializer convention check", "git fetch origin \"${{ github.base_ref }}\" --depth=1\npython3 .github/scripts/check_csharp_nonnullable_property_initializers.py $(git diff --name-only --diff-filter=ACMRT \"origin/${{ github.base_ref }}...HEAD\")"));

        job.Steps = newSteps.ToArray();
        return job;
    }
}

class GitHubActionsSetupDotNetStep : GitHubActionsStep
{
    public GitHubActionsSetupDotNetStep(string[] versions)
    {
        Versions = versions;
    }

    string[] Versions { get; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- uses: actions/setup-dotnet@v4");

        using (writer.Indent())
        {
            writer.WriteLine("with:");
            using (writer.Indent())
            {
                writer.WriteLine("dotnet-version: |");
                using (writer.Indent())
                {
                    foreach (var version in Versions)
                    {
                        writer.WriteLine(version);
                    }
                }
            }
        }
    }
}

class GitHubActionsRunStep : GitHubActionsStep
{
    public GitHubActionsRunStep(string name, string script)
    {
        Name = name;
        Script = script;
    }

    string Name { get; }
    string Script { get; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine($"- name: 'Run: {Name}'");
        writer.WriteLine("  run: |");

        using (writer.Indent())
        {
            using (writer.Indent())
            {
                foreach (var line in Script.Split('\n'))
                {
                    writer.WriteLine(line);
                }
            }
        }
    }
}