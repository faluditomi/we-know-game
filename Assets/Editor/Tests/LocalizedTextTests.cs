using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class LocalizedTextTests
{
    [Test]
    public void EveryKeyHasEnglishText()
    {
        foreach(string key in LocalizedText.AllKeys)
            Assert.IsTrue(LocalizedText.HasTranslation(GameLanguage.English, key), key);
    }

    [Test]
    public void EveryKeyHasHungarianText()
    {
        foreach(string key in LocalizedText.AllKeys)
            Assert.IsTrue(LocalizedText.HasTranslation(GameLanguage.Hungarian, key), key);
    }

    [Test]
    public void KeysAreUnique()
    {
        CollectionAssert.AllItemsAreUnique(LocalizedText.AllKeys);
    }

    [Test]
    public void LanguagesReturnTheirOwnText()
    {
        Assert.AreEqual("PLAY", LocalizedText.Get(GameLanguage.English, LocalizedText.Play));
        Assert.AreEqual("JÁTÉK", LocalizedText.Get(GameLanguage.Hungarian, LocalizedText.Play));
    }

    [Test]
    public void MissingTranslationFallsBackToEnglish()
    {
        var english = new Dictionary<string, string> { { "a", "Apple" }, { "b", "Bee" } };
        var hungarian = new Dictionary<string, string> { { "a", "Alma" }, { "b", "  " } };

        Assert.AreEqual("Alma", LocalizedText.Lookup(hungarian, english, "a"));
        Assert.AreEqual("Bee", LocalizedText.Lookup(hungarian, english, "b"));
        Assert.IsNull(LocalizedText.Lookup(hungarian, english, "c"));
    }

    [Test]
    public void UnknownKeyIsShownAsTheKey()
    {
        // Unknown keys are logged once per Editor session, so use a fresh key every run.
        string key = "NoSuchKey" + System.Guid.NewGuid().ToString("N");
        LogAssert.Expect(LogType.Warning, $"LocalizedText: unknown key \"{key}\".");
        Assert.AreEqual(key, LocalizedText.Get(GameLanguage.Hungarian, key));
    }

    [Test]
    public void FormatSubstitutesArguments()
    {
        Assert.AreEqual("30 s", LocalizedText.Format(GameLanguage.English, LocalizedText.SecondsValue, 30));
        Assert.AreEqual("30 mp", LocalizedText.Format(GameLanguage.Hungarian, LocalizedText.SecondsValue, 30));
        Assert.AreEqual("3 × 4  ·  6 pár", LocalizedText.Format(GameLanguage.Hungarian, LocalizedText.PickerSize, 3, 4, 6));
    }

    [Test]
    public void NativeNamesDoNotDependOnTheCurrentLanguage()
    {
        Assert.AreEqual("English", GameLanguages.NativeName(GameLanguage.English));
        Assert.AreEqual("Magyar", GameLanguages.NativeName(GameLanguage.Hungarian));
    }
}
