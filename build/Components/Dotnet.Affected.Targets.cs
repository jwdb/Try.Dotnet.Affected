using System;
using System.IO;
using System.Text.Json;
using Nuke.Common;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;

namespace Components;
interface IDotnetAffectedTargets : INukeBuild
{
	AzurePipelines AzurePipelines => AzurePipelines.Instance;
	GitHubActions GitHubActions => GitHubActions.Instance;

	// ReSharper disable once ClassNeverInstantiated.Local
	record AffectedJson(string Name, string FilePath);

	public Target DotnetAffected => definition => definition
		.Executes(() =>
		{
			var args = new Arguments()
				.Add("tool")
				.Add("run")
				.Add("dotnet-affected")
				.Add("--format json")
				.Add("--verbose", condition: Verbosity >= Verbosity.Normal);

			if (AzurePipelines != null)
			{
				args.Add("--from {value}", AzurePipelines.PullRequestSourceBranch)
					.Add("--to {value}", AzurePipelines.PullRequestTargetBranch);
			}

			if (GitHubActions != null)
			{
				if (GitHubActions.IsPullRequest)
				{
					args.Add("--from {value}", GitHubActions.HeadRef)
						.Add("--to {value}", GitHubActions.BaseRef);
				}
				else
				{
					args.Add("--from {value}", GitHubActions.GitHubEvent["before"]?.ToString());
				}
			}

			DotNetTasks.DotNetToolRestore();
			DotNetTasks.DotNet(args.ToString(), exitHandler: DotnetToolExitHandler);

			if (!File.Exists("affected.json"))
			{
				return;
			}

			var toBuildJson = File.ReadAllText("affected.json");
			var affectedProjects = JsonSerializer.Deserialize<AffectedJson[]>(toBuildJson);
			foreach (var project in affectedProjects)
			{
				Build.ProjectsToBuild.Add(project.Name, project.FilePath);
			}
		});


	void DotnetToolExitHandler(IProcess obj)
	{
		switch (obj.ExitCode)
		{
			case 0: // Success
			case 166: // No changed projects
				return;
			default:
				throw new Exception();
		}
	}

}
