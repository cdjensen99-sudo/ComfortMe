namespace ComfortMe;

internal static class ModConstants
{
    public const string ModGuid = "hardwire99.comfortme";
    public const string ModName = "ComfortMe";
    public const string ModVersion = "1.3.0";
    public const string BuildLabel = "1.3.0";

    /// <summary>Vanilla comfort scan radius in meters (3D).</summary>
    public const float ComfortRadius = 10f;

    /// <summary>
    /// Shared Hardwire99 language files (one JSON per language, prefixed keys per mod).
    /// Repo: https://github.com/cdjensen99-sudo/translations
    /// </summary>
    public const string TranslationsUrl =
        "https://raw.githubusercontent.com/cdjensen99-sudo/translations/main/";
}
