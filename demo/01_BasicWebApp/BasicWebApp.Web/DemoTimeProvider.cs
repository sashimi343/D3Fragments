namespace BasicWebApp.Web;

public sealed class DemoTimeProvider : TimeProvider
{
    private readonly object _gate = new();
    private int _days;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
        {
            return System.GetUtcNow().AddDays(_days);
        }
    }

    public void Advance(int days)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(days);
        lock (_gate)
        {
            long total = (long)_days + days;
            if (total > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(days));
            }
            // Exceeding the representable date range throws ArgumentOutOfRangeException (the caller treats it as an input error)
            _ = System.GetUtcNow().AddDays(total);
            _days = (int)total;
        }
    }
}