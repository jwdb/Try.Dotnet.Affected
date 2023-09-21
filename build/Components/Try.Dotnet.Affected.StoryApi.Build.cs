using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Pulumi.AzureNative.Web;

namespace Components;

interface ITryDotnetAffectedStoryApiBuild : INukeBuild
{
    const string ProjectName = "Try.Dotnet.Affected.StoryApi";
    public AbsolutePath OutputDirectory => RootDirectory / "output" / ProjectName;
    public AbsolutePath PublishArtifact => RootDirectory / "artifacts" / ProjectName / "api.zip";

    static string PublishingUsername;
    static string PublishingPassword;
    static string PublishingAppServiceName;

    Target CleanTryDotnetAffectedStoryApi => definition => definition
        .Inherit(Build.BaseTarget(ProjectName))
        .TriggeredBy<Build>(c => c.Clean)
        .Executes(() =>
        {
            File.Delete(PublishArtifact);
        });


    Target CompileTryDotnetAffectedStoryApi => definition => definition
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

    Target PublishTryDotnetAffectedStoryApi => definition => definition
        .Inherit(Build.BaseTarget(ProjectName))
        .DependsOn(CompileTryDotnetAffectedStoryApi, CleanTryDotnetAffectedStoryApi)
        .TriggeredBy<Build>(c => c.PublishSolution)
        .Executes(() =>
        {
            var project = Build.ProjectsToBuild[ProjectName];

            DotNetTasks.DotNetPublish(s => s
                .SetOutput(OutputDirectory)
                .SetProject(project)
                .SetConfiguration(Build.Configuration)
                .EnableNoRestore());

            Directory.CreateDirectory(Path.GetDirectoryName(PublishArtifact)!);
            ZipFile.CreateFromDirectory(OutputDirectory, PublishArtifact);
        });

    Target DeployStackTryDotnetAffectedStoryApi => definition => definition
        .After<ITryDotnetAffectedDeployBase>(c => c.DeployStackTryDotnetAffectedDeployBase)
        .Before<IPulumiTargets>(c => c.ProvisionPulumi)
        .DependentFor<IPulumiTargets>(c => c.ProvisionPulumi)
        .Executes(() => 
            IPulumiTargets.Stack.With(() =>
            {
                var resourceGroupName = IPulumiTargets.Stack.GetOutput<string>("ResourceGroupName");
                var appService = new WebApp("app", new WebAppArgs
                {
                    ResourceGroupName = resourceGroupName,
                });

                var publishingCredentials = ListWebAppPublishingCredentials.Invoke(new()
                {
                    ResourceGroupName = resourceGroupName,
                    Name = appService.Name
                });

                publishingCredentials.Apply(c => PublishingUsername = c.PublishingUserName);
                publishingCredentials.Apply(c => PublishingPassword = c.PublishingPassword);
                appService.Name.Apply(c => PublishingAppServiceName = c);

                return new { AppServiceName = appService.Name };
            })
        );

    Target PublishStackTryDotnetAffectedStoryApi => definition => definition
        .Inherit(Build.BaseTarget(ProjectName))
        .DependsOn(DeployStackTryDotnetAffectedStoryApi)
        .DependsOn<IPulumiTargets>(c => c.ProvisionPulumi)
        .TriggeredBy<Build>(c => c.PublishSolution)
        .Executes(async () =>
        {
            var base64Auth = Convert.ToBase64String(Encoding.Default.GetBytes($"{PublishingUsername}:{PublishingPassword}"));

            await using var package = File.OpenRead(PublishArtifact);
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);
            await httpClient.PostAsync($"https://{PublishingAppServiceName}.scm.azurewebsites.net/api/zipdeploy",
                new StreamContent(package));
        });
}