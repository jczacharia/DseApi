// Copyright (c) PNC Financial Services. All rights reserved.


using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace Dse.Ui.Tests;

// PageTest owns a fresh browser/page per test; we only feed it the Aspire-resolved Angular URL
// (known only after StartAsync) via ContextOptions so tests can navigate with relative routes.
public abstract class AspirePageTest(AspireAppFixture app) : PageTest
{
    protected AspireAppFixture App { get; } = app;

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = App.UiBaseUrl.ToString(),
        IgnoreHTTPSErrors = true,
    };
}
