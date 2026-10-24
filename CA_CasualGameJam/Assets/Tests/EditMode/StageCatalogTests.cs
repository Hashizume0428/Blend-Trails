using NUnit.Framework;

public class StageCatalogTests
{
    [Test]
    public void ThirtyPuzzlesMapToScenesAndDisplayNumbersHaveThreeKinds()
    {
        Assert.That(StageCatalog.Count, Is.EqualTo(30));
        Assert.That(StageCatalog.SceneName(1), Is.EqualTo("SampleScene"));
        for (int number = 2; number <= 30; number++)
            Assert.That(StageCatalog.SceneName(number), Is.EqualTo($"Stage{number:00}"));
        Assert.That(StageCatalog.SceneName(0), Is.Empty);
        Assert.That(StageCatalog.SceneName(31), Is.Empty, "Stage31 reuses a normal scene rather than introducing a puzzle31.");
        Assert.That(StageCatalog.Kind(4), Is.EqualTo(StageKind.Tutorial));
        Assert.That(StageCatalog.Kind(5), Is.EqualTo(StageKind.Normal));
        Assert.That(StageCatalog.Kind(30), Is.EqualTo(StageKind.Normal));
        Assert.That(StageCatalog.Kind(31), Is.EqualTo(StageKind.Extra));
    }

    [Test]
    public void FirstExtraDrawExcludesTheLastEightNormalPuzzlesAndAllTutorials()
    {
        var candidates = StageCatalog.ExtraCandidates(new[] { 23, 24, 25, 26, 27, 28, 29, 30 });
        CollectionAssert.AreEqual(System.Linq.Enumerable.Range(5, 18), candidates);
    }
}
