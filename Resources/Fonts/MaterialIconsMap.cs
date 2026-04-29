#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace AjroudSoftwares.MaterialDesignIcons.Maui;

/// <summary>
/// Runtime icon resolver backed by bundled metadata generated from Google's codepoints files.
/// </summary>
public static class MaterialIconsMap
{
    private const string MetadataResourceName = "AjroudSoftwares.MaterialDesignIcons.Maui.Resources.Fonts.MaterialIcons.metadata.txt";

    private static readonly IReadOnlyDictionary<string, string[]> SemanticAliases = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["user"] = new[] { "person", "account_circle", "account_box" },
        ["profile"] = new[] { "person", "account_circle", "badge" },
        ["login"] = new[] { "login", "person", "account_circle" },
        ["logout"] = new[] { "logout", "exit_to_app" },
        ["back"] = new[] { "arrow_back", "chevron_left", "navigate_before" },
        ["forward"] = new[] { "arrow_forward", "chevron_right", "navigate_next" },
        ["menu"] = new[] { "menu", "more_vert", "more_horiz" },
        ["settings"] = new[] { "settings", "tune", "manage_accounts" },
        ["search"] = new[] { "search", "manage_search", "travel_explore" },
        ["delete"] = new[] { "delete", "delete_outline", "backspace" },
        ["remove"] = new[] { "remove", "delete", "close" },
        ["close"] = new[] { "close", "cancel", "highlight_off" },
        ["favorite"] = new[] { "favorite", "favorite_border", "star" },
        ["star"] = new[] { "star", "grade", "stars" },
        ["notification"] = new[] { "notifications", "notifications_active", "campaign" },
        ["bell"] = new[] { "notifications", "notifications_active" },
        ["email"] = new[] { "mail", "email", "alternate_email" },
        ["message"] = new[] { "message", "chat", "sms" },
        ["phone"] = new[] { "call", "phone", "contact_phone" },
        ["camera"] = new[] { "photo_camera", "camera_alt", "add_a_photo" },
        ["image"] = new[] { "image", "photo", "photo_library" },
        ["edit"] = new[] { "edit", "edit_note", "draw" },
        ["save"] = new[] { "save", "save_as", "download_done" },
        ["download"] = new[] { "download", "file_download", "download_for_offline" },
        ["upload"] = new[] { "upload", "file_upload", "publish" },
        ["warning"] = new[] { "warning", "error", "report_problem" },
        ["info"] = new[] { "info", "info_outline", "help" },
        ["help"] = new[] { "help", "help_outline", "live_help" },
        ["home"] = new[] { "home", "house", "cottage" },
    };

    private static readonly Lazy<MaterialIconIndex> Index = new(BuildIndex, LazyThreadSafetyMode.ExecutionAndPublication);

    public static IReadOnlyList<MaterialIconInfo> All => Index.Value.All;

    public static IReadOnlyDictionary<string, string> Regular => Index.Value.Regular;

    public static IReadOnlyDictionary<string, string> Outlined => Index.Value.Outlined;

    public static IReadOnlyDictionary<string, string> Rounded => Index.Value.Rounded;

    public static IReadOnlyDictionary<string, string> Sharp => Index.Value.Sharp;

    public static IReadOnlyDictionary<string, IReadOnlyList<MaterialIconInfo>> Categories => Index.Value.Categories;

    public static MaterialIconInfo? Find(string name, MaterialIconVariant variant = MaterialIconVariant.Regular) =>
        Index.Value.Find(name, variant);

    public static IReadOnlyList<MaterialIconInfo> FindAll(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? Array.Empty<MaterialIconInfo>()
            : Index.Value.FindAll(name);

    public static IEnumerable<MaterialIconInfo> Search(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Array.Empty<MaterialIconInfo>();
        }

        var normalizedKeyword = Normalize(keyword);
        return Index.Value.All.Where(icon =>
            icon.Key.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || icon.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || icon.PascalName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
            || Normalize(icon.Name).Contains(normalizedKeyword, StringComparison.Ordinal)
            || Normalize(icon.PascalName).Contains(normalizedKeyword, StringComparison.Ordinal));
    }

    public static IEnumerable<MaterialIconInfo> FindByKeyword(string keyword) => Search(keyword);

    /// <summary>
    /// Returns ranked suggestions for a natural-language or approximate icon query.
    /// Useful for AI agents and search UIs that do not know the exact Google icon name.
    /// </summary>
    public static IReadOnlyList<MaterialIconInfo> SearchTop(
        string query,
        int limit = 10,
        MaterialIconVariant? variant = null)
    {
        if (string.IsNullOrWhiteSpace(query) || limit <= 0)
        {
            return Array.Empty<MaterialIconInfo>();
        }

        var normalizedQuery = Normalize(query);
        if (normalizedQuery.Length == 0)
        {
            return Array.Empty<MaterialIconInfo>();
        }

        var variantName = variant?.ToString();
        var expandedTerms = ExpandQueryTerms(query);

        var results = Index.Value.All
            .Where(icon => !variant.HasValue || icon.Variant == variant.Value)
            .Select(icon => new { Icon = icon, Score = Score(icon, normalizedQuery, expandedTerms, variantName) })
            .Where(entry => entry.Score > 0)
            .OrderByDescending(entry => entry.Score)
            .ThenBy(entry => entry.Icon.Key, StringComparer.Ordinal)
            .Select(entry => entry.Icon)
            .Take(limit)
            .ToArray();

        return results;
    }

    /// <summary>
    /// Alias for SearchTop intended to make AI-agent usage more obvious.
    /// </summary>
    public static IReadOnlyList<MaterialIconInfo> Suggest(
        string query,
        int limit = 10,
        MaterialIconVariant? variant = null) =>
        SearchTop(query, limit, variant);

    public static bool TryResolve(string name, MaterialIconVariant variant, out string glyph)
    {
        var icon = Find(name, variant);
        if (icon is null)
        {
            glyph = string.Empty;
            return false;
        }

        glyph = icon.Glyph;
        return true;
    }

    public static bool TryResolve(string key, out string glyph)
    {
        var resolved = Resolve(key);
        if (resolved is null)
        {
            glyph = string.Empty;
            return false;
        }

        glyph = resolved;
        return true;
    }

    public static string? Resolve(string name, MaterialIconVariant variant) => Find(name, variant)?.Glyph;

    public static string? Resolve(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (TryParseQualifiedKey(key, out var variant, out var name))
        {
            return Resolve(name, variant);
        }

        return Resolve(key, MaterialIconVariant.Regular);
    }

    public static string GetRequired(string name, MaterialIconVariant variant = MaterialIconVariant.Regular) =>
        Resolve(name, variant)
        ?? throw new KeyNotFoundException($"Material icon '{name}' was not found in variant '{variant}'.");

    internal static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var buffer = new char[value.Length];
        var index = 0;
        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                buffer[index++] = char.ToLowerInvariant(character);
            }
        }

        return new string(buffer, 0, index);
    }

    private static bool TryParseQualifiedKey(string key, out MaterialIconVariant variant, out string name)
    {
        var separator = key.IndexOf('.');
        if (separator > 0)
        {
            var prefix = key[..separator];
            if (MaterialIconVariantExtensions.TryParse(prefix, out variant))
            {
                name = key[(separator + 1)..];
                return !string.IsNullOrWhiteSpace(name);
            }
        }

        variant = MaterialIconVariant.Regular;
        name = key;
        return false;
    }

    private static string[] ExpandQueryTerms(string query)
    {
        var parts = query
            .Split(new[] { ' ', '_', '-', '.', '/' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(Normalize)
            .Where(part => part.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (parts.Count == 0)
        {
            return Array.Empty<string>();
        }

        var expanded = new HashSet<string>(parts, StringComparer.Ordinal);
        foreach (var part in parts)
        {
            if (SemanticAliases.TryGetValue(part, out var aliases))
            {
                foreach (var alias in aliases)
                {
                    var normalizedAlias = Normalize(alias);
                    if (normalizedAlias.Length > 0)
                    {
                        expanded.Add(normalizedAlias);
                    }
                }
            }
        }

        return expanded.ToArray();
    }

    private static int Score(
        MaterialIconInfo icon,
        string normalizedQuery,
        IReadOnlyList<string> expandedTerms,
        string? variantName)
    {
        var normalizedName = Normalize(icon.Name);
        var normalizedPascal = Normalize(icon.PascalName);
        var normalizedKey = Normalize(icon.Key);
        var score = 0;

        if (normalizedName == normalizedQuery || normalizedPascal == normalizedQuery)
        {
            score += 1000;
        }

        if (normalizedKey == normalizedQuery)
        {
            score += 950;
        }

        if (normalizedName.StartsWith(normalizedQuery, StringComparison.Ordinal))
        {
            score += 240;
        }

        if (normalizedPascal.StartsWith(normalizedQuery, StringComparison.Ordinal))
        {
            score += 220;
        }

        if (normalizedName.Contains(normalizedQuery, StringComparison.Ordinal))
        {
            score += 160;
        }

        if (normalizedPascal.Contains(normalizedQuery, StringComparison.Ordinal))
        {
            score += 140;
        }

        if (!string.IsNullOrWhiteSpace(variantName)
            && icon.Variant.ToString().Equals(variantName, StringComparison.OrdinalIgnoreCase))
        {
            score += 40;
        }

        foreach (var term in expandedTerms)
        {
            if (term == normalizedName || term == normalizedPascal)
            {
                score += 120;
                continue;
            }

            if (normalizedName.Contains(term, StringComparison.Ordinal))
            {
                score += 36;
            }

            if (normalizedPascal.Contains(term, StringComparison.Ordinal))
            {
                score += 32;
            }
        }

        return score;
    }

    private static MaterialIconIndex BuildIndex()
    {
        using var stream = typeof(MaterialIconsMap).Assembly.GetManifestResourceStream(MetadataResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Embedded icon metadata resource '{MetadataResourceName}' was not found.");
        }

        using var reader = new StreamReader(stream);

        var all = new List<MaterialIconInfo>();
        var byVariantName = new Dictionary<MaterialIconVariant, Dictionary<string, MaterialIconInfo>>();
        var byName = new Dictionary<string, List<MaterialIconInfo>>(StringComparer.Ordinal);
        var byVariantKey = new Dictionary<MaterialIconVariant, Dictionary<string, string>>();
        var categories = new Dictionary<string, List<MaterialIconInfo>>(StringComparer.OrdinalIgnoreCase);

        foreach (MaterialIconVariant variant in Enum.GetValues(typeof(MaterialIconVariant)))
        {
            byVariantName[variant] = new Dictionary<string, MaterialIconInfo>(StringComparer.Ordinal);
            byVariantKey[variant] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var parts = line.Split('\t');
            if (parts.Length != 4 || !MaterialIconVariantExtensions.TryParse(parts[0], out var variant))
            {
                continue;
            }

            var snakeName = parts[1];
            var pascalName = parts[2];
            var glyph = char.ConvertFromUtf32(int.Parse(parts[3], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            var icon = new MaterialIconInfo(variant, snakeName, pascalName, glyph);

            all.Add(icon);

            var normalizedSnake = Normalize(snakeName);
            var normalizedPascal = Normalize(pascalName);
            byVariantName[variant][normalizedSnake] = icon;
            byVariantName[variant][normalizedPascal] = icon;
            byVariantKey[variant][icon.Key] = icon.Glyph;

            AddNameIndex(byName, normalizedSnake, icon);
            AddNameIndex(byName, normalizedPascal, icon);

            var category = snakeName.Split('_', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "misc";
            if (!categories.TryGetValue(category, out var categoryIcons))
            {
                categoryIcons = new List<MaterialIconInfo>();
                categories[category] = categoryIcons;
            }

            categoryIcons.Add(icon);
        }

        return new MaterialIconIndex(
            all.OrderBy(icon => icon.Key, StringComparer.Ordinal).ToArray(),
            byVariantName,
            byName,
            byVariantKey,
            categories.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<MaterialIconInfo>)pair.Value.OrderBy(icon => icon.Key, StringComparer.Ordinal).ToArray(),
                StringComparer.OrdinalIgnoreCase));
    }

    private static void AddNameIndex(Dictionary<string, List<MaterialIconInfo>> byName, string key, MaterialIconInfo icon)
    {
        if (!byName.TryGetValue(key, out var icons))
        {
            icons = new List<MaterialIconInfo>();
            byName[key] = icons;
        }

        if (!icons.Any(existing => existing.Key.Equals(icon.Key, StringComparison.Ordinal)))
        {
            icons.Add(icon);
        }
    }

    private sealed class MaterialIconIndex
    {
        private readonly IReadOnlyDictionary<MaterialIconVariant, Dictionary<string, MaterialIconInfo>> _byVariantName;
        private readonly IReadOnlyDictionary<string, List<MaterialIconInfo>> _byName;

        public MaterialIconIndex(
            IReadOnlyList<MaterialIconInfo> all,
            IReadOnlyDictionary<MaterialIconVariant, Dictionary<string, MaterialIconInfo>> byVariantName,
            IReadOnlyDictionary<string, List<MaterialIconInfo>> byName,
            IReadOnlyDictionary<MaterialIconVariant, Dictionary<string, string>> byVariantKey,
            IReadOnlyDictionary<string, IReadOnlyList<MaterialIconInfo>> categories)
        {
            All = all;
            _byVariantName = byVariantName;
            _byName = byName;
            Regular = byVariantKey[MaterialIconVariant.Regular];
            Outlined = byVariantKey[MaterialIconVariant.Outlined];
            Rounded = byVariantKey[MaterialIconVariant.Rounded];
            Sharp = byVariantKey[MaterialIconVariant.Sharp];
            Categories = categories;
        }

        public IReadOnlyList<MaterialIconInfo> All { get; }

        public IReadOnlyDictionary<string, string> Regular { get; }

        public IReadOnlyDictionary<string, string> Outlined { get; }

        public IReadOnlyDictionary<string, string> Rounded { get; }

        public IReadOnlyDictionary<string, string> Sharp { get; }

        public IReadOnlyDictionary<string, IReadOnlyList<MaterialIconInfo>> Categories { get; }

        public MaterialIconInfo? Find(string name, MaterialIconVariant variant)
        {
            var normalized = Normalize(name);
            if (_byVariantName[variant].TryGetValue(normalized, out var icon))
            {
                return icon;
            }

            return null;
        }

        public IReadOnlyList<MaterialIconInfo> FindAll(string name)
        {
            var normalized = Normalize(name);
            if (_byName.TryGetValue(normalized, out var icons))
            {
                return icons.OrderBy(icon => icon.Key, StringComparer.Ordinal).ToArray();
            }

            return Array.Empty<MaterialIconInfo>();
        }
    }
}