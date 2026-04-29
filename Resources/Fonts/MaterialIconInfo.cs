#nullable enable

namespace AjroudSoftwares.MaterialDesignIcons.Maui;

public sealed class MaterialIconInfo
{
    public MaterialIconInfo(MaterialIconVariant variant, string name, string pascalName, string glyph)
    {
        Variant = variant;
        Name = name;
        PascalName = pascalName;
        Glyph = glyph;
    }

    public MaterialIconVariant Variant { get; }

    public string Name { get; }

    public string PascalName { get; }

    public string Glyph { get; }

    public string Key => $"{Variant}.{PascalName}";

    public string FontFamily => Variant.ToFontFamily();

    public override string ToString() => Key;
}