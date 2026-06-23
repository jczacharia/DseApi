// Copyright (c) PNC Financial Services. All rights reserved.


using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Logging;

[assembly: CaptureConsole(CaptureError = true, CaptureOut = true)]

namespace Dse.Tests;

public sealed class ApiHost(ITestOutputHelper outputHelper) : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string s_webRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "dist", "dse", "browser"));

    public string BaseAddress => ClientOptions.BaseAddress.ToString().TrimEnd('/');

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        UseKestrel(0);
        builder.UseEnvironment("Test");
        builder.UseWebRoot(s_webRoot);
        builder.ConfigureAppConfiguration(sources =>
        {
            for (int i = sources.Sources.Count - 1; i >= 0; i--)
            {
                if (sources.Sources[i] is EnvironmentVariablesConfigurationSource)
                {
                    sources.Sources.RemoveAt(i);
                }
            }
        });
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddXUnit(outputHelper);
        });
    }

    public ValueTask InitializeAsync()
    {
        StartServer();
        return ValueTask.CompletedTask;
    }
}
