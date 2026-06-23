// Copyright (c) PNC Financial Services. All rights reserved.


namespace Dse.Tests.ES;

public sealed class ElasticHealthCheckTests(ITestOutputHelper outputHelper) : ApiTest(outputHelper)
{
    [Fact]
    public async Task ReadyEndpointWithLiveClusterReportsHealthy()
    {
        var response = await Client.GetAsync("/health/elastic/ready", TestContext.Current.CancellationToken);
        Assert.True(response.IsSuccessStatusCode);
    }
}
