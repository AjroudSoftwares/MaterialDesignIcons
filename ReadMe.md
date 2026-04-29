# Material Design Icons for .NET MAUI

Use Google Material icon fonts in .NET MAUI with simple runtime lookup, multi-variant support, and a bundled metadata index generated from the official codepoints files.

## Install

```bash
dotnet add package AjroudSoftwares.MaterialDesignIcons.Maui
```

## What This Package Provides

- Four Google Material variants: Regular, Outlined, Rounded, Sharp
- Runtime lookup by icon name without hardcoding every glyph in C# source
- Bundled metadata generated from Google's official `.codepoints` files
- C# helpers via `MaterialIcons` and `MaterialIconsMap`
- Search and discovery support via `Search`, `Find`, `FindAll`, and `Categories`
- XAML markup extensions for dynamic icon and font resolution on supported target frameworks

## Register Fonts in MAUI

```csharp
builder.ConfigureFonts(fonts =>
{
    fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIconsRegular");
    fonts.AddFont("MaterialIconsOutlined-Regular.ttf", "MaterialIconsOutlined");
    fonts.AddFont("MaterialIconsRound-Regular.ttf", "MaterialIconsRounded");
    fonts.AddFont("MaterialIconsSharp-Regular.ttf", "MaterialIconsSharp");
});
```

## Usage in C#

Resolve an icon directly by its Google name. The lookup is forgiving, so names like `home`, `Home`, `arrow_back`, and `ArrowBack` work.

```csharp
using AjroudSoftwares.MaterialDesignIcons.Maui;

var regularHome = MaterialIcons.GetRequired("home");
var outlinedHome = MaterialIcons.GetRequired("home", MaterialIconVariant.Outlined);

var roundedArrowBack = MaterialIconsMap.Resolve("arrow_back", MaterialIconVariant.Rounded);
var sharpSearch = MaterialIconsMap.Resolve("Sharp.Search");

var regularFont = MaterialIcons.FontFamily();
var roundedFont = MaterialIcons.FontFamily(MaterialIconVariant.Rounded);
```

## AI Quick Reference

For AI agents, prefer this order:

1. `MaterialIcons.GetRequired("home")` when you already know the exact icon name.
2. `MaterialIcons.Suggest("user profile")` when you know the intent but not the exact Google icon name.
3. `MaterialIcons.FontFamily(MaterialIconVariant.Outlined)` to get the matching MAUI font alias.

```csharp
using AjroudSoftwares.MaterialDesignIcons.Maui;

var suggestions = MaterialIcons.Suggest("user profile", limit: 5);
var best = suggestions.FirstOrDefault();

var glyph = best?.Glyph ?? MaterialIcons.GetRequired("person");
var font = best?.FontFamily ?? MaterialIcons.FontFamily();
```

If you already know the variant, pass it into suggestions to reduce ambiguity:

```csharp
var outlinedSuggestions = MaterialIcons.Suggest("back", limit: 5, variant: MaterialIconVariant.Outlined);
```

### Runtime Lookup API

```csharp
using AjroudSoftwares.MaterialDesignIcons.Maui;

var glyph = MaterialIcons.Get("settings");
var required = MaterialIcons.GetRequired("person", MaterialIconVariant.Outlined);
var icon = MaterialIcons.Find("arrow_back", MaterialIconVariant.Rounded);
var allHomes = MaterialIcons.FindAll("home");

if (MaterialIconsMap.TryResolve("favorite", MaterialIconVariant.Sharp, out var favoriteGlyph))
{
    // use favoriteGlyph
}

if (MaterialIconsMap.TryResolve("Outlined.Notifications", out var notificationsGlyph))
{
    // use notificationsGlyph
}
```

### Using with FontImageSource

```csharp
using AjroudSoftwares.MaterialDesignIcons.Maui;
using Microsoft.Maui.Controls;

var icon = MaterialIcons.Find("home", MaterialIconVariant.Outlined);

var imageSource = new FontImageSource
{
    Glyph = icon?.Glyph,
    FontFamily = icon?.FontFamily,
    Size = 24,
};
```

## Usage in XAML

For all target frameworks, you can resolve the glyph in C# and bind it in XAML like normal text.

```xml
<Label Text="{Binding HomeGlyph}"
       FontFamily="MaterialIconsRegular"
       FontSize="28" />
```

```csharp
using AjroudSoftwares.MaterialDesignIcons.Maui;

public string HomeGlyph => MaterialIcons.GetRequired("home");
public string RoundedArrowGlyph => MaterialIcons.GetRequired("arrow_back", MaterialIconVariant.Rounded);
```

### XAML Markup Extensions

On `net9.0+` and `net10.0+`, the package also includes markup extensions for simpler XAML usage:

```xml
xmlns:mdi="clr-namespace:AjroudSoftwares.MaterialDesignIcons.Maui"

<Label Text="{mdi:Icon Name=home, Variant=Outlined}"
       FontFamily="{mdi:IconFont Variant=Outlined}"
       FontSize="28" />
```

`IconExtension` resolves the glyph string.

`IconFontExtension` resolves the matching MAUI font alias.

## Search and Discovery

```csharp
using AjroudSoftwares.MaterialDesignIcons.Maui;

var arrows = MaterialIconsMap.Search("arrow");
var accounts = MaterialIconsMap.Categories["account"];
var homeMatches = MaterialIcons.FindAll("home");
var userSuggestions = MaterialIconsMap.Suggest("user profile", limit: 5);
```

Each search result is a `MaterialIconInfo` instance with:

- `Variant`
- `Name`
- `PascalName`
- `Glyph`
- `Key`
- `FontFamily`

## Migration from Older Versions

Older versions exposed generated constants such as `MaterialIcons.Regular.Home` and XAML usage through `x:Static`.

That API has been replaced with metadata-backed runtime lookup:

- Old: `MaterialIcons.Regular.Home`
- New: `MaterialIcons.GetRequired("home")`

- Old: `MaterialIcons.Rounded.ArrowBack`
- New: `MaterialIcons.GetRequired("arrow_back", MaterialIconVariant.Rounded)`

- Old: `MaterialIconsMap.Resolve("Rounded.ArrowBack")`
- New: `MaterialIconsMap.Resolve("Rounded.ArrowBack")` or `MaterialIconsMap.Resolve("arrow_back", MaterialIconVariant.Rounded)`

This keeps the library smaller and simpler while using Google's icon names as the main source of truth.

## Notes

- Icon names come from Google's Material icon codepoints metadata, not from parsing names out of the font files themselves.
- The package currently targets the four Google Material variants only: Regular, Outlined, Rounded, and Sharp.
- Markup extensions are currently compiled for `net9.0+` and `net10.0+`.
- If you prefer explicit control, you can always use `MaterialIcons.GetRequired(...)` in C# and bind the result into XAML.

## License

MIT. See `License.txt`.
