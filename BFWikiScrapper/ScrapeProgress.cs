using System.Runtime.CompilerServices;

namespace BFWikiScrapper;

public sealed class ScrapeProgress
{
    private int _activeTasks;
    private int _failedUnits;
    private int _pagesDiscovered;
    private int _unitsScraped;

    public int PagesDiscovered => _pagesDiscovered;
    public int UnitsScraped => _unitsScraped;
    public int FailedUnits => _failedUnits;
    public int ActiveTasks => _activeTasks;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementPages()
    {
        Interlocked.Increment(ref _pagesDiscovered);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementUnits()
    {
        Interlocked.Increment(ref _unitsScraped);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementFailures()
    {
        Interlocked.Increment(ref _failedUnits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementActive()
    {
        Interlocked.Increment(ref _activeTasks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DecrementActive()
    {
        Interlocked.Decrement(ref _activeTasks);
    }
}