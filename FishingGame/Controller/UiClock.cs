namespace FishingGame.Controller;

internal sealed class UiClock
{
    private CancellationTokenSource _cts = new();
    public int DelayMs { get; private set; } = 900;

    public event EventHandler<int>? DelayChanged;

    public void SetDelay(int ms)
    {
        if (ms < 0) ms = 0;
        DelayMs = ms;

        try { _cts.Cancel(); } catch {}
        _cts.Dispose();
        _cts = new();
        DelayChanged?.Invoke(this, ms);
    }

    public Task DelayAsync(CancellationToken external)
    {
        var linked = CancellationTokenSource.CreateLinkedTokenSource(external, _cts.Token);
        return Task.Delay(DelayMs, linked.Token).ContinueWith(_ => { }, TaskScheduler.Default);
    }
}