using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Toolbelts
{
    internal static class ModLocalization
    {
        private static Dictionary<string, Dictionary<string, string>>? _data;

        private static Dictionary<string, Dictionary<string, string>> Data =>
            _data ??= Load();

        internal static void Reload() => _data = null;

        private static Dictionary<string, Dictionary<string, string>> Load()
        {
            var asm = Assembly.GetExecutingAssembly();

            // Find the embedded localization.json by suffix (name can vary by project config)
            var resourceName = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("localization.json"));

            if (resourceName != null)
            {
                try
                {
                    using var stream = asm.GetManifestResourceStream(resourceName)!;
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    var result = TryParse(reader.ReadToEnd());
                    if (result != null)
                    {
                        MelonLogger.Msg($"[ClothingExpanded] Loaded localization ({result.Count} languages) from '{resourceName}'.");
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"[ClothingExpanded] Failed to read embedded resource '{resourceName}': {ex.Message}");
                }
            }
            else
            {
                var all = string.Join(", ", asm.GetManifestResourceNames());
                MelonLogger.Warning($"[ClothingExpanded] localization.json not found in embedded resources. Available: [{all}]");
            }

            return Fallback;
        }

        private static Dictionary<string, Dictionary<string, string>>? TryParse(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[ClothingExpanded] Localization JSON parse error: {ex.Message}");
                return null;
            }
        }

        private static string? s_DetectedLang;

        internal static string Get(string key)
        {
            if (s_DetectedLang == null)
            {
                var raw = Localization.Language;
                s_DetectedLang = NormalizeLanguage(raw);
                MelonLogger.Msg($"[ClothingExpanded] Language: raw='{raw}' → '{s_DetectedLang}'");
            }

            var data = Data;
            if (data.TryGetValue(s_DetectedLang, out var dict) && dict.TryGetValue(key, out var val))   return val;
            if (data.TryGetValue("English",       out var en)   && en.TryGetValue(key,   out var enVal)) return enVal;
            return key;
        }

        // Maps whatever Localization.Language returns to our JSON language keys.
        // TLD returns the English display name, but we guard against lower-case or aliases.
        private static string NormalizeLanguage(string? raw) => (raw ?? "").ToLowerInvariant() switch
        {
            "russian" or "ru"                                    => "Russian",
            "french"  or "fr" or "français"                     => "French",
            "german"  or "de" or "deutsch"                      => "German",
            "spanish" or "es" or "español"                      => "Spanish",
            "brazilian" or "portuguese" or "pt" or "pt-br"
                or "português"                                   => "Brazilian",
            "polish"  or "pl" or "polski"                       => "Polish",
            "czech"   or "cs" or "čeština"                      => "Czech",
            "turkish" or "tr" or "türkçe"                       => "Turkish",
            "italian" or "it" or "italiano"                     => "Italian",
            "dutch"   or "nl" or "nederlands"                   => "Dutch",
            "japanese" or "ja" or "日本語"                       => "Japanese",
            "korean"  or "ko" or "한국어"                        => "Korean",
            "chinesesimplified"  or "zh-cn" or "zh_cn"
                or "简体中文"                                     => "ChineseSimplified",
            "chinesetraditional" or "zh-tw" or "zh_tw"
                or "繁體中文"                                     => "ChineseTraditional",
            _                                                    => "English",
        };

        private static readonly Dictionary<string, Dictionary<string, string>> Fallback = new()
        {
            ["English"] = new()
            {
                ["GAMEPLAY_TB_AttachBeltLabel"]      = "Attach Belt",
                ["GAMEPLAY_TB_DetachBeltLabel"]      = "Detach Belt",
                ["GAMEPLAY_TB_AttachCramponsLabel"]  = "Attach Crampons",
                ["GAMEPLAY_TB_DetachCramponsLabel"]  = "Detach Crampons",
                ["GAMEPLAY_TB_AttachScabbardLabel"]  = "Attach Scabbard",
                ["GAMEPLAY_TB_DetachScabbardLabel"]  = "Detach Scabbard",
                ["GAMEPLAY_TB_NoBelt"]               = "No toolbelt in inventory",
                ["GAMEPLAY_TB_NoCrampons"]           = "No crampons in inventory",
                ["GAMEPLAY_TB_NoScabbard"]           = "No rifle scabbard in inventory",
                ["GAMEPLAY_TB_AttachingProgressBar"] = "Attaching...",
                ["GAMEPLAY_TB_DetachingProgressBar"] = "Detaching...",
            }
        };
    }

    [HarmonyPatch(typeof(Localization), nameof(Localization.Get))]
    internal static class Patch_LocalizationGet
    {
        private static readonly HashSet<string> s_OurKeys = new()
        {
            "GAMEPLAY_TB_AttachBeltLabel",
            "GAMEPLAY_TB_DetachBeltLabel",
            "GAMEPLAY_TB_AttachCramponsLabel",
            "GAMEPLAY_TB_DetachCramponsLabel",
            "GAMEPLAY_TB_AttachScabbardLabel",
            "GAMEPLAY_TB_DetachScabbardLabel",
            "GAMEPLAY_TB_NoBelt",
            "GAMEPLAY_TB_NoCrampons",
            "GAMEPLAY_TB_NoScabbard",
            "GAMEPLAY_TB_AttachingProgressBar",
            "GAMEPLAY_TB_DetachingProgressBar",
        };

        static void Postfix(string key, ref string __result)
        {
            if (key == null || !s_OurKeys.Contains(key)) return;
            __result = ModLocalization.Get(key);
        }
    }
}
