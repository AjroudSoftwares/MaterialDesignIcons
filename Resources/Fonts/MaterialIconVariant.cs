#nullable enable

using System;

namespace AjroudSoftwares.MaterialDesignIcons.Maui;

public enum MaterialIconVariant
{
    Regular,
    Outlined,
    Rounded,
    Sharp,
}

public static class MaterialIconVariantExtensions
{
    public static string ToFontFamily(this MaterialIconVariant variant) =>
        variant switch
        {
            MaterialIconVariant.Regular => "MaterialIconsRegular",
            MaterialIconVariant.Outlined => "MaterialIconsOutlined",
            MaterialIconVariant.Rounded => "MaterialIconsRounded",
            MaterialIconVariant.Sharp => "MaterialIconsSharp",
            _ => "MaterialIconsRegular",
        };

    public static string ToFontFile(this MaterialIconVariant variant) =>
        variant switch
        {
            MaterialIconVariant.Regular => "MaterialIcons-Regular.ttf",
            MaterialIconVariant.Outlined => "MaterialIconsOutlined-Regular.ttf",
            MaterialIconVariant.Rounded => "MaterialIconsRound-Regular.ttf",
            MaterialIconVariant.Sharp => "MaterialIconsSharp-Regular.ttf",
            _ => "MaterialIcons-Regular.ttf",
        };

    public static bool TryParse(string? value, out MaterialIconVariant variant)
    {
        if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, ignoreCase: true, out variant))
        {
            return true;
        }

        variant = MaterialIconVariant.Regular;
        return false;
    }
}