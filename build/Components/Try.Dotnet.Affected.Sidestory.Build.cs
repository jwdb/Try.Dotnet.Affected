using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;

namespace Components;

interface ITryDotnetAffectedSidestoryBuild : INukeBuild
{
    const string ProjectName = "Try.Dotnet.Affected.Sidestory";
    public AbsolutePath OutputDirectory => RootDirectory / "output" / ProjectName;

    Target CompileTryDotnetAffectedSidestory => definition => definition
        .Inherit(Build.BaseTarget(ProjectName))
        .TriggeredBy<Build>(c => c.CompileSolution)
        .Executes(() =>
    {
        var project = Build.ProjectsToBuild[ProjectName];
        
        MSBuildTasks.MSBuild(settings => settings
            .SetTargetPath(project)
            .SetTargets("Build")
            .SetConfiguration(Build.Configuration)
            .EnableRestore()
            .EnableNodeReuse());
    });

    Target PublishTryDotnetAffectedSidestory => definition => definition
        .DependsOn(CompileTryDotnetAffectedSidestory)
        .Inherit(Build.BaseTarget(ProjectName))
        .TriggeredBy<Build>(c => c.PublishSolution)
        .Executes(() =>
        {
            var project = Build.ProjectsToBuild[ProjectName];
            DotNetTasks.DotNetPublish(s => s
                .SetOutput(OutputDirectory)
                .SetProject(project)
                .SetConfiguration(Build.Configuration)
                .EnableNoRestore());
        });
}