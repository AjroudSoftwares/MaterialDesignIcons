# Material Design Icons for .NET MAUI

Use Material Design icon fonts in .NET MAUI with strongly-typed C# constants, multi-variant support, and a generated search index.

## Install

```bash
dotnet add package AjroudSoftwares.MaterialDesignIcons.Maui
```

## What This Package Provides

- Four icon variants: Regular, Outlined, Rounded, Sharp
- Generated C# constants:
    - `MaterialIcons.Regular.*`
    - `MaterialIcons.Outlined.*`
    - `MaterialIcons.Rounded.*`
    - `MaterialIcons.Sharp.*`
- Runtime discovery index via `MaterialIconsMap`:
    - `Search(string keyword)`
    - `Resolve(string key)`
    - `Categories`

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

```csharp
using AjroudSoftwares.MaterialDesignIcons.Maui;

var regularHome = MaterialIcons.Regular.Home;
var outlinedHome = MaterialIcons.Outlined.Home;

var key = "Rounded.ArrowBack";
var glyph = MaterialIconsMap.Resolve(key);
```

## Usage in XAML

```xml
xmlns:mdi="clr-namespace:AjroudSoftwares.MaterialDesignIcons.Maui"

<Label Text="{x:Static mdi:MaterialIcons+Regular.Home}"
             FontFamily="MaterialIconsRegular"
             FontSize="28" />
```

## AI/Copilot-Friendly Discovery

```csharp
var arrows = MaterialIconsMap.Search("arrow");
var allAccountIcons = MaterialIconsMap.Categories["account"];
```

## License

MIT. See `License.txt`.
