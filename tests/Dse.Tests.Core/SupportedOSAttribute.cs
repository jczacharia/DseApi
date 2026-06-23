// Copyright (c) PNC Financial Services. All rights reserved.


using System.Reflection;
using System.Runtime.InteropServices;
using Xunit.v3;

namespace Dse.Tests;

/*
The intended usage of this sample attribute is as an extra attribute on a unit test method. For example:

    public class TestClass
    {
        [Fact]
        [SupportedOS(SupportedOS.Linux, SupportedOS.macOS)]
        public void TestMethod()
        {
        }
    }

TestMethod will only run when executed on Linux or macOS; it will not run on Windows or FreeBSD, and will be
dynamically skipped instead with a message about the current OS not being supported.
*/

public sealed class SupportedOsAttribute(params SupportedOs[] supportedOSes) :
    BeforeAfterTestAttribute
{
    private static readonly Dictionary<SupportedOs, OSPlatform> s_osMappings = new()
    {
        { SupportedOs.FreeBsd, OSPlatform.Create("FreeBSD") },
        { SupportedOs.Linux, OSPlatform.Linux },
        { SupportedOs.MacOs, OSPlatform.OSX },
        { SupportedOs.Windows, OSPlatform.Windows },
    };

    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        bool match = false;

        foreach (SupportedOs supportedOs in supportedOSes)
        {
            if (!s_osMappings.TryGetValue(supportedOs, out OSPlatform osPlatform))
            {
                throw new InvalidOperationException($"Supported OS value '{supportedOs}' is not a known OS");
            }

            if (RuntimeInformation.IsOSPlatform(osPlatform))
            {
                match = true;
                break;
            }
        }

        // We use the dynamic skip exception message pattern to turn this into a skipped test
        // when it's not running on one of the targeted OSes
        if (!match)
        {
            throw new InvalidOperationException(
                $"$XunitDynamicSkip$This test is not supported on {RuntimeInformation.OSDescription}");
        }
    }
}
