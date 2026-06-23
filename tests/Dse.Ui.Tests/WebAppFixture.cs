// Copyright (c) PNC Financial Services. All rights reserved.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Dse.Ui.Tests;

/// <summary>
/// Hosts the app for Playwright E2E tests and ensures the browser is installed. Shared across the
/// test assembly.
/// <para>
/// Local dev (fast): set <c>DSE_E2E_BASE_URL</c> to an already-running instance (e.g. the SpaProxy
/// dev server at https://localhost:4200) and the fixture drives that, skipping the publish step.
/// </para>
/// <para>
/// CI (self-contained): with no override, the fixture publishes <c>Dse.Api</c> — which runs the
/// PublishAngular target (npm ci + npm run build into wwwroot) — and hosts it in Production on a
/// real port, so SpaProxy stays inactive and Playwright drives the production static artifact.
/// </para>
/// </summary>
public sealed class WebAppFixture : IAsyncLifetime
{
    private static readonly string RepoRoot = FindRepoRoot();
    private readonly string _publishDir = Path.Combine(Path.GetTempPath(), "dse-e2e", Guid.NewGuid().ToString("N"));
    private Process? _api;

    public string BaseUrl { get; } =
        Environment.GetEnvironmentVariable("DSE_E2E_BASE_URL") is { Length: > 0 } url ? url : "http://127.0.0.1:5199";

    public async ValueTask InitializeAsync()
    {
        InstallBrowsers();

        // An external instance was supplied (local dev): just wait until it answers.
        if (Environment.GetEnvironmentVariable("DSE_E2E_BASE_URL") is { Length: > 0 })
        {
            await WaitForReadyAsync(BaseUrl, TimeSpan.FromMinutes(1));
            return;
        }

        string apiProject = Path.Combine(RepoRoot, "src", "Dse.Api", "Dse.Api.csproj");

        // Publish runs the PublishAngular MSBuild target: npm ci + npm run build -> wwwroot.
        await ExecAsync("dotnet", $"publish \"{apiProject}\" -c Release -o \"{_publishDir}\"", RepoRoot);

        _api = StartProcess(
            "dotnet",
            $"\"{Path.Combine(_publishDir, "Dse.Api.dll")}\"",
            _publishDir,
            new Dictionary<string, string>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["ASPNETCORE_URLS"] = BaseUrl,
            });

        await WaitForReadyAsync(BaseUrl, TimeSpan.FromMinutes(2));
    }

    public async ValueTask DisposeAsync()
    {
        if (_api is { HasExited: false })
        {
            _api.Kill(entireProcessTree: true);
            await _api.WaitForExitAsync();
        }

        _api?.Dispose();

        try
        {
            Directory.Delete(_publishDir, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
            // Nothing to clean up.
        }
    }

    // Downloads the Chromium binary Playwright drives. Skippable locally once the cache is warm.
    // NOTE (CI): the builder image must already provide Chromium's OS-level libraries, and reach
    // Playwright's browser CDN (or a mirror via PLAYWRIGHT_DOWNLOAD_HOST). These are the two things
    // to confirm on the first pipeline run.
    private static void InstallBrowsers()
    {
        if (Environment.GetEnvironmentVariable("DSE_E2E_SKIP_BROWSER_INSTALL") == "1")
        {
            return;
        }

        int exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright browser install failed with exit code {exitCode}.");
        }
    }

    private static async Task WaitForReadyAsync(string baseUrl, TimeSpan timeout)
    {
        using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(5) };
        DateTimeOffset deadline = DateTimeOffset.UtcNow + timeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using HttpResponseMessage response = await client.GetAsync(baseUrl);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Server not up yet.
            }
            catch (TaskCanceledException)
            {
                // Request timed out; retry until the deadline.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException($"The web app did not become ready at {baseUrl} within {timeout}.");
    }

    private static async Task ExecAsync(string fileName, string arguments, string workingDirectory)
    {
        using Process process = StartProcess(fileName, arguments, workingDirectory, environment: null);
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"'{fileName} {arguments}' exited with code {process.ExitCode}.");
        }
    }

    private static Process StartProcess(
        string fileName,
        string arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = ResolveExecutable(fileName),
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
        };

        if (environment is not null)
        {
            foreach (KeyValuePair<string, string> variable in environment)
            {
                startInfo.Environment[variable.Key] = variable.Value;
            }
        }

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start '{fileName}'.");
    }

    private static string ResolveExecutable(string fileName)
    {
        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        return isWindows && fileName == "npm" ? "npm.cmd" : fileName;
    }

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Dse.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root (Dse.slnx).");
    }
}
