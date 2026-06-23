// Copyright (c) PNC Financial Services. All rights reserved.


using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ProjectResource> api = builder.AddProject<Dse_Api>("api");

// Angular dev server (native node process — no container). Aspire injects the API's URL as the
// `services__api__*` env vars, which proxy.conf.cjs reads to forward /api calls to the backend.
builder.AddJavaScriptApp("ui", "../../", "start")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(targetPort: 4200, port: 4200, env: "PORT", isProxied: false)
    // Health = the dev server actually answered (not just "process running"), so tests that
    // WaitForResourceHealthyAsync("ui") don't race ng serve's first compile.
    .WithHttpHealthCheck("/")
    .WithExternalHttpEndpoints();

builder.Build().Run();
