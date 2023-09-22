using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Chocolatey;

// ReSharper disable InconsistentNaming
namespace Components;

interface IAzureTargets : INukeBuild
{
    [PathVariable("az")] static Tool Az;

    [Parameter][Secret] string ARM_CLIENT_ID => TryGetValue(() => ARM_CLIENT_ID);
    [Parameter][Secret] string ARM_CLIENT_SECRET => TryGetValue(() => ARM_CLIENT_SECRET);
    [Parameter][Secret] string ARM_TENANT_ID => TryGetValue(() => ARM_TENANT_ID);

    Target InstallAzureCli => definition => definition
        .Executes(() => ChocolateyTasks.Chocolatey("upgrade azure-cli -y"));

    Target AzLogin => definition => definition
        .DependsOn(InstallAzureCli)
        .Executes(() =>
        {
            Az(new Arguments()
                .Add("login")
                .Add("--service-principal")
                .Add("-u {value}", ARM_CLIENT_ID, secret: true)
                .Add("-p {value}", ARM_CLIENT_SECRET, secret: true)
                .Add("--tenant {value}", ARM_TENANT_ID, secret: true)
                .RenderForExecution());
        });
}