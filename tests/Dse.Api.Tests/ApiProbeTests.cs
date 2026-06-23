// Copyright (c) PNC Financial Services. All rights reserved.

using System.Net;

namespace Dse.Api.Tests;

// PROBE: verifies the pipeline can run in-process .NET integration tests (WebApplicationFactory).
// This is the test layer that feeds the Coverlet/opencover coverage gate (in-process => measurable).
public sealed class ApiProbeTests(ApiHost host) : IClassFixture<ApiHost>
{
    [Fact]
    public async Task Weatherforecast_returns_ok()
    {
        using HttpClient client = host.CreateClient();

        using HttpResponseMessage response =
            await client.GetAsync("/weatherforecast", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
