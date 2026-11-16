using NUnit.Framework;
using UnityEngine;

public class TileDescriptionTests
{
    private Tile tile;

    [SetUp]
    public void CreateTile()
    {
        tile = ScriptableObject.CreateInstance<Tile>();
        tile.foundDescription = "this is an apple";
    }

    [TearDown]
    public void DestroyTile()
    {
        Object.DestroyImmediate(tile);
    }

    [Test]
    public void EnglishUsesTheEnglishDescription()
    {
        tile.foundDescriptionHungarian = "ez egy alma";
        Assert.AreEqual("this is an apple", tile.GetDescription(GameLanguage.English));
    }

    [Test]
    public void HungarianUsesTheHungarianDescription()
    {
        tile.foundDescriptionHungarian = "ez egy alma";
        Assert.AreEqual("ez egy alma", tile.GetDescription(GameLanguage.Hungarian));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void MissingHungarianFallsBackToEnglish(string hungarian)
    {
        tile.foundDescriptionHungarian = hungarian;
        Assert.AreEqual("this is an apple", tile.GetDescription(GameLanguage.Hungarian));
    }
}
