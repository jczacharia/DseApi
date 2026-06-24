// Copyright (c) PNC Financial Services. All rights reserved.


using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceScan.SourceGenerator;

namespace Dse.Core;

[AttributeUsage(AttributeTargets.Class)]
public class OptionAttribute(string? section = null, string? name = null) : Attribute
{
    public string? Section { get; } = section;
    public string? Name { get; } = name;
}

public static partial class ServiceCollectionExtensions
{
    [ScanForTypes(AttributeFilter = typeof(OptionAttribute), Handler = nameof(AddOption))]
    public static partial IServiceCollection AddCoreOptions(this IServiceCollection services);

    private static void AddOption<TOptions>(IServiceCollection services) where TOptions : class
    {
        var attr = typeof(TOptions).GetCustomAttribute<OptionAttribute>();
        var builder = services.AddOptions<TOptions>(attr?.Name);
        builder.Services.AddSingleton<IValidateOptions<TOptions>>(s => new FluentValidateOptions<TOptions>(s, builder.Name));

        if (attr?.Section is { } section)
        {
            builder.BindConfiguration(section);
        }

        if (!DseEnvironment.IsDocumentGenerationBuild)
        {
            builder.ValidateDataAnnotations();
            builder.ValidateOnStart();
        }
    }

    private sealed class FluentValidateOptions<T>(IServiceProvider sp, string? optionsName) : IValidateOptions<T> where T : class
    {
        public ValidateOptionsResult Validate(string? name, T options)
        {
            if (optionsName is not null && optionsName != name)
            {
                return ValidateOptionsResult.Skip;
            }

            ArgumentNullException.ThrowIfNull(options);

            using IServiceScope scope = sp.CreateScope();
            string type = options.GetType().Name;

            if (scope.ServiceProvider.GetServices<IValidator<T>>()
                    .SelectMany(validator => validator.Validate(options) is { IsValid: false } result
                        ? result.Errors.Select(failure =>
                            $"Validation failed for {type}.{failure.PropertyName} with the error: {failure.ErrorMessage}")
                        : [])
                    .ToArray() is { Length: > 0 } errors)
            {
                return ValidateOptionsResult.Fail(errors);
            }

            return ValidateOptionsResult.Success;
        }
    }
}
