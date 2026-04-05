---
name: gui
description: Reference for the GUI system — layout XML, generated code, elements, styles, panels, anchors, input handling, and the workflow for creating new UI. Use when adding UI panels, modifying layouts, creating styles, or understanding GUI architecture.
user-invocable: false
---

# GUI System

Tree-based element hierarchy. XML layout files are processed by the asset pipeline into generated C# builder classes. Scene services handle layout, rendering, input, styling, and animation.

## Elements

All types in `Olve.Engine3D.GUI.Elements`. Base class: `GuiElement`.

- **Box** — Flexbox container. Properties: Width, Height, Weight, Gap, Margin, Padding, Justify, Align, Vertical, BackgroundColor, Border (width/color/radius), AspectRatio.
- **Text** — Auto-sized text. Properties: Content, FontSize (default 16), FontWeight, Color.
- **Image** — Texture display. Properties: Texture (AssetPath), Tint, Alpha, AspectRatio.
- **Interactive elements** — Checkbox, Slider, RadioButton, Dropdown, Divider (each has a dedicated engine service).

Common flags on all elements:
- `Interactive = true` — Enables mouse input (hover, click).
- `InheritParentState = true` — Child inherits parent's hover/pressed states (e.g. button text highlights with button).
- `StyleKey` — References a named style from `Styles.cs`.

## Layout XML

Location: `src/Olve.Trains/resources/layouts/*.xml`. One file per panel/screen.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Box id="Panel" Style="StationPanelStyle" Interactive="true">
    <Text id="Header" Content='"Station"' Style="StationPanelHeaderStyle" />
    <Box id="ContentArea" Style="StationPanelContainerStyle" />
</Box>
```

- `id` — Becomes a named field on the generated record. Without `id`, auto-named `TypeName_N`.
- `Style` — References a `StyleKey` name from `Styles.cs`.
- All other attributes emitted verbatim as C# expressions: strings `Content='"Hello"'`, enums `Justify="Justify.Center"`, numbers `Width="200"`, booleans `Vertical="true"`.

**After modifying layouts**, run the asset pipeline: `cd src/Olve.Trains.AssetPipeline && dotnet run`

Generated output: `src/Olve.Trains/assets/Layouts/<FileName>.cs` (namespace `Olve.Generated.Layouts`). Produces a `record` with named fields and a `Build<FileName>()` method. Element IDs use `Id.FromName<GuiElement>("LayoutName/ElementType/ElementId")`.

## Styles

Location: `src/Olve.Trains/Scenes/GameUI/GUI/Styles.cs`

Styles define visual appearance and state-driven animations via `GuiElementStyling<T>`:

```csharp
public static readonly GuiElementStyling<Box> PanelStyle = new()
{
    StyleKey = new StyleKey(nameof(PanelStyle)),
    StateTransitions = ButtonTransitions,  // Optional animation config
    OnStateChanged = (box, weights) =>
    {
        var focused = weights[GuiNodeState.Focused];
        box.BackgroundColor = (0.2f, 0.22f, 0.21f, 0.9f);
        box.BorderColor = Lerp(DefaultBorder, FocusBorder, focused);
    }
};
```

State flags: `Show`, `Enabled`, `Focused` (hover), `Pressed` (mouse down), `Active` (toggled). The `weights` dict gives 0.0-1.0 animated values per state.

**Conventions:** Prefer shared styles for visual cohesion. Only create panel-specific styles when unique look/behavior is needed. Register all styles in `GameStyleRegistration.RegisterAllStyles()` (in `GameStyleService.cs`).

Constants: `Styles.DefaultBorderRadius` (6), `Styles.DefaultPadding` (2), `Styles.DefaultBorderWidth` (1.5f), `Styles.PanelBackground`, `Styles.DefaultBorder`, `Styles.FocusBorder`, `Styles.DimTextColor`, `Styles.ButtonTransitions`.

## Panels

A panel service is an `ISceneService` that builds a layout, registers it with the GUI, handles input, and cleans up.

**Opening:** Build layout via `Layouts.Build<Name>()`, register an anchor with `guiAnchorService.RegisterAnchor(position, growthDirection, depth)`, then register the element tree with `guiElementService.RegisterElementAndChildren(anchorId, rootElement)`.

**Closing:** `guiElementService.UnregisterElementAndChildren(registrationId)` then `guiAnchorService.UnregisterAnchor(anchorId)`.

**Button clicks:** Subscribe to `guiActivationService.GuiElementActivated`, compare `message.NodeId` against element IDs using `guiElementService.TryGetGuiNodeId()`.

**Dynamic rows:** Use a separate layout XML as a row template. Build instances with `Layouts.BuildRowName()`, set content, then `RegisterElementAndChildren` into a container node. Track registration IDs for cleanup.

**Anchors:** `AnchorPosition` (TopLeft, TopRight, Center, MiddleRight, etc.) + `GrowthDirection` (DownRight, DownLeft, Left, Center, etc.). Convention: grow towards the center of the screen. Panels typically use `depth: 10`.

**Current approach:** Modal panels with a full-screen transparent overlay. Clicking the overlay closes the panel. Future directions may include entity-anchored popups or a window system.

**Canonical example:** `StationInfoPanelService.cs` — demonstrates all patterns including dynamic inventory rows.

## Adding a New Panel

1. Create layout XML in `src/Olve.Trains/resources/layouts/MyPanel.xml`
2. Run asset pipeline: `cd src/Olve.Trains.AssetPipeline && dotnet run`
3. Add/reuse styles in `Styles.cs`, register new ones in `GameStyleRegistration`
4. Create panel service in `src/Olve.Trains/Scenes/GameUI/GUI/MyPanelService.cs`
5. Register in `src/Olve.Trains/Scenes/GameUI/UISceneServiceRegistration.cs`

## Deep References

For comprehensive details on specific topics, read the reference files:

- `references/elements.md` — Full property reference for Box, Text, Image and interactive elements
- `references/layout.md` — Layout XML format, flexbox algorithm, coordinate system, asset pipeline codegen
- `references/styles-and-interactivity.md` — Style definition, state system, animation flow, input pipeline, anchors
- `references/panels.md` — Panel service pattern with full code examples, dynamic rows, registration
- `references/adding-a-new-panel.md` — Detailed step-by-step walkthrough with complete code templates

## Key Files

| File | Purpose |
|------|---------|
| `src/Olve.Trains/resources/layouts/*.xml` | Layout definitions |
| `src/Olve.Trains/assets/Layouts/*.cs` | Generated layout builders (do not edit) |
| `src/Olve.Trains/Scenes/GameUI/GUI/Styles.cs` | All game styles |
| `src/Olve.Trains/Scenes/GameUI/GUI/GameStyleService.cs` | Style registration |
| `src/Olve.Trains/Scenes/GameUI/UISceneServiceRegistration.cs` | Panel service registration |
| `src/Olve.Trains/Scenes/GameUI/GUI/*PanelService.cs` | Panel implementations |
| `src/Olve.Trains/Shared/GUI/GuiSceneServiceRegistration.cs` | Core GUI service wiring |
