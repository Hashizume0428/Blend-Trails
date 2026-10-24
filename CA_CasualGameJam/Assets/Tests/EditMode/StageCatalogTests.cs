using NUnit.Framework;

public class StageCatalogTests
{
    [Test]
    public void TenStagesMapToPlayableScenesAndUnknownNumbersStayUnavailable()
    {
        Assert.That(StageCatalog.Count, Is.EqualTo(10));
        Assert.That(StageCatalog.SceneName(1), Is.EqualTo("SampleScene"));
        for (int number = 2; number <= 10; number++)
            Assert.That(StageCatalog.SceneName(number), Is.EqualTo($"Stage{number:00}"));
        Assert.That(StageCatalog.SceneName(0), Is.Empty);
        Assert.That(StageCatalog.SceneName(11), Is.Empty);
    }
}
