// Copyright (c) PNC Financial Services. All rights reserved.


using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace Dse;

public static class DseEnvironment
{
    public static readonly bool IsRelease = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyConfigurationAttribute>()
        ?.Configuration.Equals("Release", StringComparison.OrdinalIgnoreCase) ?? false;

    public static readonly bool IsDebug = !IsRelease;

    extension(IHostEnvironment env)
    {
        public bool IsTest() => env.IsEnvironment("Test");
        public bool IsLocalBuild() => IsDebug && (env.IsDevelopment() || env.IsTest());
        public bool IsProductionBuild() => IsRelease && (env.IsProduction() || env.IsTest());
    }

    public static bool IsDocumentGenerationBuild() =>
        Assembly.GetEntryAssembly()?.GetName().Name is "GetDocument.Insider";
}
