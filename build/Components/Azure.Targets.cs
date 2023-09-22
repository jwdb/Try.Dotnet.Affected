using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Chocolatey;

namespace Components;

interface IAzureTargets : INukeBuild
{
    [PathVariable("az")] static Tool Az;

    Target InstallAzureCli => definition => definition
        .Executes(() => ChocolateyTasks.Chocolatey("upgrade azure-cli -y"));

    Target AzLogin => definition => definition
        .DependsOn(InstallAzureCli)
        .Executes(() =>
        {
            Az("login --service-principal -u $env:ARM_CLIENT_ID -p $env:ARM_CLIENT_SECRET --tenant $env:ARM_TENANT_ID");
        });
}