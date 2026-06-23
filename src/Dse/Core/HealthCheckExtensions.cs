// Copyright (c) PNC Financial Services. All rights reserved.


using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dse.Core;

public static class HealthCheckDefaults
{
    // Shared readiness-probe budget. Evaluated at health-check registration time (before options bind), so it is a
    // constant rather than a per-environment config knob — a probe timeout is not an operational dial.
    public static readonly TimeSpan ReadinessTimeout = TimeSpan.FromSeconds(8);
}

/// <summary>Aggregated health of the service: overall status, total evaluation time, and a per-check breakdown.</summary>
/// <param name="Status">Overall status — <c>Healthy</c>, <c>Degraded</c>, or <c>Unhealthy</c>.</param>
/// <param name="TotalDuration">Wall-clock time taken to evaluate every check.</param>
/// <param name="Checks">One entry per registered health check.</param>
public sealed record DseHealthReport(
    string Status,
    string TotalDuration,
    IEnumerable<HealthReportEntry> Checks);

/// <summary>The result of a single registered health check.</summary>
/// <param name="Name">The check's registration name (e.g. <c>elastic</c>, <c>self</c>).</param>
/// <param name="Status">This check's status.</param>
/// <param name="Duration">How long this check took.</param>
/// <param name="Description">Human-readable detail the check chose to report, if any.</param>
/// <param name="Exception">The failure message when the check threw, if any.</param>
/// <param name="Data">Free-form diagnostic data the check attached.</param>
public sealed record HealthReportEntry(
    string Name,
    string Status,
    string Duration,
    string? Description,
    string? Exception,
    IReadOnlyDictionary<string, object> Data);

public static class HealthCheckExtensions
{
    private static async Task WriteHealthReport(HttpContext context, HealthReport report)
    {
        string result = JsonSerializer.Serialize(ToReport(report), JsonDefaults.Pretty);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(result);
    }

    private static DseHealthReport ToReport(HealthReport report) => new(
        report.Status.ToString(),
        report.TotalDuration.ToString(),
        report.Entries.Select(e => new HealthReportEntry(e.Key,
            e.Value.Status.ToString(),
            e.Value.Duration.ToString(),
            e.Value.Description,
            e.Value.Exception?.Message,
            e.Value.Data)));

    extension(HealthCheckOptions options)
    {
        public HealthCheckOptions WithReportWriter()
        {
            options.ResponseWriter = WriteHealthReport;
            return options;
        }
    }
}
