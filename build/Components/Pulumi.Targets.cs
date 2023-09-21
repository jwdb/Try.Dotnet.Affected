using Nuke.Common;
using Nuke.Common.Tooling;
using Pulumi.Automation;
using System.Collections.Generic;
using System;
using System.Linq;
using Nuke.Common.Tools.Chocolatey;
using Pulumi;
using Log = Serilog.Log;
using Output = Pulumi.Output;

namespace Components;

[ParameterPrefix("Pulumi")]
interface IPulumiTargets : INukeBuild
{
    [Parameter("Access token")]
    string AccessToken => TryGetValue(() => AccessToken);

    [PathVariable("az")] static Tool Az;

    public static PulumiStack Stack = new();

    Target InstallPulumi => definition => definition.Executes(() => ChocolateyTasks.Chocolatey("upgrade pulumi -y"));

    Target ProvisionPulumi => definition => definition
        .DependsOn(InstallPulumi)
        .Executes(async () =>
        {
            if (!EnvironmentInfo.HasVariable("PULUMI_ACCESS_TOKEN") && !string.IsNullOrWhiteSpace(AccessToken))
            {
                EnvironmentInfo.SetVariable("PULUMI_ACCESS_TOKEN", AccessToken);
            }

            var program = Stack.Build();

            using var workspace = await LocalWorkspace.CreateOrSelectStackAsync(
                new InlineProgramArgs("Try.Dotnet.Affected", "WebApp", program));

            await workspace.UpAsync(new()
            {
                OnStandardOutput = Log.Information,
                OnStandardError = Log.Error
            });
        });
}

public class PulumiStack
{
    readonly List<Action> Actions = new();
    readonly Dictionary<string, object> Outputs = new ();


    public PulumiStack With(Action action)
    {
        Actions.Add(action);
        return this;
    }

    public PulumiStack With<T>(Func<T> action)
    {
        Actions.Add(() =>
        {
            var result = action();
            var outputs = typeof(T).GetProperties().Where(c => c.PropertyType.IsGenericType && c.PropertyType.GetGenericTypeDefinition()  == typeof(Output<>));
            foreach (var item in outputs)
            {
                Outputs.Add(item.Name, item.GetValue(result));
            }
        });
        return this;
    }

    public Output<T> GetOutput<T>(string name)
    {
        return Outputs[name] as Output<T>;
    }

    public PulumiFn Build() =>
        PulumiFn.Create(() => {
            Actions.ForEach(action => action());
        });
}