// Copyright (c) PNC Financial Services. All rights reserved.


namespace Dse.Ui.Tests;

// PROBE: drives the Angular app through the Aspire dev topology (api + ng serve) with Playwright.
public sealed class HomePageTests(AspireAppFixture app) : AspirePageTest(app)
{
    [Fact]
    public async Task Serves_the_angular_app_at_root()
    {
        await Page.GotoAsync("/");

        await Expect(Page.Locator("dse-root h1")).ToContainTextAsync("Hello, dse");
    }
}
