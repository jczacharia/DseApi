// Copyright (c) PNC Financial Services. All rights reserved.


using Dse.Api;
using Dse.ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// OpenTelemetry, health checks, resilience, service discovery (Aspire ServiceDefaults).
builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Serve the built Angular SPA from wwwroot (populated on publish). In dev, Dse.AppHost (Aspire)
// runs `ng serve` separately and the dev server proxies API calls here.
app.UseDefaultFiles();
app.UseStaticFiles();

// /health and /alive (from ServiceDefaults).
app.MapDefaultEndpoints();

string[] summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching",
};

app.MapGet("/weatherforecast", () =>
    {
        WeatherForecast[] forecast = Enumerable.Range(start: 1, count: 5)
            .Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(minValue: -20, maxValue: 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

// SPA client-side routing fallback: unmatched non-API requests return index.html.
app.MapFallbackToFile("index.html");

app.Run();

namespace Dse.Api
{
    internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
