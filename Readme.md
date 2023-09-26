# Dotnet Affected 🔍 &amp; Nuke ☢️


This project shows an example on how to integrate [Dotnet Affected](https://github.com/leonardochaia/dotnet-affected) with [Nuke Build](https://nuke.build/).

<img src="https://img.shields.io/github/actions/workflow/status/jwdb/Try.Dotnet.Affected/continuous.yml" alt="shields">

🪟Project Screenshots:
--
<img src="https://yuki.zip/s/j0ht48azcf.png" alt="project-screenshot" />

💡Features
--

*   Use dotnet-affected to trigger specific build tasks
*   Seperate build files for different projects
*   Pulumi deployment to Azure using Pulumi Automation

🛠️ Installation Steps:
--
1. Clone repository
```bash
git clone https://github.com/jwdb/Try.Dotnet.Affected/
```
2. Install Nuke
```bash
dotnet tool install Nuke.GlobalTool --global
```
3. Nuke ☢️ it from orbit 🚀

```bash
nuke
```

Project Structure
--
The project consists of multiple example projects and a build project.
Example projects:

* `/Try.Dotnet.Affected/` This is a plain console app with a dependency to `/Try.Dotnet.Affected.Dependency/`
* `/Try.Dotnet.Affected.Sidestory/` Another plain console app with no dependencies.
* `/Try.Dotnet.Affected.Tests/` This has a reference to `/Try.Dotnet.Affected/` and serves as a test-project for it.
* `/Try.Dotnet.Affected.StoryApi/` A simple WebApi with no external dependencies
* `/Try.Dotnet.Affected.ProxyStoryApi/` This project has a implicit dependenc on the StoryApi through a app setting.

All these projects have their own Companion `*.build.cs` under `/build/Components/`. These files describe how the project should be *Compiled*, *Tested*, *Published* and *Deployed*.

The description on how this is done is contained into individual targets.

---
### Compile
Every `*.Build.cs` file has a **Compile** target that defines on how the project is built. This target has a dependency on The **DotnetAffected** Target.

This target is *TriggeredBy the **CompileSolution** Target* but only ran if *the project is included in the **ProjectsToBuild** array* .

---
### Test
When a project has Tests, a **Test** target can be added. Yet again this has a dependency on the **DotnetAffected** Target but also on the **Compile** Target.

This target is *TriggeredBy the **RunTestsSolution** Target* but only ran if *the project is included in the **ProjectsToBuild** array*.

---
### Publish
If a project can be Published. a **Publish** target can be added. In the same way as the previous targt, this has a dependency on **DotnetAffected** and the **Compile** Target.

This target is *TriggeredBy the **RunTestsSolution** Target* but only ran if *the project is included in the **ProjectsToBuild** array*.

---

### Deploy
The most complicated step. The step is divided into two parts: **DeployStack** and **PublishStack**. 

#### **DeployStack**
This target is to setup the infrastructure using *Pulumi*.
This function makes it possible to add extra items to the Pulumi stack:
```csharp
 IPulumiTargets.Stack.With(() => { })
 ```
 This function either has a `Action` as parameter or a `Func<{[name] = Output<T>}>` where each property of the anonymous type of the type `Output<T>` can be used in other Deploy targets using: `IPulumiTargets.Stack.GetOutput<string>("ResourceGroupName")`.

This Target has a *DependentFor* constraint on **IPulumiTargets.ProvisionPulumi**. Using this constraint this Target will be ran before Provisioning has happened.

Using a combination of *After* and *Before* this Target can be placed on a certain place in the pulumi stack.

---
⚠️ Warning: This target has no dependency on **DotnetAffected** because otherwise the resource would be removed by Pulumi when it has not changed.

---
#### **PublishStack**
With this target the *published* project is *deployed* to the provisioned *stack*.
This target yet again dependent on **DotnetAffected** but also on **IPulumiTargets.ProvisionPulumi**.

This target is *TriggeredBy the **PublishSoltuin** Target* but only ran if *the project is included in the **ProjectsToBuild** array*.