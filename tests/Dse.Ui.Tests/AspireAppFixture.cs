// Copyright (c) PNC Financial Services. All rights reserved.


using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Dse.Ui.Tests;
using Projects;

// One Aspire app graph (api + Angular ui) started once for the whole assembly; Playwright's
// PageTest then manages a browser per test. Aspire tests are E2E/behavioral and excluded from
// the coverage gate (the API runs as a child process — coverlet can't see it).
[assembly: CaptureConsole(CaptureError = true, CaptureOut = true)]
[assembly: CollectionBehavior(DisableTestParallelization = true)]
[assembly: AssemblyFixture(typeof(AspireAppFixture))]

namespace Dse.Ui.Tests;

public sealed class AspireAppFixture : IAsyncLifetime
{
    private static readonly TimeSpan s_startupTimeout = TimeSpan.FromMinutes(5);
    private DistributedApplication? _app;

    public Uri UiBaseUrl { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        EnsureBrowsersInstalled();

        // AppHost uses http endpoints; allow them without dev HTTPS (tests have no launch profile).
        Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");

        using CancellationTokenSource cts = new(s_startupTimeout);
        CancellationToken ct = cts.Token;

        IDistributedApplicationTestingBuilder builder =
            await DistributedApplicationTestingBuilder.CreateAsync<Dse_AppHost>(ct);

        // For backing services reached by connection string (e.g. Elasticsearch), inject them here
        // before BuildAsync so StartAsync never needs a container, e.g.:
        //   builder.Configuration["ConnectionStrings:elastic"] =
        //       Environment.GetEnvironmentVariable("DSE_TEST_ELASTIC") ?? "http://localhost:9200";

        _app = await builder.BuildAsync(ct).WaitAsync(s_startupTimeout, ct);
        await _app.StartAsync(ct).WaitAsync(s_startupTimeout, ct);

        // "Healthy" here means ng serve answered (the ui resource has WithHttpHealthCheck("/")).
        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("ui", ct)
            .WaitAsync(s_startupTimeout, ct);

        UiBaseUrl = _app.GetEndpoint("ui", "http");
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    // Downloads the Chromium binary. Skip via DSE_E2E_SKIP_BROWSER_INSTALL=1 when already cached.
    // CI note: OS libs must be pre-baked in the RHEL image (no --with-deps); point at a mirror with
    // PLAYWRIGHT_DOWNLOAD_HOST, or pre-stage browsers and set PLAYWRIGHT_BROWSERS_PATH.
    private static void EnsureBrowsersInstalled()
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
}
