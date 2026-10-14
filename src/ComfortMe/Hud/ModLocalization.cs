using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using HarmonyLib;

namespace ComfortMe;

/// <summary>
/// Loads HUD strings from translations/*.json into vanilla Localization.
/// One file per language is shared by Hardwire99 mods (comfortme_* keys for this plugin).
/// Piece names stay on Valheim's own language files.
/// </summary>
internal static class ModLocalization
{
    private const string FolderName = "translations";
    private const string EmbeddedEnglishName = "ComfortMe.English.json";
    private const int DownloadTimeoutMs = 8000;
    private static readonly Dictionary<string, string> english = new Dictionary<string, string>(StringComparer.Ordinal);
    private static readonly object downloadLock = new object();
    private static readonly HashSet<string> queuedDownloads = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static string appliedLanguage;
    private static bool hooked;
    private static bool loggedFolder;
    private static volatile bool pendingReload;
    private static volatile bool englishFetched;

    internal static void Hook()
    {
        if (hooked)
        {
            return;
        }

        hooked = true;
        Localization.OnLanguageChange += ApplySelected;
        ApplySelected();
    }

    internal static void EnsureApplied()
    {
        if (Localization.instance == null)
        {
            return;
        }

        if (pendingReload)
        {
            pendingReload = false;
            english.Clear();
            appliedLanguage = null;
            ApplySelected();
            return;
        }

        string language = CurrentLanguage();
        if (appliedLanguage == language && HasOurWord())
        {
            return;
        }

        ApplySelected();
    }

    internal static string T(string key)
    {
        EnsureApplied();
        string fallback = Fallback(key);
        if (Localization.instance == null)
        {
            return fallback;
        }

        string localized = Localization.instance.Localize("$" + key);
        if (string.IsNullOrEmpty(localized) || localized == "$" + key)
        {
            return fallback;
        }

        return localized;
    }

    internal static void ApplySelected()
    {
        QueueDownload();
        string language = CurrentLanguage();
        Dictionary<string, string> words = LoadMerged(language);
        Localization loc = Localization.instance;
        if (loc == null)
        {
            return;
        }

        foreach (KeyValuePair<string, string> pair in words)
        {
            SetWord(loc, pair.Key, pair.Value);
        }

        appliedLanguage = language;
        GroupCatalogPanel.InvalidateText();
        ComfortMePlugin.Log?.LogInfo($"Localization applied ({language}, {words.Count} string(s)).");
    }

    private static string CurrentLanguage()
    {
        if (Localization.instance == null)
        {
            return "English";
        }

        string language = Localization.instance.GetSelectedLanguage();
        return string.IsNullOrEmpty(language) ? "English" : language;
    }

    private static Dictionary<string, string> LoadMerged(string language)
    {
        if (english.Count == 0)
        {
            LoadFile("English", english);
        }

        Dictionary<string, string> merged = new Dictionary<string, string>(english, StringComparer.Ordinal);
        string file = MapLanguageFile(language);
        if (!string.Equals(file, "English", StringComparison.OrdinalIgnoreCase))
        {
            Dictionary<string, string> overlay = new Dictionary<string, string>(StringComparer.Ordinal);
            LoadFile(file, overlay);
            foreach (KeyValuePair<string, string> pair in overlay)
            {
                merged[pair.Key] = pair.Value;
            }
        }

        return merged;
    }

    private static string MapLanguageFile(string language)
    {
        if (string.Equals(language, "Chinese_Simplified", StringComparison.OrdinalIgnoreCase)
            || string.Equals(language, "ChineseSimplified", StringComparison.OrdinalIgnoreCase)
            || string.Equals(language, "SChinese", StringComparison.OrdinalIgnoreCase))
        {
            return "Chinese";
        }

        return language;
    }

    private static void QueueDownload()
    {
        if (ModConfig.DownloadTranslations == null || !ModConfig.DownloadTranslations.Value)
        {
            return;
        }

        string baseUrl = ModConfig.TranslationsUrl != null ? ModConfig.TranslationsUrl.Value : null;
        if (string.IsNullOrEmpty(baseUrl))
        {
            return;
        }

        string language = MapLanguageFile(CurrentLanguage());
        if (!IsSafeLanguageName(language))
        {
            return;
        }

        lock (downloadLock)
        {
            if (!queuedDownloads.Add(language))
            {
                return;
            }
        }

        string url = baseUrl.TrimEnd('/') + "/";
        ThreadPool.QueueUserWorkItem(_ => FetchFromGitHub(url, language));
    }

    private static void FetchFromGitHub(string baseUrl, string language)
    {
        try
        {
            TryEnableTls12();
            bool updated = false;
            if (!englishFetched && TryDownloadFile(baseUrl, "English"))
            {
                englishFetched = true;
                updated = true;
            }

            if (!string.Equals(language, "English", StringComparison.OrdinalIgnoreCase)
                && TryDownloadFile(baseUrl, language))
            {
                updated = true;
            }

            if (updated)
            {
                pendingReload = true;
            }
        }
        catch (Exception ex)
        {
            ComfortMePlugin.Log?.LogInfo($"Translation download skipped: {ex.Message}");
        }
    }

    private static bool TryDownloadFile(string baseUrl, string language)
    {
        if (!IsSafeLanguageName(language))
        {
            return false;
        }

        string url = baseUrl + language + ".json";
        try
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.UserAgent = "ComfortMe/" + ModConstants.ModVersion;
            request.Timeout = DownloadTimeoutMs;
            request.ReadWriteTimeout = DownloadTimeoutMs;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            {
                if (response.StatusCode != HttpStatusCode.OK || stream == null)
                {
                    return false;
                }

                string json;
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    json = reader.ReadToEnd();
                }

                Dictionary<string, string> parsed = new Dictionary<string, string>(StringComparer.Ordinal);
                ParseObject(json, parsed);
                if (parsed.Count == 0)
                {
                    ComfortMePlugin.Log?.LogInfo($"GitHub {language}.json was empty or invalid; keeping local file.");
                    return false;
                }

                string dir = CacheDirectory();
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, language + ".json"), json, new UTF8Encoding(false));
                ComfortMePlugin.Log?.LogInfo($"Updated translations from GitHub: {language}.json ({parsed.Count} string(s)).");
                return true;
            }
        }
        catch (WebException ex)
        {
            HttpWebResponse response = ex.Response as HttpWebResponse;
            if (response != null && response.StatusCode == HttpStatusCode.NotFound)
            {
                ComfortMePlugin.Log?.LogInfo($"No GitHub translations for {language} (using local files).");
            }
            else
            {
                ComfortMePlugin.Log?.LogInfo($"Translation download skipped ({language}): {ex.Message}");
            }

            return false;
        }
        catch (Exception ex)
        {
            ComfortMePlugin.Log?.LogInfo($"Translation download skipped ({language}): {ex.Message}");
            return false;
        }
    }

    private static string CacheDirectory()
    {
        string dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        return Path.Combine(dllDir, FolderName);
    }

    private static bool IsSafeLanguageName(string language)
    {
        if (string.IsNullOrEmpty(language) || language.Length > 64)
        {
            return false;
        }

        for (int i = 0; i < language.Length; i++)
        {
            char c = language[i];
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
            {
                return false;
            }
        }

        return true;
    }

    private static void TryEnableTls12()
    {
        try
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
        }
        catch
        {
            // Older Mono may already have a usable protocol set.
        }
    }

    private static void LoadFile(string language, Dictionary<string, string> dest)
    {
        dest.Clear();
        string path = FindFile(language);
        if (path != null)
        {
            ParseObject(File.ReadAllText(path, Encoding.UTF8), dest);
            return;
        }

        if (string.Equals(language, "English", StringComparison.OrdinalIgnoreCase)
            && TryLoadEmbeddedEnglish(dest))
        {
            return;
        }

        if (!string.Equals(language, "English", StringComparison.OrdinalIgnoreCase))
        {
            ComfortMePlugin.Log?.LogWarning($"Localization file '{language}.json' was not found.");
        }
    }

    private static bool TryLoadEmbeddedEnglish(Dictionary<string, string> dest)
    {
        Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedEnglishName);
        if (stream == null)
        {
            ComfortMePlugin.Log?.LogWarning("Embedded English.json was not found in the DLL.");
            return false;
        }

        using (stream)
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        {
            ParseObject(reader.ReadToEnd(), dest);
        }

        return dest.Count > 0;
    }

    private static string FindFile(string language)
    {
        string fileName = language + ".json";
        string dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string[] roots =
        {
            dllDir,
            Path.Combine(dllDir ?? string.Empty, FolderName),
            Path.GetFullPath(Path.Combine(dllDir ?? ".", "..", "..", "..", FolderName)),
        };

        string logged = null;
        for (int i = 0; i < roots.Length; i++)
        {
            string root = roots[i];
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                continue;
            }

            if (!loggedFolder)
            {
                logged = root;
            }

            string direct = Path.Combine(root, fileName);
            if (File.Exists(direct))
            {
                loggedFolder = true;
                return direct;
            }
        }

        if (!loggedFolder && logged != null)
        {
            loggedFolder = true;
            ComfortMePlugin.Log?.LogInfo($"Localization folder: {logged}");
        }

        return null;
    }

    private static void SetWord(Localization loc, string key, string value)
    {
        MethodInfo add = AccessTools.Method(typeof(Localization), "AddWord", new[] { typeof(string), typeof(string) });
        if (add != null)
        {
            add.Invoke(loc, new object[] { key, value });
            return;
        }

        Dictionary<string, string> best = null;
        foreach (FieldInfo field in AccessTools.GetDeclaredFields(typeof(Localization)))
        {
            if (field.FieldType != typeof(Dictionary<string, string>))
            {
                continue;
            }

            Dictionary<string, string> dict = field.GetValue(loc) as Dictionary<string, string>;
            if (dict == null)
            {
                continue;
            }

            if (best == null || dict.Count > best.Count)
            {
                best = dict;
            }
        }

        if (best != null)
        {
            best[key] = value;
            return;
        }

        ComfortMePlugin.Log?.LogWarning("Could not insert localization tokens into Localization.");
    }

    private static bool HasOurWord()
    {
        if (Localization.instance == null)
        {
            return false;
        }

        string localized = Localization.instance.Localize("$comfortme_category");
        return !string.IsNullOrEmpty(localized) && localized != "$comfortme_category";
    }

    private static string Fallback(string key)
    {
        if (english.Count == 0)
        {
            LoadFile("English", english);
        }

        return english.TryGetValue(key, out string value) ? value : key;
    }

    private static void ParseObject(string json, Dictionary<string, string> dest)
    {
        int i = 0;
        SkipWs(json, ref i);
        if (i >= json.Length || json[i] != '{')
        {
            return;
        }

        i++;
        while (i < json.Length)
        {
            SkipWs(json, ref i);
            if (i < json.Length && json[i] == '}')
            {
                return;
            }

            if (!TryReadString(json, ref i, out string key))
            {
                return;
            }

            SkipWs(json, ref i);
            if (i >= json.Length || json[i] != ':')
            {
                return;
            }

            i++;
            SkipWs(json, ref i);
            if (!TryReadString(json, ref i, out string value))
            {
                return;
            }

            if (!string.IsNullOrEmpty(key))
            {
                dest[key] = value ?? string.Empty;
            }

            SkipWs(json, ref i);
            if (i < json.Length && json[i] == ',')
            {
                i++;
            }
        }
    }

    private static void SkipWs(string json, ref int i)
    {
        while (i < json.Length && char.IsWhiteSpace(json[i]))
        {
            i++;
        }
    }

    private static bool TryReadString(string json, ref int i, out string value)
    {
        value = null;
        SkipWs(json, ref i);
        if (i >= json.Length || json[i] != '"')
        {
            return false;
        }

        i++;
        StringBuilder builder = new StringBuilder();
        while (i < json.Length)
        {
            char c = json[i++];
            if (c == '"')
            {
                value = builder.ToString();
                return true;
            }

            if (c != '\\' || i >= json.Length)
            {
                builder.Append(c);
                continue;
            }

            char esc = json[i++];
            switch (esc)
            {
                case '"':
                case '\\':
                case '/':
                    builder.Append(esc);
                    break;
                case 'b':
                    builder.Append('\b');
                    break;
                case 'f':
                    builder.Append('\f');
                    break;
                case 'n':
                    builder.Append('\n');
                    break;
                case 'r':
                    builder.Append('\r');
                    break;
                case 't':
                    builder.Append('\t');
                    break;
                case 'u':
                    if (i + 4 <= json.Length
                        && int.TryParse(json.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code))
                    {
                        builder.Append((char)code);
                        i += 4;
                    }

                    break;
                default:
                    builder.Append(esc);
                    break;
            }
        }

        return false;
    }
}
