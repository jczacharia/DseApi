// Copyright (c) PNC Financial Services. All rights reserved.


using Dse.Core;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Cluster;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthStatus = Elastic.Clients.Elasticsearch.HealthStatus;

namespace Dse.ES;

public sealed class ElasticHealthCheck(ElasticsearchClient elastic) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            HealthResponse cluster = await elastic.Cluster.HealthAsync(cancellationToken);
            Dictionary<string, object> data = cluster.ConvertToDictionary();

            if (cluster is { IsValidResponse: false })
            {
                return new HealthCheckResult(context.Registration.FailureStatus,
                    $"Cluster health call failed: {cluster.DebugInformation}", data: data);
            }

            return cluster.Status switch
            {
                HealthStatus.Green => HealthCheckResult.Healthy($"Cluster '{cluster.ClusterName}' is green.", data),
                HealthStatus.Yellow => HealthCheckResult.Degraded($"Cluster '{cluster.ClusterName}' is yellow.", data: data),
                _ => new HealthCheckResult(context.Registration.FailureStatus,
                    $"Cluster '{cluster.ClusterName}' status is {cluster.Status}", data: data),
            };
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
        }
    }
}
