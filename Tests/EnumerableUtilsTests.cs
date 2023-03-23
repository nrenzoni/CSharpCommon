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

    [Test]
    public void Test_ScrambledEquals()
    {
        var dict1 = new Dictionary<string, int>
        {
            { "a", 1 },
            { "b", 2 }
        };

        var dic2 = new Dictionary<string, int>
        {
            { "b", 2 },
            { "a", 1 }
        };

        var dic3 = new Dictionary<string, int>
        {
            { "b", 2 },
        };

        Assert.IsTrue(
            dict1.ScrambledEquals(dic2));

        Assert.IsFalse(
            dict1.Equals(dic3));
    }
}
