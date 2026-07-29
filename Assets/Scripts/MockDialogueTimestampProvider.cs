using System;
using System.Globalization;

public class MockDialogueTimestampProvider
{
    private readonly DateTime startTimestamp;
    private readonly TimeSpan increment;
    private int emittedCount;

    public MockDialogueTimestampProvider()
        : this(new DateTime(2026, 5, 31, 7, 3, 0), TimeSpan.FromMinutes(17d))
    {
    }

    public MockDialogueTimestampProvider(DateTime startTimestamp, TimeSpan increment)
    {
        this.startTimestamp = startTimestamp;
        this.increment = increment;
    }

    public string GetNextTimestamp()
    {
        DateTime timestamp = startTimestamp.AddTicks(increment.Ticks * emittedCount);
        emittedCount++;
        return timestamp.ToString("dddd, dd MMMM yyyy, h:mm tt", CultureInfo.InvariantCulture);
    }
}
