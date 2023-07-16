using CustomShared;
using NUnit.Framework;

namespace Tests;

[TestFixture]
public class DateUtilTests
{
    [Test]
    public void ParseToInstant_test()
    {
        var str = "2023-01-03T13:00:00.9311520Z";

        var parsedInstant = DateUtils.ParseToInstant(str);
    }   
}
