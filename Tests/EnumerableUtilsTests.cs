using CustomShared;

namespace Tests;

public class EnumerableUtilsTests
{
    [Test]
    public void Test_Pairwise()
    {
        var range = Enumerable.Range(
            0,
            10);

        var pairs = range.Pairwise(2).ToArray();
    }

    [Test]
    public void Test_ShuffleFisherYates()
    {
        var array = Enumerable.Range(
                0,
                100)
            .ToArray();

        array.ShuffleFisherYates();
    }
}
