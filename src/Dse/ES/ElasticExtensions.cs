// Copyright (c) PNC Financial Services. All rights reserved.


using Dse.Core;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Dse.ES;

public static class ElasticExtensions
{
    extension(IServiceCollection services)
    {
        public void AddElastic()
        {
            services.AddSingleton<ElasticsearchClient>(static sp =>
            {
                var opts = sp.GetRequiredService<IOptions<ElasticOptions>>().Value;
                var es = new ElasticsearchClientSettings(new SingleNodePool(new Uri(opts.BaseAddress)));

                if (!string.IsNullOrEmpty(opts.ApiKey))
                {
                    es.Authentication(new ApiKey(opts.ApiKey));
                }
                else if (!string.IsNullOrEmpty(opts.Username))
                {
                    es.Authentication(new BasicAuthentication(opts.Username, opts.Password ?? string.Empty));
                }

                if (Environment.GetEnvironmentVariable("HTTPS_PROXY") is { } proxy)
                {
                    es.Proxy(new Uri(proxy));
                }

                return new ElasticsearchClient(new DistributedTransport<IElasticsearchClientSettings>(es));
            });

            services.AddSingleton<ITransport>(static sp => sp.GetRequiredService<ElasticsearchClient>().Transport);

            services.AddSingleton<IDistributedLockProvider, EsDistributedLockProvider>();

            services
                .AddHealthChecks()
                .AddCheck<ElasticHealthCheck>("elastic", HealthStatus.Unhealthy, ["ready"], HealthCheckCore.ReadinessTimeout);
        }
    }
}
