namespace EasyIntervals;

using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents intersection type between two intervals.
/// </summary>
public enum IntersectionType
{
    /// <summary>
    /// Given interval intersects in any way another interval.
    /// </summary>
    Any,

    /// <summary>
    /// Given interval covers another interval.
    /// </summary>
    Cover,

    /// <summary>
    /// Given interval is within another interval.
    /// </summary>
    Within,
}

/// <summary>
/// IntervalSet is a collection for storing large amount of unique intervals
/// where multiple add, remove and search operations can be done in efficient time.
/// </summary>
/// <remarks>
/// It's an implementation of <see href="https://en.wikipedia.org/wiki/Interval_tree#Augmented_tree">Augmented Interval Tree</see>
/// abstract data structure, using self-balancing Binary Search Tree (BST) - <see href="https://en.wikipedia.org/wiki/AA_tree">AA Tree</see>.
/// It provides functionalities for add, remove, intersect, except, union and merge of intervals.
/// </remarks>
/// <typeparam name="TLimit">Represents the limit type of start and end of interval</typeparam>
public class IntervalSet<TLimit> : BaseIntervalSet<IntervalSet<TLimit>, Interval<TLimit>, TLimit>
{
    /// <summary>
    /// Creates an empty IntervalSet.
    /// </summary>
    public IntervalSet()
        : this(Enumerable.Empty<Interval<TLimit>>(), Comparer<TLimit>.Default)
    { }

    /// <summary>
    /// Creates an IntervalSet with limit <c>comparison</c>.
    /// </summary>
    /// <param name="comparison">comparison</param>
    public IntervalSet(Comparison<TLimit> comparison)
        : this(Enumerable.Empty<Interval<TLimit>>(), Comparer<TLimit>.Create(comparison))
    { }

    /// <summary>
    /// Creates an IntervalSet with limit <c>comparer</c>.
    /// </summary>
    /// <param name="comparer">comparer</param>
    public IntervalSet(IComparer<TLimit> comparer)
        : this(Enumerable.Empty<Interval<TLimit>>(), comparer)
    { }

    /// <summary>
    /// Creates IntervalSet with <c>intervals</c>.
    /// </summary>
    /// <param name="intervals">intervals</param>
    public IntervalSet(IEnumerable<Interval<TLimit>> intervals)
        : this(intervals, Comparer<TLimit>.Default)
    { }

    /// <summary>
    /// Creates IntervalSet with limit <c>comparer</c> and <c>intervals</c>.
    /// </summary>
    /// <param name="comparer">comparer</param>
    /// <param name="intervals">intervals</param>
    public IntervalSet(IEnumerable<Interval<TLimit>> intervals, IComparer<TLimit> comparer)
        : this(intervals, areIntervalsSorted: false, areIntervalsUnique: false, comparer)
    { }

    /// <summary>
    /// Creates IntervalSet with limit <c>comparer</c>, <c>intervals</c> and flag if intervals are sorted.
    /// </summary>
    /// <param name="comparer">comparer</param>
    /// <param name="intervals">intervals</param>
    private IntervalSet(
        IEnumerable<Interval<TLimit>> intervals, bool areIntervalsSorted, bool areIntervalsUnique, IComparer<TLimit> comparer)
        : base(intervals, areIntervalsSorted, areIntervalsUnique, IntervalComparer<TLimit>.Create(comparer), comparer)
    { }

    protected override IntervalSet<TLimit> CreateInstance(
        IEnumerable<Interval<TLimit>> intervals,
        bool areIntervalsSorted,
        bool areIntervalsUnique,
        IComparer<TLimit> limitComparer) => new IntervalSet<TLimit>(intervals, areIntervalsSorted: true, areIntervalsUnique: true, limitComparer);

    /// <summary>
    /// Removes intervals intersecting <c>limit</c>.
    /// </summary>
    /// <param name="limit">Limit</param>
    /// <returns>true if any intervals are intersected and removed; otherwise, false.</returns>
    public bool Remove(TLimit limit)
    {
        var intervalsToRemove = Intersect(limit);
        return intervalsToRemove.Select(itv => Remove(itv)).Any();
    }

    /// <summary>
    /// Intersects the set with <c>limit</c>.
    /// </summary>
    /// <param name="limit"></param>
    /// <returns>interval set with intersected intervals.</returns>
    public IntervalSet<TLimit> Intersect(TLimit limit) =>
        Intersect((limit, limit, IntervalType.Closed), IntersectionType.Any);

    /// <summary>
    /// Merges intersecting intervals.
    /// </summary>
    /// <returns>interval set with merged intervals.</returns>
    public IntervalSet<TLimit> Merge()
    {
        var result = new List<Interval<TLimit>>();
        Merge(_aaTree.Root, result);

        return new IntervalSet<TLimit>(result, areIntervalsSorted: true, areIntervalsUnique: true, _limitComparer);
    }

    private void Merge(
        AATree<Interval<TLimit>>.Node? node, IList<Interval<TLimit>> intervals)
    {
        if (node is null)
        {
            return;
        }

        Merge(node.Left, intervals);

        MergeCurrent(node, intervals);

        Merge(node.Right, intervals);
    }

    private void MergeCurrent(AATree<Interval<TLimit>>.Node? node, IList<Interval<TLimit>> intervals)
    {
        if (intervals.Count > 0)
        {
            var lastIndex = intervals.Count - 1;
            var precedingInterval = intervals[lastIndex];
            var isMerged = TryMerge(precedingInterval, node!.Value, out Interval<TLimit> mergedInterval);

            if (isMerged)
            {
                intervals[lastIndex] = mergedInterval;
                return;
            }
        }

        intervals.Add(node!.Value);
    }

    private bool TryMerge(
        in Interval<TLimit> precedingInterval, in Interval<TLimit> followingInterval, out Interval<TLimit> result)
    {
        if (IntervalTools.HasAnyIntersection(precedingInterval, followingInterval, _limitComparer)
                || IntervalTools.Touch(precedingInterval, followingInterval, _limitComparer))
        {
            result = IntervalTools.Merge(precedingInterval, followingInterval, _limitComparer);
            return true;
        }

        result = default!;
        return false;
    }
}
