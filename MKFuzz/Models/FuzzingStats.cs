namespace MKFuzz.Models;

public class FuzzingStats
{
    public int FuzzersAlive { get; set; }
    public long TotalExecs { get; set; }
    public int Crashes { get; set; }
    public double Coverage { get; set; }
    public int PendingItems { get; set; }
    public long ExecsPerSecond { get; set; }
}