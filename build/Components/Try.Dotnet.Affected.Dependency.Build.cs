using Nuke.Common;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NuGet;

namespace Components;

interface ITryDotnetAffectedDependencyBuild : INukeBuild
{
    const string ProjectName = "Try.Dotnet.Affected.Dependency";

    Target CompileTryDotnetAffectedDependency => definition => definition
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
}