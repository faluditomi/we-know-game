/// <summary>Display languages the game can be shown in. Stored by value; do not renumber.</summary>
public enum GameLanguage
{
    English = 0,
    Hungarian = 1
}

public static class GameLanguages
{
    /// <summary>Every supported language, in dropdown order.</summary>
    public static readonly GameLanguage[] All = { GameLanguage.English, GameLanguage.Hungarian };

    public static bool IsDefined(int value) => value >= 0 && value < All.Length;

    /// <summary>The language's name in that language, so it reads the same whatever the current language is.</summary>
    public static string NativeName(GameLanguage language)
    {
        switch(language)
        {
            case GameLanguage.Hungarian: return "Magyar";
            default: return "English";
        }
    }
}
