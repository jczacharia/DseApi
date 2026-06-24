// Copyright (c) PNC Financial Services. All rights reserved.


using Dse;
using Dse.Core;
using Dse.ES;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets("dse");
builder.Services.AddCoreOptions();
builder.Services.AddCoreValidators();

builder.Services.AddElastic();

builder.Services.AddProblemDetails(static s => s.ApplyCoreCustomization());
builder.Services.AddScoped<ProblemDetailsFactory, DefaultProblemDetailsFactory>();
builder.Services.ConfigureHttpClientDefaults(static o => o.RemoveAllLoggers());
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

builder.WebHost.UseKestrel(o => o.AddServerHeader = false); // Security best practice
builder.Host.UseDefaultServiceProvider(static options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(opts =>
{
    opts.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
    opts.AddDocumentTransformer(static (doc, _, _) =>
    {
        doc.Info.Title = "DSE";
        doc.Info.Description = "Enterprise Search";
        return Task.CompletedTask;
    });
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.Configure<ForwardedHeadersOptions>(static opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedProto
                            | ForwardedHeaders.XForwardedHost
                            | ForwardedHeaders.XForwardedPrefix;
    // OpenShift proxy IP is dynamic; cluster Route is the trust boundary.
    opts.KnownIPNetworks.Clear();
    opts.KnownProxies.Clear();
});

WebApplication app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsProduction())
{
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

RouteGroupBuilder healthChecks = app.MapGroup("health").WithTags("Health").AllowAnonymous();
healthChecks.MapHealthChecks("", new HealthCheckOptions().WithReportWriter());

healthChecks.MapHealthChecks("startup", new HealthCheckOptions
{
    Predicate = static r => r.Tags.Contains("startup"),
}.WithReportWriter());

healthChecks.MapHealthChecks("live", new HealthCheckOptions
{
    Predicate = static r => r.Tags.Contains("live"),
}.WithReportWriter());

healthChecks.MapHealthChecks("ready", new HealthCheckOptions
{
    Predicate = static r => r.Tags.Contains("ready"),
}.WithReportWriter());

healthChecks.MapHealthChecks("sources", new HealthCheckOptions
{
    Predicate = static r => r.Tags.Contains("source"),
}.WithReportWriter());

var healthOpts = app.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>();
foreach (string name in healthOpts.Value.Registrations.Select(r => r.Name))
{
    healthChecks.MapHealthChecks($"{name}", new HealthCheckOptions
    {
        Predicate = r => r.Name == name,
    }.WithReportWriter());
}

app.UseAuthentication();
app.UseAuthorization();

RouteGroupBuilder api = app.MapGroup("api").WithTags("Api").RequireAuthorization();
api.MapOpenApi().AllowAnonymous();
api.MapScalarApiReference().AllowAnonymous();
api.MapCoreEndpoints();

if (DseEnvironment.ServesSpa)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");
}

app.Run();
