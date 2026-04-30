#nullable enable

namespace AjroudSoftwares.MaterialDesignIcons.Maui;

public sealed class MaterialIconInfo(MaterialIconVariant variant, string name, string pascalName, string glyph)
{
    public MaterialIconVariant Variant { get; } = variant;

    public string Name { get; } = name;

    public string PascalName { get; } = pascalName;

    public string Glyph { get; } = glyph;

    public string Key => $"{Variant}.{PascalName}";

    public string FontFamily => Variant.ToFontFamily();

    public override string ToString() => Key;
}