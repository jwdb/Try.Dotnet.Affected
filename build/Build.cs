using System;
using System.Collections.Generic;
using System.IO;
using Components;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;

[GitHubActions(
    "continuous",
    GitHubActionsImage.WindowsLatest,
    On = new[] { GitHubActionsTrigger.Push },
    FetchDepth = 0,
    InvokedTargets = new[] { nameof(CompileSolution) })]
[GitHubActions(
    "deploy",
    GitHubActionsImage.WindowsLatest,
    On = new[] { GitHubActionsTrigger.WorkflowDispatch },
    FetchDepth = 0,
    InvokedTargets = new[] { nameof(DeploySolution) })]
class Build : NukeBuild,
    IDotnetAffectedTargets,
    IPulumiTargets,
    IAzureTargets,
    ITryDotnetAffectedDeployBase,
    ITryDotnetAffectedBuild,
    ITryDotnetAffectedDependencyBuild,
    ITryDotnetAffectedSidestoryBuild,
    ITryDotnetAffectedTestsBuild,
    ITryDotnetAffectedStoryApiBuild,
    ITryDotnetAffectedProxyStoryApiBuild
{
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    public static readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    public static Dictionary<string, string> ProjectsToBuild = new();

    public static int Main() => Execute<Build>(x => x.DeploySolution);

    public Target Clean => definition => definition
        .Executes(() =>
        {
            File.Delete("affected.json"); 
        });

    public Target Restore => definition => definition
        .DependsOn(Clean)
        .Executes(() => { });

    public Target CompileSolution => definition => definition
        .DependsOn(Restore)
        .DependsOn<IDotnetAffectedTargets>(c => c.DotnetAffected)
        .Executes(() => { });

    public Target PublishSolution => definition => definition
        .DependsOn(CompileSolution)
        .Executes(() => { });

    public Target RunTestsSolution => definition => definition
        .DependsOn(CompileSolution)
        .Executes(() => { });

    public Target DeploySolution => definition => definition
        .DependsOn(PublishSolution)
        .Executes(() => { });

    public static Func<string, Target> BaseTarget => projectName => 
        definition => definition
        .DependsOn<IDotnetAffectedTargets>(c => c.DotnetAffected)
        .OnlyWhenDynamic(() => ProjectsToBuild.ContainsKey(projectName), $"{projectName} is affected");
}