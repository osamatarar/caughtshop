using Caught.Data;

namespace Caught.Services;

public sealed class ProductClickFlushService(
    ProductClickBuffer buffer,
    IServiceScopeFactory scopes,
    ILogger<ProductClickFlushService> logger) : BackgroundService
{
    private static readonly TimeSpan Sweep = TimeSpan.FromSeconds(2);
    private readonly SemaphoreSlim gate = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var failed = false;
            try
            {
                await Flush(buffer.OverCap, CancellationToken.None);
            }
            catch (Exception exception)
            {
                failed = true;
                logger.LogError(exception, "Product click flush failed.");
            }

            try
            {
                if (failed)
                {
                    await Task.Delay(Sweep, stoppingToken);
                }
                else
                {
                    await buffer.WaitAsync(Sweep, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        try
        {
            await Flush(includeOpen: true, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown ended before the last batch could be written.
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Product click flush failed during shutdown.");
        }
    }

    public override void Dispose()
    {
        gate.Dispose();
        base.Dispose();
    }

    private async Task Flush(bool includeOpen, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var batch = buffer.Drain(DateTime.UtcNow, includeOpen);
            if (batch.Count == 0)
            {
                return;
            }

            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<Db>();
                await db.Execute(
                    "SELECT caught.add_product_clicks(@ProductIds, @Channels, @BucketStarts, @Counts)",
                    new
                    {
                        ProductIds = batch.Select(item => item.ProductId).ToArray(),
                        Channels = batch.Select(item => item.Channel).ToArray(),
                        BucketStarts = batch.Select(item => new DateTimeOffset(item.BucketStart)).ToArray(),
                        Counts = batch.Select(item => item.Count).ToArray(),
                    },
                    cancellationToken);
            }
            catch
            {
                buffer.Restore(batch);
                throw;
            }
        }
        finally
        {
            gate.Release();
        }
    }
}
