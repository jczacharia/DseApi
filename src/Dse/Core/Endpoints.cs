// Copyright (c) PNC Financial Services. All rights reserved.


using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ServiceScan.SourceGenerator;

namespace Dse.Core;

public interface IEndpoint
{
    static abstract void MapEndpoint(IEndpointRouteBuilder endpoints);
}

public class HelloWorldEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", () => "Hello World!").WithDescription("DERP");
    }
}

public static partial class ServiceCollectionExtensions
{
    [ScanForTypes(AssignableTo = typeof(IEndpoint), Handler = nameof(IEndpoint.MapEndpoint))]
    public static partial IEndpointRouteBuilder MapCoreEndpoints(this IEndpointRouteBuilder endpoints);
}
