using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Chocolatey;

namespace Components;

[ParameterPrefix("Azure")]
interface IAzureTargets : INukeBuild
{
    [Parameter("Tenant ID")]
    string TenantId => TryGetValue(() => TenantId);

    [Parameter("Client ID")]
    string ClientId => TryGetValue(() => ClientId);

    [Parameter("Client Secret")]
    string ClientSecret => TryGetValue(() => ClientSecret);

    Target InstallAzureCli => definition => definition
        .Executes(() => ChocolateyTasks.Chocolatey("upgrade azure-cli -y"));

    Target AzLogin => definition => definition
        .DependsOn(InstallAzureCli)
        .Executes(() =>
        {
            ProcessTasks.StartProcess("az", "login --service-principal -u $env:ARM_CLIENT_ID -p $env:ARM_CLIENT_SECRET --tenant $env:ARM_TENANT_ID",
                    environmentVariables: new Dictionary<string, string>
                    {
                        { "ARM_TENANT_ID", TenantId },
                        { "ARM_CLIENT_SECRET", ClientSecret },
                        { "ARM_CLIENT_ID", ClientId },
                    })
                .WaitForExit();
        });
}