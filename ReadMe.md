# 🎨 MaterialDesignIcons for .NET MAUI

Effortlessly integrate Material Design Icons into your .NET MAUI apps with font and C# mappings.

---

## 🚀 Quick Start

### 1️⃣ Install the Package & Register the Font

Install via NuGet:

```bash
dotnet add package AjroudSoftwares.MaterialDesignIcons
```

The following files are automatically copied to your project:

- `Resources/Fonts/MaterialDesignIcons.ttf` — the icon font
- `Resources/Fonts/MaterialIcons.cs` — C# constants for icon glyphs

Register the font in `MauiProgram.cs`:

```csharp
builder.ConfigureFonts(fonts =>
{
    fonts.AddFont("MaterialDesignIcons.ttf", "MaterialIcons");
});
```

---

### 2️⃣ Use the Icons

#### 🧩 XAML Example

Add the namespace:

```xml
xmlns:mdi="clr-namespace:MaterialDesignIcons"
```

Use the icon:

```xml
<Label Text="{x:Static mdi:MaterialIcons.Account}"
       FontFamily="MaterialIcons"
       FontSize="32" />

<!-- Or as a FontImageSource -->
<FontImageSource x:Key="IconAccount"
                 Glyph="{x:Static mdi:MaterialIcons.Account}"
                 FontFamily="MaterialIcons"
                 Size="32" />
```

#### 💻 C# Example

```csharp
using MaterialDesignIcons;

var iconLabel = new Label
{
    Text = MaterialIcons.Account,
    FontFamily = "MaterialIcons",
    FontSize = 32
};
```

---

## 📦 Package Info

- Author: Aymen Ajroud
- Company: AjroudSoftwares
- License: MIT
- Version: 1.0.0

---
## 📄 License

This project is licensed under the [MIT License](./LICENSE).  
Feel free to use, modify, and distribute it with proper attribution.
___
✨ Enjoy beautiful Material Design Icons in your .NET MAUI app!
