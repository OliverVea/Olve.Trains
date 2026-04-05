# Adding a New Panel — Step-by-Step

## 1. Create Layout XML

File: `src/Olve.Trains/resources/layouts/MyPanel.xml`

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Box id="Overlay" Style="StationPanelOverlayStyle" Interactive="true">
    <Box id="Panel" Style="StationPanelStyle" Interactive="true">
        <Text id="Header" Content='"My Panel"' Style="StationPanelHeaderStyle" />
        <Text id="Description" Content='""' Style="StationPanelInfoTextStyle" />
        <Box id="ContentContainer" Style="StationPanelContainerStyle" />
    </Box>
</Box>
```

Reuse existing styles where possible. Check `Styles.cs` for available styles before creating new ones. The Station panel styles work well as a starting point for info panels.

If you need dynamic list content, also create a row template:

```xml
<!-- MyRow.xml -->
<?xml version="1.0" encoding="utf-8" ?>
<Box id="Row" Style="InventoryRowStyle">
    <Text id="Label" Content='""' Style="InventoryRowTextStyle" />
    <Text id="Value" Content='""' Style="InventoryRowAmountStyle" />
</Box>
```

## 2. Run Asset Pipeline

```bash
cd src/Olve.Trains.AssetPipeline && dotnet run
```

This generates `src/Olve.Trains/assets/Layouts/MyPanel.cs` (and `MyRow.cs` if applicable).

## 3. Add Styles (if needed)

File: `src/Olve.Trains/Scenes/GameUI/GUI/Styles.cs`

Only create new styles if the existing ones don't fit. If you do create new styles:

```csharp
public static readonly GuiElementStyling<Box> MySpecialStyle = new()
{
    StyleKey = new StyleKey(nameof(MySpecialStyle)),
    OnStateChanged = (box, _) =>
    {
        box.BackgroundColor = PanelBackground;
        // ... set properties
    }
};
```

Register in `GameStyleService.cs` — add to the `GameStyles` array:

```csharp
private static readonly IGuiElementStyling[] GameStyles =
[
    // ... existing styles ...
    Styles.MySpecialStyle,
];
```

## 4. Create Panel Service

File: `src/Olve.Trains/Scenes/GameUI/GUI/MyPanelService.cs`

Use `StationInfoPanelService` as the canonical template. Key ingredients:

- Constructor-inject `GuiElementService`, `GuiActivationService`, `GuiAnchorService`, plus domain services
- Track `_panel`, `_registrationId`, `_anchorId`, `_isOpen` state
- `Load()`: subscribe to `GuiElementActivated`
- `Unload()`: unsubscribe, close if open
- `OpenPanel()`: build layout, register anchor + elements
- `ClosePanel()`: unregister elements + anchor
- `OnGuiElementActivated()`: handle overlay click to close, button clicks for actions
- `Update()`: detect open trigger (e.g. entity click), update content if open

## 5. Register Service

File: `src/Olve.Trains/Scenes/GameUI/UISceneServiceRegistration.cs`

```csharp
services.AddSceneService<MyPanelService>(sceneId);
```

## Checklist

- [ ] Layout XML created in `resources/layouts/`
- [ ] Row template XML created (if dynamic list content)
- [ ] Asset pipeline run successfully
- [ ] Generated C# files appear in `assets/Layouts/`
- [ ] New styles added to `Styles.cs` and registered in `GameStyleService.cs` (if needed)
- [ ] Panel service created with open/close/click handling
- [ ] Service registered in `UISceneServiceRegistration.cs`
- [ ] Integration tests pass
