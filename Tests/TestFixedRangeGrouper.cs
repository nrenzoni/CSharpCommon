using CustomShared;

namespace Tests;

public class TestFixedRangeGrouper
{
    [Test]
    public void TestFixedRangeGrouper_with_center()
    {
        var fixedRangeGrouper1 =
            new FixedRangeBinGrouper<decimal>(
                1,
                0);

        var inDecimals = new[] { 1.1m, 1.2m, 2m, 3m, -.5m, -.7m };

        foreach (var dec in inDecimals)
        {
            fixedRangeGrouper1.Emplace(
                dec,
                dec);
        }

        var binsWithCountsIncludingEmpty =
            fixedRangeGrouper1
                .GetBinsWithCountsIncludingEmpty()
                .ToList();

        var middleStartKey = 0m - 1m / 2m;

        Assert.That(fixedRangeGrouper1.Groups.ContainsKey(middleStartKey));

        Assert.That(
            binsWithCountsIncludingEmpty.Count,
            Is.EqualTo(4));

        var binnedElementsCount =
            binsWithCountsIncludingEmpty
                .Select(x => (int)x.Count)
                .Sum();

        Assert.That(
            binnedElementsCount,
            Is.EqualTo(inDecimals.Length));
    }

    [Test]
    public void TestFixedRangeGrouper_without_center()
    {
        var fixedRangeGrouper1 =
            new FixedRangeBinGrouper<decimal>(
                1,
                null);

        var inDecimals = new[] { 1.1m, 1.2m, 2m, 3m, -.5m, -.7m };

        foreach (var dec in inDecimals)
        {
            fixedRangeGrouper1.Emplace(
                dec,
                dec);
        }

        var binsWithCountsIncludingEmpty =
            fixedRangeGrouper1
                .GetBinsWithCountsIncludingEmpty()
                .ToList();

        var binnedElementsCount =
            binsWithCountsIncludingEmpty
                .Select(x => (int)x.Count)
                .Sum();

        Assert.That(
            binnedElementsCount,
            Is.EqualTo(inDecimals.Length));
    }
}
