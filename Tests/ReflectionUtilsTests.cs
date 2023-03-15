using CustomShared;

namespace Tests;

public class ReflectionUtilsTests
{
    public record GetPropertyNamesAndValuesRecord(
        DecimalWithInf DecimalWithInf);

    [Test]
    public void Test_GetPropertyNamesAndValues()
    {
        var record = new GetPropertyNamesAndValuesRecord(
            new DecimalWithInf(
                positiveInfinity: true
            ));

        var propertyNamesAndValues = ReflectionUtils.GetPropertyNamesAndValues(
            record,
            false);
    }

    record GetPropertyNamesRecord(
        decimal D1,
        DecimalWithInf D2);

    [Test]
    public void Test_GetPropertyNames()
    {
        var propertyNames =
            ReflectionUtils.GetPropertyNames(typeof(GetPropertyNamesRecord));
    }
}
