using System.Collections.Generic;
using ThreeDEngine.Core.Models;
using ThreeDEngine.Core.Services;

namespace BlazorApp.Client.Services;

public sealed class WorkspaceOverviewService
{
    public Scene GetScene() => SampleSceneFactory.CreateDefault();

    public IReadOnlyList<ProjectSummary> GetProjects() =>
    new List<ProjectSummary>
    {
        new(
            "3DEngine.Core",
            "Shared engine/domain layer used by every host.",
            new[] { "System.* and platform-neutral packages" },
            new[] { "3DEngine", "BlazorApp", "BlazorApp.Client" },
            new[] { "Contracts", "Scene models", "Resource descriptors", "Shared services" },
            new[] { "SDL/Vulkan code", "Blazor components", "ASP.NET Core startup" }),
        new(
            "3DEngine",
            "Native desktop runtime and renderer host.",
            new[] { "3DEngine.Core", "Vortice.Vulkan.SampleFramework", "Vulkan/SDL packages" },
            new[] { "Direct user launch" },
            new[] { "Entry point", "Native window loop", "Renderer implementation" },
            new[] { "Shared scene contracts", "Blazor pages" }),
        new(
            "BlazorApp",
            "ASP.NET Core server host for the web/editor experience.",
            new[] { "BlazorApp.Client", "ASP.NET Core packages" },
            new[] { "Direct user launch" },
            new[] { "Host startup", "Routing", "Prerender/interactive bootstrapping" },
            new[] { "Native rendering code", "Engine domain ownership" }),
        new(
            "BlazorApp.Client",
            "Interactive editor-oriented UI client.",
            new[] { "3DEngine.Core", "Blazor packages" },
            new[] { "BlazorApp" },
            new[] { "Pages", "Navigation", "Editor dashboard", "Shared-model presentation" },
            new[] { "SDL/Vulkan code", "Server startup code" })
    };

    public IReadOnlyList<LaunchTarget> GetLaunchTargets() =>
    new List<LaunchTarget>
    {
        new(
            "Desktop runtime",
            @".\3DEngine\3DEngine.csproj",
            "Runs the SDL/Vulkan native host."),
        new(
            "Web editor",
            @".\BlazorApp\BlazorApp\BlazorApp.csproj",
            "Runs the ASP.NET Core + Blazor editor host.")
    };
}

public sealed record ProjectSummary(
    string Name,
    string Purpose,
    IReadOnlyList<string> AllowedDependencies,
    IReadOnlyList<string> Consumers,
    IReadOnlyList<string> Includes,
    IReadOnlyList<string> Excludes);

public sealed record LaunchTarget(string Name, string ProjectPath, string Description);
