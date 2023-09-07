using Nuke.Common;
using Nuke.Common.Tools.MSBuild;

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
            MSBuildTasks.MSBuild(settings => settings
                .SetTargetPath(project)
                .SetTargets("Build")
                .SetConfiguration(Build.Configuration)
                .EnableNodeReuse());
    });
}