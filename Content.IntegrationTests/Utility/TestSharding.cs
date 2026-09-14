using System;
using System.Linq;

namespace Content.IntegrationTests.Utility;

/// <summary>
///     Splits a large <c>[TestCaseSource]</c> array across several CI jobs via the
///     <c>CI_TEST_SHARD_INDEX</c> / <c>CI_TEST_SHARD_COUNT</c> environment variables, so one namespace
///     with an expensive fan-out (many cases, each needing real per-case work) can run as several
///     parallel matrix legs instead of a single job.
/// </summary>
/// <remarks>
///     This partitions the array before NUnit ever discovers it, so <c>--list-tests</c> and
///     <c>--filter</c> naturally see only that shard's slice - no test-name filtering required.
///     Both variables unset (local dev, or any unsharded job) runs the full array unchanged.
/// </remarks>
public static class TestSharding
{
    public static T[] Shard<T>(T[] items)
    {
        var indexVar = Environment.GetEnvironmentVariable("CI_TEST_SHARD_INDEX");
        var countVar = Environment.GetEnvironmentVariable("CI_TEST_SHARD_COUNT");

        if (string.IsNullOrEmpty(indexVar) || string.IsNullOrEmpty(countVar))
            return items;

        var index = int.Parse(indexVar);
        var count = int.Parse(countVar);

        if (count <= 1)
            return items;

        if (index < 0 || index >= count)
            throw new ArgumentOutOfRangeException(nameof(indexVar), $"CI_TEST_SHARD_INDEX ({index}) must be in [0, CI_TEST_SHARD_COUNT) ([0, {count})).");

        return items.Where((_, i) => i % count == index).ToArray();
    }
}
