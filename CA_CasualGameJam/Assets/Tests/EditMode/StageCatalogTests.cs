using NUnit.Framework;

public class StageCatalogTests
{
    [Test]
    public void SixtyPuzzlesMapToScenesAndDisplayNumbersHaveThreeKinds()
    {
        Assert.That(StageCatalog.Count, Is.EqualTo(60));
        Assert.That(StageCatalog.SceneName(1), Is.EqualTo("SampleScene"));
        for (int number = 2; number <= 60; number++)
            Assert.That(StageCatalog.SceneName(number), Is.EqualTo($"Stage{number:00}"));
        Assert.That(StageCatalog.SceneName(0), Is.Empty);
        Assert.That(StageCatalog.SceneName(61), Is.Empty, "Stage61 reuses a normal scene rather than introducing a puzzle61.");
        Assert.That(StageCatalog.Kind(4), Is.EqualTo(StageKind.Tutorial));
        Assert.That(StageCatalog.Kind(5), Is.EqualTo(StageKind.Normal));
        Assert.That(StageCatalog.Kind(30), Is.EqualTo(StageKind.Normal));
        Assert.That(StageCatalog.Kind(31), Is.EqualTo(StageKind.Normal));
        Assert.That(StageCatalog.Kind(60), Is.EqualTo(StageKind.Normal));
        Assert.That(StageCatalog.Kind(61), Is.EqualTo(StageKind.Extra));
    }

    [Test]
    public void FirstExtraDrawExcludesTheLastEightNormalPuzzlesAndAllTutorials()
    {
        var candidates = StageCatalog.ExtraCandidates(new[] { 53, 54, 55, 56, 57, 58, 59, 60 });
        CollectionAssert.AreEqual(System.Linq.Enumerable.Range(5, 48), candidates);
    }
}
