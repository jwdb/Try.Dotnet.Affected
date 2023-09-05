using System;
using System.Collections.Generic;
using System.IO;
using Nuke.Common;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;

class Build : NukeBuild
{
    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;
    AzurePipelines AzurePipelines => AzurePipelines.Instance;

    AbsolutePath OutputDirectory => RootDirectory / "output";

    public List<string> projectsBuilt = new();

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            File.Delete("affected.txt");
        });

    Target Restore => _ => _
        .Executes(() =>
        {
        });

    Target DotnetAffected => _ => _
        .DependsOn(Clean, Restore)
        .Executes(() =>
    {
        var args = new Arguments()
            .Add("tool")
            .Add("run")
            .Add("dotnet-affected")
            .Add("--format text")
            .Add("--verbose", condition: Verbosity >= Verbosity.Normal);

        if (AzurePipelines != null)
        {
            args.Add("--from", AzurePipelines.PullRequestSourceBranch)
                .Add("--to", AzurePipelines.PullRequestTargetBranch);
        }

        DotNetTasks.DotNet(args.ToString(), exitHandler: DotnetToolExitHandler);
    });

    Target Compile => _ => _
        .DependsOn(Restore, DotnetAffected)
        .Executes(() =>
        {
            var toBuild = File.ReadAllLines("affected.txt");
            foreach (var project in toBuild)
            {
                MSBuildTasks.MSBuild(_ => _
                    .SetTargetPath(project)
                    .SetTargets("Build")
                    .SetConfiguration(Configuration)
                    .EnableNodeReuse());

                projectsBuilt.Add(project);
            }
        });

    Target PublishProject => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            foreach (var project in projectsBuilt)
            {
                var msbuildProject = ProjectModelTasks.ParseProject(project);

                var projectName = msbuildProject.GetPropertyValue("MSBuildProjectName");

                DotNetTasks.DotNetPublish(s => s
                    .SetOutput(OutputDirectory / projectName)
                    .SetProject(project)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore());
            }

        });

    Target RunTests => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            foreach (var project in projectsBuilt)
            {
                DotNetTasks.DotNetTest(c => c.SetConfiguration(Configuration).SetProjectFile(project)
                    .AddLoggers("trx"));
            }
        });

void DotnetToolExitHandler(IProcess obj)
    {
        if (obj.ExitCode == 0) return;
        if (obj.ExitCode == 166) return; // No changed projects

        throw new Exception();
    }
}
