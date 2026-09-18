namespace XmlTransformer.Tests;

internal static class TestPaths
{
    public static string Stylesheet =>
        Path.Combine(AppContext.BaseDirectory, "transforms", "TeamcenterToMatmas05.xslt");

    public static string Fixture(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
}

internal sealed class FixedClock : XmlTransformer.Core.IClock
{
    public FixedClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; }
}
