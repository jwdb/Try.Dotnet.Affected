using Nuke.Common;
using Pulumi.AzureNative.Resources;

namespace Components;

interface ITryDotnetAffectedDeployBase : INukeBuild
{
    public const string AzureRegion = "westeurope";

    Target DeployStackTryDotnetAffectedDeployBase => definition => definition
        .DependsOn<IAzureTargets>(c => c.AzLogin)
        .DependentFor<IPulumiTargets>(c => c.ProvisionPulumi)
        .Triggers<IPulumiTargets>(c => c.ProvisionPulumi)
        .Executes(() =>
            IPulumiTargets.Stack.With(() =>
            {
                var resourceGroup = new ResourceGroup("poc-nuke-pulumi", new()
                {
                    Location = AzureRegion,
                });

                return new { ResourceGroupName = resourceGroup.Name };
            })
        );
}