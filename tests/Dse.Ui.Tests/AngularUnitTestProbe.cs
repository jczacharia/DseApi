// Copyright (c) PNC Financial Services. All rights reserved.


using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Dse.Ui.Tests;

// PROBE: verifies the pipeline's test phase can run Angular (Vitest) unit tests via npm.
// The enterprise pipeline only invokes `dotnet test`, so Angular unit tests are driven from here.
public sealed class AngularUnitTestProbe
{
    [Fact]
    public async Task Angular_unit_tests_pass()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string repoRoot = FindRepoRoot();

        // Ensure deps exist (the .NET build/test phase doesn't necessarily run npm install first).
        if (!Directory.Exists(Path.Combine(repoRoot, "node_modules")))
        {
            Assert.Equal(expected: 0, await RunNpmAsync("ci", repoRoot, ct));
        }

        Assert.Equal(expected: 0, await RunNpmAsync("run test:ci", repoRoot, ct));
    }

    private static async Task<int> RunNpmAsync(string arguments, string workingDirectory, CancellationToken ct)
    {
        string npm = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "npm.cmd" : "npm";

        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = npm,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
        }) ?? throw new InvalidOperationException($"Failed to start 'npm {arguments}'.");

        await process.WaitForExitAsync(ct);
        return process.ExitCode;
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
