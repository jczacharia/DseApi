// Copyright (c) PNC Financial Services. All rights reserved.


using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ServiceScan.SourceGenerator;

namespace Dse.Core;

public static partial class Validation
{
    [GenerateServiceRegistrations(AssignableTo = typeof(IValidator<>), Lifetime = ServiceLifetime.Scoped)]
    public static partial IServiceCollection AddCoreValidators(this IServiceCollection services);
}
