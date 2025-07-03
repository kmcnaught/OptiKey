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

## TODOs and Future Work

### Contrast Handling for Highlight Text
The ContrastEnsureConverter was created but should be applied to {highlight} text colors in FormattedTextHelper, not button colors. Button colors can remain as user-configured, but highlighted text needs minimum 3:1 contrast against white backgrounds.

**Implementation needed**: 
- Apply ContrastEnsureConverter to the HighlightColor binding in XAML files that use FormattedTextHelper.FormattedText with {highlight} markup
- This ensures highlighted text is always visible against white text label backgrounds