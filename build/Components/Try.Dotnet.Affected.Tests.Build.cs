using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;

namespace Components;

interface ITryDotnetAffectedTestsBuild : INukeBuild
{
    const string ProjectName = "Try.Dotnet.Affected.Tests";

    Target CompileTryDotnetAffectedTests => definition => definition
        .Inherit(Build.BaseTarget(ProjectName))
        .DependsOn<ITryDotnetAffectedBuild>(c => c.CompileTryDotnetAffected)
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

    Target RunTestsTryDotnetAffectedTests => definition => definition
        .Inherit(Build.BaseTarget(ProjectName))
        .DependsOn(CompileTryDotnetAffectedTests)
        .TriggeredBy<Build>(c => c.RunTestsSolution)
        .Executes(() =>
        {
            var project = Build.ProjectsToBuild[ProjectName];
            DotNetTasks.DotNetTest(c => c
                .SetConfiguration(Build.Configuration)
                .SetProjectFile(project)
                .AddLoggers("trx"));
        });
}