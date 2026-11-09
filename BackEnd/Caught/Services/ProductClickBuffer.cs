namespace Caught.Services;

public readonly record struct ProductClickCount(int ProductId, string Channel, DateTime BucketStart, int Count);

public sealed class ProductClickBuffer : IDisposable
{
    public const int PendingCap = 100_000;

    private readonly Lock gate = new();
    private readonly Dictionary<BucketKey, int> counts = [];
    private readonly SemaphoreSlim pulse = new(0, 1);
    private int pending;

    public void Record(int productId, string channel)
    {
        if (channel is not ("tiktok" or "amazon" or "brand" or "product" or "notification"))
        {
            throw new ArgumentOutOfRangeException(nameof(channel));
        }

        var key = new BucketKey(productId, channel, Align(DateTime.UtcNow));
        var flushNow = false;
        lock (gate)
        {
            counts.TryGetValue(key, out var count);
            counts[key] = count + 1;
            pending++;
            flushNow = pending >= PendingCap;
        }

        if (flushNow)
        {
            Pulse();
        }
    }

    public bool OverCap
    {
        get
        {
            lock (gate)
            {
                return pending >= PendingCap;
            }
        }
    }

    public List<ProductClickCount> Drain(DateTime utc, bool includeOpen)
    {
        var current = Align(utc);
        var batch = new List<ProductClickCount>();
        lock (gate)
        {
            List<BucketKey>? selected = null;
            foreach (var pair in counts)
            {
                if (!includeOpen && pair.Key.BucketStart >= current)
                {
                    continue;
                }

                selected ??= [];
                selected.Add(pair.Key);
            }

            if (selected is null)
            {
                return batch;
            }

            foreach (var key in selected)
            {
                if (!counts.Remove(key, out var count) || count <= 0)
                {
                    continue;
                }

                pending -= count;
                batch.Add(new ProductClickCount(key.ProductId, key.Channel, key.BucketStart, count));
            }
        }

        return batch;
    }

    public void Restore(IReadOnlyList<ProductClickCount> batch)
    {
        lock (gate)
        {
            foreach (var item in batch)
            {
                var key = new BucketKey(item.ProductId, item.Channel, item.BucketStart);
                counts.TryGetValue(key, out var count);
                counts[key] = count + item.Count;
                pending += item.Count;
            }
        }
    }

    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return pulse.WaitAsync(timeout, cancellationToken);
    }

    public void Dispose()
    {
        pulse.Dispose();
    }

    private static DateTime Align(DateTime utc)
    {
        var moment = utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime();
        var ticks = (moment - DateTime.UnixEpoch).Ticks;
        const long windowTicks = TimeSpan.TicksPerSecond * 10;
        var floored = ticks >= 0
            ? ticks / windowTicks * windowTicks
            : (ticks - windowTicks + 1) / windowTicks * windowTicks;
        return DateTime.SpecifyKind(DateTime.UnixEpoch.AddTicks(floored), DateTimeKind.Utc);
    }

    private void Pulse()
    {
        try
        {
            pulse.Release();
        }
        catch (SemaphoreFullException)
        {
            // A flush is already waiting to run.
        }
    }

    private readonly record struct BucketKey(int ProductId, string Channel, DateTime BucketStart);
}
