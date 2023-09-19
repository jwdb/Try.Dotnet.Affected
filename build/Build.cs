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
class Build : NukeBuild,
    IDotnetAffectedTargets,
    ITryDotnetAffectedBuild,
    ITryDotnetAffectedDependencyBuild,
    ITryDotnetAffectedSidestoryBuild,
    ITryDotnetAffectedTestsBuild
{
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    public static readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;


    public static Dictionary<string, string> ProjectsToBuild = new();

    public static int Main() => Execute<Build>(x => x.CompileSolution);

    Target Clean => definition => definition
        .Executes(() =>
        {
            File.Delete("affected.json"); 
        });

    Target Restore => definition => definition
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

    public static Func<string, Target> BaseTarget => projectName => 
        definition => definition
        .DependsOn<IDotnetAffectedTargets>(c => c.DotnetAffected)
        .OnlyWhenDynamic(() => ProjectsToBuild.ContainsKey(projectName), $"{projectName} is affected");
}
