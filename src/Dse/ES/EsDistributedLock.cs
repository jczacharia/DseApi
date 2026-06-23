// Copyright (c) PNC Financial Services. All rights reserved.


using System.Security.Cryptography;
using System.Text;
using Elastic.Clients.Elasticsearch;
using Medallion.Threading;
using Microsoft.Extensions.Logging;

namespace Dse.ES;

/// <summary>The lock document — one per lock name, id <c>lock:{name}</c>. Lease-bound so a dead holder self-heals.</summary>
public sealed record EsLockDocument(
    string Name,
    Guid Holder,
    string Owner,
    DateTimeOffset AcquiredAt,
    DateTimeOffset LeaseUntil);

/// <summary>
///     A <see cref="IDistributedLock" /> backed by an Elasticsearch create-only document. Acquire is a real-time,
///     strongly-consistent <c>op_type=create</c> on the document's primary shard (NOT a near-real-time search), so it is a
///     sound cross-pod mutex. A <see cref="EsLockDocument.LeaseUntil" /> field lets a crashed holder's lock expire and be
///     stolen; the live holder renews it from a background heartbeat (see <see cref="EsDistributedSynchronizationHandle" />)
///     for the duration of a long critical section.
/// </summary>
public sealed class EsDistributedLock : IDistributedLock
{
    public const string IndexName = "dse-locks";

    /// <summary>Generous enough that a stalled-but-alive holder is not stolen mid-flight; the heartbeat renews well inside it.</summary>
    public static readonly TimeSpan DefaultLeaseDuration = TimeSpan.FromMinutes(2);

    private static readonly TimeSpan s_pollInterval = TimeSpan.FromMilliseconds(250);

    private readonly ElasticsearchClient _client;
    private readonly ILogger<EsDistributedLock> _logger;
    private readonly TimeSpan _leaseDuration;
    private readonly string _owner = Environment.MachineName;
    private readonly string _id;

    public EsDistributedLock(string name, ElasticsearchClient client, ILogger<EsDistributedLock> logger)
        : this(name, client, logger, DefaultLeaseDuration)
    {
    }

    internal EsDistributedLock(string name, ElasticsearchClient client, ILogger<EsDistributedLock> logger, TimeSpan leaseDuration)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _leaseDuration = leaseDuration;
        _id = ToId(name);
    }

    public string Name { get; }

    public IDistributedSynchronizationHandle? TryAcquire(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        TryAcquireAsync(timeout, cancellationToken).AsTask().GetAwaiter().GetResult();

    public IDistributedSynchronizationHandle Acquire(TimeSpan? timeout = null, CancellationToken cancellationToken = default) =>
        AcquireAsync(timeout, cancellationToken).AsTask().GetAwaiter().GetResult();

    public ValueTask<IDistributedSynchronizationHandle?> TryAcquireAsync(TimeSpan timeout = default, CancellationToken cancellationToken = default) =>
        AcquireLoopAsync(ValidateTimeout(timeout), cancellationToken);

    public async ValueTask<IDistributedSynchronizationHandle> AcquireAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        IDistributedSynchronizationHandle? handle =
            await AcquireLoopAsync(ValidateTimeout(timeout ?? Timeout.InfiniteTimeSpan), cancellationToken).ConfigureAwait(false);
        return handle ?? throw new TimeoutException($"Timeout exceeded when trying to acquire the lock '{Name}'.");
    }

    private async ValueTask<IDistributedSynchronizationHandle?> AcquireLoopAsync(TimeSpan timeout, CancellationToken ct)
    {
        bool infinite = timeout == Timeout.InfiniteTimeSpan;
        long deadline = Environment.TickCount64 + (long)timeout.TotalMilliseconds;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            if (await TryAcquireOnceAsync(ct).ConfigureAwait(false) is { } handle)
            {
                return handle;
            }

            if (infinite)
            {
                await Task.Delay(s_pollInterval, ct).ConfigureAwait(false);
                continue;
            }

            long remaining = deadline - Environment.TickCount64;
            if (remaining <= 0)
            {
                return null;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(s_pollInterval.TotalMilliseconds, remaining)), ct).ConfigureAwait(false);
        }
    }

    private async ValueTask<IDistributedSynchronizationHandle?> TryAcquireOnceAsync(CancellationToken ct)
    {
        var holder = Guid.NewGuid();
        if (await CreateAsync(holder, ct).ConfigureAwait(false))
        {
            return StartHandle(holder);
        }

        // Conflict: a lock exists. Steal it only if its lease has expired (the prior holder is presumed dead).
        GetResponse<EsLockDocument> existing = await _client.GetAsync<EsLockDocument>(IndexName, _id, ct).ConfigureAwait(false);
        if (existing is { Found: true, Source: { } held } && held.LeaseUntil > DateTimeOffset.UtcNow)
        {
            return null;
        }

        if (existing.Found)
        {
            _logger.LogWarning("Stealing expired lock '{Name}' (held by {Holder})", Name, existing.Source?.Holder);
            await _client.DeleteAsync(IndexName, _id,
                (DeleteRequestDescriptor d) => d.IfSeqNo(existing.SeqNo).IfPrimaryTerm(existing.PrimaryTerm), ct).ConfigureAwait(false);
        }

        // One retry after a steal; if we lose the race for the freed slot, the other acquirer legitimately won.
        return await CreateAsync(holder, ct).ConfigureAwait(false) ? StartHandle(holder) : null;
    }

    private async ValueTask<bool> CreateAsync(Guid holder, CancellationToken ct)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var doc = new EsLockDocument(Name, holder, _owner, now, now + _leaseDuration);
        IndexResponse response = await _client.IndexAsync(doc, i => i
            .Index(IndexName)
            .Id(_id)
            .OpType(OpType.Create)
            .Refresh(Refresh.True), ct).ConfigureAwait(false);

        // 409 version_conflict ⇒ a document already exists ⇒ slot is taken.
        return response is { IsValidResponse: true, Result: Result.Created };
    }

    private EsDistributedSynchronizationHandle StartHandle(Guid holder) =>
        new(_client, _id, Name, holder, _leaseDuration, _logger);

    private static TimeSpan ValidateTimeout(TimeSpan timeout)
    {
        if (timeout != Timeout.InfiniteTimeSpan && (timeout < TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue))
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be non-negative or infinite.");
        }

        return timeout;
    }

    // ES _id is capped at 512 bytes; hash names that would overflow so any caller-supplied name is safe.
    private static string ToId(string name)
    {
        string raw = $"lock:{name}";
        if (Encoding.UTF8.GetByteCount(raw) <= 512)
        {
            return raw;
        }

        return "lock:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(name)));
    }
}

/// <summary>
///     A held Elasticsearch lock. Renews its lease from a background heartbeat at a third of the lease duration so a single
///     missed beat never lets the lock expire under the holder, signals <see cref="HandleLostToken" /> if the lease is lost
///     to a steal, and releases the slot on dispose. Only the current holder (matching <see cref="EsLockDocument.Holder" />)
///     may renew or release.
/// </summary>
internal sealed class EsDistributedSynchronizationHandle : IDistributedSynchronizationHandle
{
    private readonly ElasticsearchClient _client;
    private readonly string _id;
    private readonly string _name;
    private readonly Guid _holder;
    private readonly TimeSpan _leaseDuration;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _lost = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _heartbeat;
    private int _disposed;

    public EsDistributedSynchronizationHandle(
        ElasticsearchClient client, string id, string name, Guid holder, TimeSpan leaseDuration, ILogger logger)
    {
        _client = client;
        _id = id;
        _name = name;
        _holder = holder;
        _leaseDuration = leaseDuration;
        _logger = logger;
        _heartbeat = HeartbeatAsync(_stop.Token);
    }

    public CancellationToken HandleLostToken => _lost.Token;

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        await _stop.CancelAsync().ConfigureAwait(false);
        try
        {
            await _heartbeat.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected on stop.
        }

        await ReleaseAsync().ConfigureAwait(false);

        _stop.Dispose();
        _lost.Dispose();
    }

    private async Task HeartbeatAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_leaseDuration / 3);
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                if (!await TryRenewAsync(ct).ConfigureAwait(false))
                {
                    _logger.LogWarning("Lease for lock '{Name}' was lost (held by {Holder})", _name, _holder);
                    await _lost.CancelAsync().ConfigureAwait(false);
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on dispose.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lease heartbeat for lock '{Name}' failed; lock may expire if the work outlives its lease", _name);
        }
    }

    private async Task<bool> TryRenewAsync(CancellationToken ct)
    {
        GetResponse<EsLockDocument> existing = await _client.GetAsync<EsLockDocument>(EsDistributedLock.IndexName, _id, ct).ConfigureAwait(false);
        if (existing is not { Found: true, Source: { } held } || held.Holder != _holder)
        {
            return false;
        }

        EsLockDocument renewed = held with { LeaseUntil = DateTimeOffset.UtcNow + _leaseDuration };
        // CAS: only renew if nobody has touched the document since we read it.
        IndexResponse response = await _client.IndexAsync(renewed, i => i
            .Index(EsDistributedLock.IndexName)
            .Id(_id)
            .IfSeqNo(existing.SeqNo)
            .IfPrimaryTerm(existing.PrimaryTerm), ct).ConfigureAwait(false);

        return response is { IsValidResponse: true, Result: Result.Updated };
    }

    private async Task ReleaseAsync()
    {
        // Best-effort cleanup, independent of the caller's token; an expired lock self-heals anyway.
        try
        {
            GetResponse<EsLockDocument> existing =
                await _client.GetAsync<EsLockDocument>(EsDistributedLock.IndexName, _id, CancellationToken.None).ConfigureAwait(false);
            if (existing is { Found: true, Source.Holder: var holder } && holder == _holder)
            {
                await _client.DeleteAsync(EsDistributedLock.IndexName, _id,
                    (DeleteRequestDescriptor d) => d.IfSeqNo(existing.SeqNo).IfPrimaryTerm(existing.PrimaryTerm), CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to release lock '{Name}'; it will expire with its lease", _name);
        }
    }
}

/// <summary>DI-friendly factory for <see cref="EsDistributedLock" /> instances.</summary>
public sealed class EsDistributedLockProvider(ElasticsearchClient client, ILoggerFactory loggerFactory) : IDistributedLockProvider
{
    public IDistributedLock CreateLock(string name) =>
        new EsDistributedLock(name, client, loggerFactory.CreateLogger<EsDistributedLock>());
}
