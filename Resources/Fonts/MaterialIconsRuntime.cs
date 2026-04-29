#nullable enable

using System.Collections.Generic;

namespace AjroudSoftwares.MaterialDesignIcons.Maui;

public static class MaterialIcons
{
    public static string? Get(string name, MaterialIconVariant variant = MaterialIconVariant.Regular) =>
        MaterialIconsMap.Resolve(name, variant);

    public static string GetRequired(string name, MaterialIconVariant variant = MaterialIconVariant.Regular) =>
        MaterialIconsMap.GetRequired(name, variant);

    public static MaterialIconInfo? Find(string name, MaterialIconVariant variant = MaterialIconVariant.Regular) =>
        MaterialIconsMap.Find(name, variant);

    public static IReadOnlyList<MaterialIconInfo> FindAll(string name) =>
        MaterialIconsMap.FindAll(name);

    public static IReadOnlyList<MaterialIconInfo> Suggest(
        string query,
        int limit = 10,
        MaterialIconVariant? variant = null) =>
        MaterialIconsMap.Suggest(query, limit, variant);

    public static string FontFamily(MaterialIconVariant variant = MaterialIconVariant.Regular) =>
        variant.ToFontFamily();
}