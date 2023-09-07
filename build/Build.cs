using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Components;
using Nuke.Common;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

[SuppressMessage("ReSharper", "UnusedMember.Local")]
[GitHubActions(
    "continuous",
    GitHubActionsImage.WindowsLatest,
    On = new[] { GitHubActionsTrigger.Push },
    InvokedTargets = new[] { nameof(CompileSolution) })]
class Build : NukeBuild,
    ITryDotnetAffectedBuild,
    ITryDotnetAffectedDependencyBuild,
    ITryDotnetAffectedSidestoryBuild,
    ITryDotnetAffectedTestsBuild
{
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    public static readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    AzurePipelines AzurePipelines => AzurePipelines.Instance;
    GitHubActions GitHubActions => GitHubActions.Instance;

    public static Dictionary<string, string> ProjectsToBuild = new();

    public static int Main() => Execute<Build>(x => x.CompileSolution);

    Target Clean => definition => definition
        .Before(Restore)
        .Executes(() =>
        {
            File.Delete("affected.json");
        });

    Target Restore => definition => definition.Executes(() => { });

    // ReSharper disable once ClassNeverInstantiated.Local
    record AffectedJson(string Name, string FilePath);

    public Target DotnetAffected => definition => definition
        .Executes(() =>
        {
            var args = new Arguments()
                .Add("tool")
                .Add("run")
                .Add("dotnet-affected")
                .Add("--format json")
                .Add("--verbose", condition: Verbosity >= Verbosity.Normal);

            if (AzurePipelines != null)
            {
                args.Add("--from", AzurePipelines.PullRequestSourceBranch)
                    .Add("--to", AzurePipelines.PullRequestTargetBranch);
            }

            if (GitHubActions is { IsPullRequest: true })
            {
                args.Add("--from", GitHubActions.HeadRef)
                    .Add("--to", GitHubActions.BaseRef);
            }

            DotNetTasks.DotNetToolRestore();
            DotNetTasks.DotNet(args.ToString(), exitHandler: DotnetToolExitHandler);

            var toBuildJson = File.ReadAllText("affected.json");
            var affectedProjects = JsonSerializer.Deserialize<AffectedJson[]>(toBuildJson);
            foreach (var project in affectedProjects)
            {
                ProjectsToBuild.Add(project.Name, project.FilePath);
            }
        });

    public Target CompileSolution => definition => definition
        .DependsOn(Restore, DotnetAffected)
        .Executes(() => { });

    public Target PublishSolution => definition => definition
        .DependsOn(CompileSolution)
        .Executes(() => { });

    public Target RunTestsSolution => definition => definition
        .DependsOn(CompileSolution)
        .Executes(() => { });

    public static Func<string, Target> BaseTarget => projectName => 
        definition => definition
        .DependsOn<Build>(c => c.DotnetAffected)
        .OnlyWhenDynamic(() => ProjectsToBuild.ContainsKey(projectName), $"{projectName} is affected");

    void DotnetToolExitHandler(IProcess obj)
    {
        switch (obj.ExitCode)
        {
            case 0: // Success
            case 166: // No changed projects
                return;
            default:
                throw new Exception();
        }
    }
}
