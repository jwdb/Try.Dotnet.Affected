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
using Pulumi.AzureNative.Web.Inputs;

namespace Components;

interface ITryDotnetAffectedProxyStoryApiBuild : INukeBuild
{
    const string ProjectName = "Try.Dotnet.Affected.ProxyStoryApi";
    AbsolutePath OutputDirectory => RootDirectory / "output" / ProjectName;
    AbsolutePath PublishArtifact => RootDirectory / "artifacts" / ProjectName / "api.zip";

    static string PublishingUsername;
    static string PublishingPassword;
    static string PublishingAppServiceName;

    Target CleanTryDotnetAffectedProxyStoryApi => definition => definition
        .Inherit(Build.BaseTarget(ProjectName))
        .TriggeredBy<Build>(c => c.Clean)
        .Executes(() =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PublishArtifact)!);
            File.Delete(PublishArtifact);
        });

    Target CompileTryDotnetAffectedProxyStoryApi => definition => definition
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

    Target PublishTryDotnetAffectedProxyStoryApi => definition => definition
        .Inherit(Build.BaseTarget(ProjectName))
        .DependsOn(CompileTryDotnetAffectedProxyStoryApi, CleanTryDotnetAffectedProxyStoryApi)
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

    Target DeployStackTryDotnetAffectedProxyStoryApi => definition => definition
        .After<ITryDotnetAffectedDeployBase>(c => c.DeployStackTryDotnetAffectedDeployBase)
        .After<ITryDotnetAffectedStoryApiBuild>(c => c.DeployStackTryDotnetAffectedStoryApi)
        .Before<IPulumiTargets>(c => c.ProvisionPulumi)
        .DependentFor<IPulumiTargets>(c => c.ProvisionPulumi)
        .Executes(() => 
            IPulumiTargets.Stack.With(() =>
            {
                var resourceGroupName = IPulumiTargets.Stack.GetOutput<string>("ResourceGroupName");
                var storyApiName = IPulumiTargets.Stack.GetOutput<string>("StoryApiName").Apply(c => $"https://{c}.azurewebsites.net");
                var appService = new WebApp("app-proxy", new WebAppArgs
                {
                    ResourceGroupName = resourceGroupName,
                    Location = ITryDotnetAffectedDeployBase.AzureRegion,
                    SiteConfig = new SiteConfigArgs
                    {
                        AppSettings =
                        {
                            new NameValuePairArgs
                            {
                                Name = "StoryApiUrl",
                                Value = storyApiName,
                            }
                        }
                    }
                });

                var publishingCredentials = ListWebAppPublishingCredentials.Invoke(new()
                {
                    ResourceGroupName = resourceGroupName,
                    Name = appService.Name
                });

                publishingCredentials.Apply(c => PublishingUsername = c.PublishingUserName);
                publishingCredentials.Apply(c => PublishingPassword = c.PublishingPassword);
                appService.Name.Apply(c => PublishingAppServiceName = c);
            })
        );

    Target PublishStackTryDotnetAffectedProxyStoryApi => definition => definition
        .Inherit(Build.BaseTarget(ProjectName))
        .DependsOn(DeployStackTryDotnetAffectedProxyStoryApi, PublishTryDotnetAffectedProxyStoryApi)
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