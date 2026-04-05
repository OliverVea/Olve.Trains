# GUI Elements — Full Reference

All element types in `Olve.Engine3D.GUI.Elements`.

## GuiElement (base class)

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Id<GuiElement>` | Unique identifier |
| `Name` | `string` | Display name |
| `Children` | `GuiElement[]` | Child elements |
| `StyleKey` | `StyleKey?` | References a named style |
| `Interactive` | `bool` | Enables mouse input (hover, click) |
| `InheritParentState` | `bool` | Inherits parent's Focused/Pressed states |

## Box

Flexbox-like container. The primary layout element.

### Size

| Property | Type | Description |
|----------|------|-------------|
| `Width` | `int?` | Fixed width in Dp |
| `Height` | `int?` | Fixed height in Dp |
| `Weight` | `float?` | Flex weight for filling remaining space |
| `AspectRatio` | `int?` | Width/height ratio constraint |
| `FitMode` | `FitMode?` | How content scales within bounds |

### Spacing

| Property | Type | Description |
|----------|------|-------------|
| `Gap` | `float` | Space between children (Dp) |
| `Margin` | `float` | All-sides margin (Dp) |
| `MarginLeft/Right/Top/Bottom` | `float?` | Per-side margin overrides |
| `Padding` | `float` | All-sides padding (Dp) |
| `PaddingLeft/Right/Top/Bottom` | `float?` | Per-side padding overrides |

### Layout

| Property | Type | Values |
|----------|------|--------|
| `Justify` | `Justify` | `Start`, `Center`, `End`, `SpaceBetween`, `SpaceAround`, `SpaceEvenly` |
| `Align` | `Align` | `Start`, `Center`, `End`, `Stretch` |
| `Vertical` | `bool` | `true` = column, `false` = row (default) |

### Visual

| Property | Type | Description |
|----------|------|-------------|
| `BackgroundColor` | `RGBA?` | Fill color |
| `BorderWidth` | `float` | Border thickness |
| `BorderColor` | `RGBA?` | Border color |
| `BorderRadius` | `float` | Corner rounding |
| `BorderLeftWidth/RightWidth/TopWidth/BottomWidth` | `float?` | Per-side border width |

## Text

Auto-sized based on content and font metrics. Size computed by `GuiTextUpdateService`.

| Property | Type | Description |
|----------|------|-------------|
| `Content` | `string` | Text to display |
| `Font` | `string?` | Font name |
| `FontSize` | `float` | Size in Dp (default 16) |
| `FontWeight` | `float` | Weight/boldness |
| `Color` | `RGBA?` | Text color |
| `BackgroundColor` | `RGBA?` | Background fill |
| `Align` | `Align?` | Text alignment |
| `Width` | `float?` | Fixed width (otherwise auto-sized) |
| `Weight` | `float?` | Flex weight for filling remaining space |

## Image

| Property | Type | Description |
|----------|------|-------------|
| `Texture` | `AssetPath?` | Texture asset path |
| `Tint` | `RGBA?` | Color tint |
| `Alpha` | `float` | Opacity (0-1) |
| `AspectRatio` | `int?` | Width/height ratio |
| `FitMode` | `FitMode?` | How image scales |
| `SourceRect` | `Rectangle<float>?` | Sub-region for atlas textures |
| `TextureSize` | `Vector2D<int>?` | Full texture dimensions (for UV calc) |

## Interactive Elements

Each has a dedicated engine service that handles its behavior:

- **Checkbox** (`GuiCheckboxService`) — Boolean toggle. Visual feedback on check/uncheck.
- **Slider** (`GuiSliderService`) — Range input with draggable thumb.
- **RadioButton** / **RadioButtonGroup** (`GuiRadioButtonService`) — Single-select from group.
- **Dropdown** (`GuiDropdownService`) — Collapsible option list with overlay.
- **Divider** — Visual separator (no dedicated service).
