// Copyright (c) PNC Financial Services. All rights reserved.


using Microsoft.AspNetCore.Mvc.Testing;

namespace Dse.Api.Tests;

// Public so xUnit's public test classes can consume it as an IClassFixture.
public sealed class ApiHost : WebApplicationFactory<Program> { }
