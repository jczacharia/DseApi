// Copyright (c) PNC Financial Services. All rights reserved.


using System.Reflection;
using Dse;
using Dse.Api;
using Dse.Confluence;
using Dse.Core;
using Dse.ES;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

Assembly[] dseAssemblies = [typeof(DseEnvironment).Assembly, typeof(Program).Assembly, typeof(ConfluenceDoc).Assembly];

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
    opts.AddComponentsFromAssemblies(dseAssemblies);
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

app.MapOpenApi();
app.MapScalarApiReference();

app.MapDseHealthChecks();

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("api").WithTags("Api").RequireAuthorization().MapCoreEndpoints();

foreach (WebAppExtender reg in app.Services.GetServices<WebAppExtender>())
{
    reg.Register(app);
}

if (DseEnvironment.ServesSpa)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");
}

await app.RunAsync();
