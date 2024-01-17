using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace EasyIntervals.Playground;

[ShortRunJob]
// [NativeMemoryProfiler]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class IntervalCollectionsBenchmarks
{
    private const int TotalIntervalsCount = 1_000_000;
    private const int IntersectionIntervalsCount = 1_000;
    private const int MaxStartLimit = 10_000_000;
    private const int MaxIntervalLength = 1_000;

    private const int MaxIntersectionIntervalLength = 100_000;

    private readonly ISet<Interval<int, int?>> _intervals;
    private readonly List<Interval<int, int?>> _seededIntersectionIntervals;

    public IntervalCollectionsBenchmarks()
    {
        _intervals = BenchmarkTools.CreateRandomIntervals(TotalIntervalsCount, MaxStartLimit, MaxIntervalLength);
        _seededIntersectionIntervals = Enumerable.Range(0, IntersectionIntervalsCount)
            .Select(i => BenchmarkTools.CreateRandomInterval(MaxStartLimit, MaxIntersectionIntervalLength))
            .ToList();
    }

    [Benchmark]
    public void TestManyConsecutiveIntersections_IntervalSet()
    {
        var intervalSet = new IntervalSet<int, int?>(_intervals);
        foreach (var intersectionInterval in _seededIntersectionIntervals)
        {
            var _ = intervalSet.Intersect(intersectionInterval);
        }
    }

    [Benchmark]
    public void TestManyConsecutiveIntersections_IntervalSet_AfterIntervalsMerge()
    {
        var intervalSet = new IntervalSet<int, int?>(_intervals).Merge();
        foreach (var intersectionInterval in _seededIntersectionIntervals)
        {
            var _ = intervalSet.Intersect(intersectionInterval);
        }
    }

    [Benchmark]
    public void TestManyConsecutiveIntersections_SortedSet()
    {
        var intervalSet = new SortedSet<Interval<int, int?>>(_intervals, IntervalComparer<int, int?>.Create(Comparer<int>.Default));
        foreach (var intersectionInterval in _seededIntersectionIntervals)
        {
            var _ = new SortedSet<Interval<int, int?>>(intervalSet.GetViewBetween(
                    (intersectionInterval.Start, intersectionInterval.Start, IntervalType.Closed),
                    (intersectionInterval.End, intersectionInterval.End, IntervalType.Closed)),
                    intervalSet.Comparer);
        }
    }

    [Benchmark]
    public void Test100XMoreIntersectionsThanInserts_IntervalSet()
    {
        var seededIntervals = _intervals.Take(1_000);
        var intervalSet = new IntervalSet<int, int?>();
        foreach (var interval in seededIntervals)
        {
            intervalSet.Add(interval);
            var intersectionIntervals = _seededIntersectionIntervals.Take(100);
            foreach (var intersectionInterval in intersectionIntervals)
            {
                var _ = intervalSet.Intersect(intersectionInterval);
            }
        }
    }

    [Benchmark]
    public void Test100XMoreIntersectionsThanInserts_SortedSet()
    {
        var intervalSet = new SortedSet<Interval<int, int?>>(IntervalComparer<int, int?>.Create(Comparer<int>.Default));
        var seededIntervals = _intervals.Take(1_000);
        foreach (var itv in seededIntervals)
        {
            intervalSet.Add((itv.Start, itv.End, itv.Type));
            var intersectionIntervals = _seededIntersectionIntervals.Take(100);
            foreach (var intersectionInterval in intersectionIntervals)
            {
                var _ = new SortedSet<Interval<int, int?>>(intervalSet.GetViewBetween(
                        (intersectionInterval.Start, intersectionInterval.Start, IntervalType.Closed),
                        (intersectionInterval.End, intersectionInterval.End, IntervalType.Closed)),
                        intervalSet.Comparer);
            }
        }
    }
}