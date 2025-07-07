# Claude Code Assistant Instructions

This file contains instructions and notes for Claude when working on this project.

## Adding New Files to the Project

When creating new C# files in this project, they must be manually added to the `JuliusSweetland.OptiKey.Core.csproj` file.

### Steps:
1. Create the new .cs file in the appropriate directory under `src/JuliusSweetland.OptiKey.Core/`
2. Open `src/JuliusSweetland.OptiKey.Core/JuliusSweetland.OptiKey.Core.csproj`
3. Find the appropriate section based on the file type and location:
   - **ValueConverters**: Look for `<Compile Include="UI\ValueConverters\*.cs" />` entries
   - **Controls**: Look for `<Compile Include="UI\Controls\*.cs" />` entries
   - **ViewModels**: Look for `<Compile Include="UI\ViewModels\*.cs" />` entries
   - etc.
4. Add a new `<Compile Include="..." />` entry in **alphabetical order** within the appropriate section
5. Use backslashes (`\`) for the path separators (Windows-style paths)

### Example:
```xml
<Compile Include="UI\ValueConverters\ColourNameToBrush.cs" />
<Compile Include="UI\ValueConverters\ContrastEnsureConverter.cs" />
<Compile Include="UI\ValueConverters\EnabledIfNotOverridden.cs" />
```

### Important Notes:
- Files are organized alphabetically within their respective categories
- The project uses Windows-style paths with backslashes
- All C# files must be explicitly listed - the project doesn't use wildcard includes
- If you forget to add a file, it will not be compiled and you'll get build errors

## Project Structure Notes
- Main source code: `src/JuliusSweetland.OptiKey.Core/`
- ValueConverters: `UI/ValueConverters/`
- Controls: `UI/Controls/` 
- Views: `UI/Views/`
- Resources: `Properties/`
- Exhibit specific view (the main focus of this work):  `UI/Views/Exhibit`

## Creating New App Settings

To add new application settings that users can configure:

### Steps:
1. Open `src/JuliusSweetland.OptiKey.Core/Properties/Settings.cs`
2. Find an appropriate location (group similar settings together)
3. Add the new setting using this pattern:

```csharp
[global::System.Configuration.UserScopedSettingAttribute()]
[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
[global::System.Configuration.DefaultSettingValueAttribute("DefaultValue")]
[global::System.Configuration.SettingsManageabilityAttribute(global::System.Configuration.SettingsManageability.Roaming)]
public virtual string SettingName
{
    get
    {
        return ((string)(this["SettingName"]));
    }
    set
    {
        this["SettingName"] = value;
    }
}
```

### Setting Types:
- **String settings**: Use `string` type and provide default as string value
- **Boolean settings**: Use `bool` type and provide "True" or "False" as default
- **Numeric settings**: Use appropriate type (`int`, `double`, etc.) and provide numeric default

### Usage in XAML:
```xml
<!-- Add namespace -->
xmlns:properties="clr-namespace:JuliusSweetland.OptiKey.Properties"

<!-- Use in binding -->
Property="{Binding Source={x:Static properties:Settings.Default}, Path=YourSettingName}"

<!-- For colors, often need converter -->
Property="{Binding Source={x:Static properties:Settings.Default}, Path=YourColorSetting, Converter={StaticResource ColourNameToBrush}}"
```

### Example:
- Setting name: `ExhibitButtonIconColor`
- Type: `string` (for color names like "White", "Black", etc.)
- Default: `"White"`
- Used in: Button icon foreground color

## Logging System

### ExhibitStateLogger
A logging service that tracks state transitions in the Exhibit interface for analytics and debugging.

**Features:**
- Logs OnboardState, DemoState, and TempState changes with timestamps
- Writes to `C:\EyeMineLogs\exhibit_state_log.txt`
- Silent failure - logging errors don't disrupt the application
- Thread-safe with lock mechanism
- Session start/end tracking

**Usage:**
```csharp
using JuliusSweetland.OptiKey.Services;

// Log state changes
ExhibitStateLogger.LogDemoStateChange(oldState, newState);
ExhibitStateLogger.LogOnboardStateChange(oldState, newState);
ExhibitStateLogger.LogStateChange("CustomState", oldValue, newValue);

// Log session events  
ExhibitStateLogger.LogSessionStart();
ExhibitStateLogger.LogSessionEnd();

// Log state hits
ExhibitStateLogger.LogStateHit("DemoState", "RUNNING");
```

**Integration Points:**
- `OnboardingViewModel.SetState()` methods automatically log all state transitions
- Session start logged in `OnboardingViewModel` constructor
- Can be extended to log additional state events as needed

## TODOs and Future Work

### Contrast Handling for Highlight Text
The ContrastEnsureConverter was created but should be applied to {highlight} text colors in FormattedTextHelper, not button colors. Button colors can remain as user-configured, but highlighted text needs minimum 3:1 contrast against white backgrounds.

**Implementation needed**: 
- Apply ContrastEnsureConverter to the HighlightColor binding in XAML files that use FormattedTextHelper.FormattedText with {highlight} markup
- This ensures highlighted text is always visible against white text label backgrounds

## Settings
### Icon text colour
Add a setting to allow user to change the colour of the icon text (e.g. right arrow) for buttons (typically they would be black or white depending on button colours). The default should be white. Once you have done this, write instructions in Claude.md about how to create new app settings. 

### Icon text visibility
Add a bool in the settings for whether to show the text icon on buttons. default true. 

## Logging
Create a logging system to keep track of how many times we hit each of the states in OnboardState and each time we hit each DemoState. This should be written to a logfile in C:\EyeMineLogs and should be robust to whether or not the file exists or can be written too (i.e. this logging must fail silently)
