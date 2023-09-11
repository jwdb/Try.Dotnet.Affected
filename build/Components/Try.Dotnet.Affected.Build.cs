using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NuGet;

namespace Components;

interface ITryDotnetAffectedBuild : INukeBuild
{
    const string ProjectName = "Try.Dotnet.Affected";
    public AbsolutePath OutputDirectory => RootDirectory / "output" / ProjectName;

    Target CompileTryDotnetAffected => definition => definition
        .Inherit(Build.BaseTarget(ProjectName))
        .TriggeredBy<Build>(c => c.CompileSolution)
        .Executes(() =>
        {
            var project = Build.ProjectsToBuild[ProjectName];

            NuGetTasks.NuGetRestore(settings => settings.SetTargetPath(project));
            MSBuildTasks.MSBuild(settings => settings
                .SetTargetPath(project)
                .SetTargets("Build")
                .SetConfiguration(Build.Configuration)
                .EnableNodeReuse());
    });

    Target PublishTryDotnetAffected => definition => definition
        .Inherit(Build.BaseTarget(ProjectName))
        .DependsOn(CompileTryDotnetAffected)
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