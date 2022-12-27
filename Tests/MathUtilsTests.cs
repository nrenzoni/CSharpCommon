using CustomShared;

namespace Tests;

public class MathUtilsTests
{
    [Test]
    public void Test_FractionalPart()
    {
        var x = 0.2M;
        var (fractionalPart, shiftCount) = x.FractionalPart();

        Assert.IsTrue(fractionalPart == 2);

        var y = 0.002M;
        var (fractionalPart2, shiftCount2) = y.FractionalPart();

        Assert.IsTrue(fractionalPart2 == 2);
        Assert.IsTrue(shiftCount2 == 3);
    }
}
