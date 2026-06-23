// Copyright (c) PNC Financial Services. All rights reserved.


using Medallion.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Dse.Tests.ES;

public sealed class EsDistributedLockTests(ITestOutputHelper outputHelper) : ApiTest(outputHelper)
{
    private IDistributedLockProvider Provider => Services.GetRequiredService<IDistributedLockProvider>();

    // Unique per test so the shared cluster never causes cross-test contention.
    private static string UniqueName() => $"test-lock-{Guid.NewGuid():N}";

    [Fact(Timeout = 5_000)]
    public async Task AcquiresAndReleases()
    {
        await using IDistributedSynchronizationHandle? handle =
            await Provider.TryAcquireLockAsync(UniqueName(), cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(handle);
    }

    [Fact(Timeout = 5_000)]
    public async Task SecondAcquisitionFailsWhileHeld()
    {
        string name = UniqueName();
        await using IDistributedSynchronizationHandle first =
            await Provider.AcquireLockAsync(name, cancellationToken: TestContext.Current.CancellationToken);

        IDistributedSynchronizationHandle? second =
            await Provider.TryAcquireLockAsync(name, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(second);
    }

    [Fact(Timeout = 5_000)]
    public async Task ReleasingAllowsReacquisition()
    {
        string name = UniqueName();
        await using (await Provider.AcquireLockAsync(name, cancellationToken: TestContext.Current.CancellationToken))
        {
            // hold then release
        }

        await using IDistributedSynchronizationHandle? again =
            await Provider.TryAcquireLockAsync(name, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(again);
    }

    [Fact(Timeout = 5_000)]
    public async Task DifferentNamesDoNotContend()
    {
        await using IDistributedSynchronizationHandle? a =
            await Provider.TryAcquireLockAsync(UniqueName(), cancellationToken: TestContext.Current.CancellationToken);
        await using IDistributedSynchronizationHandle? b =
            await Provider.TryAcquireLockAsync(UniqueName(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(a);
        Assert.NotNull(b);
    }

    [Fact(Timeout = 10_000)]
    public async Task WaiterAcquiresAfterHolderReleases()
    {
        string name = UniqueName();
        IDistributedSynchronizationHandle holder =
            await Provider.AcquireLockAsync(name, cancellationToken: TestContext.Current.CancellationToken);

        Task<IDistributedSynchronizationHandle> waiter = Provider.AcquireLockAsync(name, TimeSpan.FromSeconds(10),
                TestContext.Current.CancellationToken)
            .AsTask();
        Assert.False(waiter.IsCompleted); // blocked while the lease is live

        await holder.DisposeAsync();

        await using IDistributedSynchronizationHandle acquired = await waiter;
        Assert.NotNull(acquired);
    }

    [Fact(Timeout = 5_000)]
    public async Task HandleLostTokenIsLiveButUncancelledWhileHeld()
    {
        await using IDistributedSynchronizationHandle handle =
            await Provider.AcquireLockAsync(UniqueName(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(handle.HandleLostToken.CanBeCanceled);
        Assert.False(handle.HandleLostToken.IsCancellationRequested);
    }
}
