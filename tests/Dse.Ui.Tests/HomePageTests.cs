// Copyright (c) PNC Financial Services. All rights reserved.

using Microsoft.Playwright.Xunit.v3;

[assembly: AssemblyFixture(typeof(Dse.Ui.Tests.WebAppFixture))]

namespace Dse.Ui.Tests;

public sealed class HomePageTests(WebAppFixture app) : PageTest
{
    [Fact]
    public async Task Serves_the_angular_app_at_root()
    {
        await Page.GotoAsync(app.BaseUrl);

        await Expect(Page).ToHaveTitleAsync("Dse");
        await Expect(Page.Locator("dse-root h1")).ToContainTextAsync("Hello, dse");
    }
}
