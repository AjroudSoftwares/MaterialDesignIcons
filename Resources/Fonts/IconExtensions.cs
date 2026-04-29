#if NET9_0_OR_GREATER

#nullable enable

using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

namespace AjroudSoftwares.MaterialDesignIcons.Maui;

[ContentProperty(nameof(Name))]
public sealed class IconExtension : IMarkupExtension
{
    public string? Name { get; set; }

    public MaterialIconVariant Variant { get; set; } = MaterialIconVariant.Regular;

    public string Fallback { get; set; } = string.Empty;

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return Fallback;
        }

        return MaterialIconsMap.Resolve(Name, Variant) ?? Fallback;
    }
}

public sealed class IconFontExtension : IMarkupExtension
{
    public MaterialIconVariant Variant { get; set; } = MaterialIconVariant.Regular;

    public object ProvideValue(IServiceProvider serviceProvider) => Variant.ToFontFamily();
}

#endif