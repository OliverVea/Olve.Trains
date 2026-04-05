# Layout System — Full Reference

## Layout XML Files

Location: `src/Olve.Trains/resources/layouts/*.xml`

### Format

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Box id="Overlay" Style="StationPanelOverlayStyle" Interactive="true">
    <Box id="Panel" Style="StationPanelStyle" Interactive="true">
        <Text id="Header" Content='"Station"' Style="StationPanelHeaderStyle" />
        <Text id="StationName" Content='""' Style="StationPanelInfoTextStyle" />
        <Box id="InventoryContainer" Style="StationPanelContainerStyle" />
    </Box>
</Box>
```

### Attribute Rules

- **`id`** — Element name. Becomes a named field on the generated record. Without `id`, auto-named `TypeName_N` (not included in the record).
- **`Style`** — References a `StyleKey` name from `Styles.cs`.
- **All other attributes** — Emitted verbatim as C# property assignments. The value must be a valid C# expression:
  - Strings: `Content='"Hello"'` (single-quoted XML containing double-quoted C# string literal)
  - Enums: `Justify="Justify.Center"`
  - Numbers: `Width="200"`, `Gap="8"`
  - Booleans: `Vertical="true"`
- The element tag name determines the C# type: `<Box>`, `<Text>`, `<Image>`, etc.

### Row Templates

For repeating content (inventory lists, train rows), create a separate XML file for the row:

```xml
<!-- InventoryRow.xml -->
<?xml version="1.0" encoding="utf-8" ?>
<Box id="Row" Style="InventoryRowStyle">
    <Text id="CargoName" Content='""' Style="InventoryRowTextStyle" />
    <Text id="AmountText" Content='""' Style="InventoryRowAmountStyle" />
</Box>
```

These are instantiated dynamically in the panel service via `Layouts.BuildInventoryRow()`.

## Asset Pipeline Codegen

**Input:** `src/Olve.Trains/resources/layouts/*.xml`
**Output:** `src/Olve.Trains/assets/Layouts/<FileName>.cs` (namespace `Olve.Generated.Layouts`)
**Template:** `src/Olve.Trains.AssetPipeline/templates/LayoutClass.scriban`
**Processor:** `src/Olve.Trains.AssetPipeline/Layouts/ProcessLayouts.cs`

Run: `cd src/Olve.Trains.AssetPipeline && dotnet run`

### Generated Code Structure

For each XML file, the pipeline generates:

1. A `record` with named fields for each element that has an `id`, plus an `Elements` list of all descendants.
2. A `Build<FileName>()` static method that constructs the full element tree.

```csharp
// Generated from StationInfoPanel.xml
public static partial class Layouts
{
    public record StationInfoPanel(
        Text Header, Text StationName, Text IndustriesHeader,
        Box InventoryContainer, Box Panel, Box Overlay,
        IReadOnlyList<GuiElement> Elements);

    public static StationInfoPanel BuildStationInfoPanel()
    {
        var Header = new Text()
        {
            Name = "Header",
            Id = Id.FromName<GuiElement>("StationInfoPanel/Text/Header"),
            StyleKey = new StyleKey("StationPanelHeaderStyle"),
            Content = "Station",
        };
        // ... more elements ...
        return new StationInfoPanel(Header, ..., [Overlay, Panel, ...]);
    }
}
```

**Element ID format:** `LayoutName/ElementType/ElementId` — deterministic via `Id.FromName<GuiElement>(...)`.

## Flexbox Layout System

Layout is computed by `GuiLayoutService` in `Olve.Engine3D.GUI.Layout`.

### Coordinate System

- **Dp** (density-independent pixels) — Used in layout definitions and element properties. Scale-independent.
- **Px** (pixels) — Screen pixels. Used for rendering.
- **LayoutContext** — Converts between Dp and Px based on screen size and UIScale.

```
Window 1920x1080, UIScale 1.0 → 1 Dp = 1 Px
Window 3840x2160, UIScale 1.0 → 1 Dp = 2 Px (retina/HiDPI)
```

### Layout Algorithm

1. Elements registered → `GuiNodeService.OnNodeAdded` → `GuiLayoutService` creates `LayoutData`
2. Property changes set dirty flag
3. On dirty, compute layout:
   - Root nodes partitioned into screen-anchored vs relative-anchored
   - Screen anchors computed first, then relative anchors
   - Recursive flexbox algorithm: parent → children
   - Output: `BoxPosition` (position + size in Px) per node

### Key Layout Types

- `LayoutBox` — Layout input: size (preferred W/H, weight, aspect ratio), gap, justify, align, margin, padding, border
- `BoxPosition` — Layout output: position (Px) + size (Px) in screen space
- `LayoutContext` — Dp/Px conversion based on screen size

### Box Layout Rules

- **Vertical=false (row):** Children laid out left-to-right. Justify controls horizontal distribution. Align controls vertical alignment.
- **Vertical=true (column):** Children laid out top-to-bottom. Justify controls vertical distribution. Align controls horizontal alignment.
- **Weight:** Elements with `Weight` fill remaining space proportionally after fixed-size elements are placed.
- **Gap:** Uniform spacing between children.
- **Padding:** Inner spacing between box border and children.
- **Margin:** Outer spacing around the box.

### Layout Update Flow

1. `GuiLayoutContextUpdater` — Updates Dp/Px conversion on window resize
2. `GuiLayoutUpdateService` — Queues element adds/removes, calls `GuiLayoutService.ComputeLayout()`
3. `GuiLayoutService` — Computes positions for all nodes, outputs `BoxPosition` per node
