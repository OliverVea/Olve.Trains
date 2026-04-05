# Panel Services — Full Reference

A panel service is an `ISceneService` that manages a GUI panel's lifecycle: build layout, register with GUI system, handle input, update content, clean up.

## Canonical Example: StationInfoPanelService

File: `src/Olve.Trains/Scenes/GameUI/GUI/StationInfoPanelService.cs`

This is the most complete example — demonstrates overlay pattern, dynamic rows, content updates, and click handling.

## Service Structure

```csharp
public class MyPanelService(
    GuiElementService guiElementService,
    GuiActivationService guiActivationService,
    GuiAnchorService guiAnchorService,
    /* domain services... */) : ISceneService
{
    private Layouts.MyPanel? _panel;
    private Id<GuiElementRegistrations> _registrationId;
    private Id<GuiAnchor> _anchorId;
    private bool _isOpen;
```

### Load/Unload

```csharp
public Result Load()
{
    guiActivationService.GuiElementActivated.Subscribe(OnGuiElementActivated);
    return Result.Success();
}

public Result Unload()
{
    guiActivationService.GuiElementActivated.Unsubscribe(OnGuiElementActivated);
    if (_isOpen) ClosePanel();
    return Result.Success();
}
```

### Opening a Panel

```csharp
private Result OpenPanel()
{
    _panel = Layouts.BuildMyPanel();

    // 1. Register anchor (screen position + growth direction)
    if (guiAnchorService.RegisterAnchor(AnchorPosition.TopRight, GrowthDirection.DownLeft, depth: 10)
        .TryPickProblems(out var problems, out _anchorId))
        return problems;

    // 2. Register element tree
    if (guiElementService.RegisterElementAndChildren(_anchorId, _panel.Root)
        .TryPickProblems(out problems, out _registrationId))
    {
        guiAnchorService.UnregisterAnchor(_anchorId);
        return problems;
    }

    _isOpen = true;
    return Result.Success();
}
```

### Closing a Panel

```csharp
private Result ClosePanel()
{
    guiElementService.UnregisterElementAndChildren(_registrationId);
    guiAnchorService.UnregisterAnchor(_anchorId);
    _isOpen = false;
    _panel = null;
    return Result.Success();
}
```

### Handling Button Clicks

```csharp
private void OnGuiElementActivated(GuiActivationService.GuiElementActivatedMessage message)
{
    if (!_isOpen || _panel is null) return;

    if (NodeIdMatches(_panel.MyButton, message.NodeId))
    {
        // Handle click
    }
}

private bool NodeIdMatches(GuiElement element, Id<GuiNode> nodeId)
{
    if (!guiElementService.TryGetGuiNodeId(element.Id, _registrationId, out var elementNodeId))
        return false;
    return nodeId == elementNodeId;
}
```

## Overlay Pattern

Current standard for closeable panels. A full-screen transparent Box intercepts clicks outside the panel content:

```xml
<Box id="Overlay" Style="PanelOverlayStyle" Interactive="true">
    <Box id="Panel" Style="PanelStyle" Interactive="true">
        <!-- panel content -->
    </Box>
</Box>
```

The overlay style has transparent background and `Weight=1` to fill the screen. Both overlay and panel are `Interactive="true"` — the panel intercepts clicks on itself, clicks that fall through to the overlay close the panel.

In the service, clicking the overlay triggers close:

```csharp
private void OnGuiElementActivated(GuiActivationService.GuiElementActivatedMessage message)
{
    if (NodeIdMatches(_panel.Overlay, message.NodeId))
        ClosePanel();
}
```

## Dynamic Row Content

For lists/repeating content, use a separate layout XML as a row template.

### Row Template XML

```xml
<!-- InventoryRow.xml -->
<Box id="Row" Style="InventoryRowStyle">
    <Text id="CargoName" Content='""' Style="InventoryRowTextStyle" />
    <Text id="AmountText" Content='""' Style="InventoryRowAmountStyle" />
</Box>
```

### Mounting Rows

```csharp
private readonly record struct MountedRow(
    Layouts.InventoryRow Row,
    Id<GuiElementRegistrations> RegistrationId);

private readonly List<MountedRow> _mountedRows = [];

private void MountRows()
{
    // Get container node ID from the panel registration
    if (!guiElementService.TryGetGuiNodeId(
        _panel.ContentContainer.Id, _registrationId, out var containerNodeId))
        return;

    foreach (var item in dataSource)
    {
        var row = Layouts.BuildInventoryRow();
        row.CargoName.Content = item.Name;
        row.AmountText.Content = $"{item.Amount}/{item.Capacity}";

        if (guiElementService.RegisterElementAndChildren(containerNodeId, row.Row)
            .TryPickProblems(out _, out var rowRegistrationId))
            continue;

        _mountedRows.Add(new MountedRow(row, rowRegistrationId));
    }
}
```

### Updating Row Content

```csharp
// In Update(), modify element properties directly
foreach (var mounted in _mountedRows)
{
    var amount = GetCurrentAmount(mounted.ItemId);
    mounted.Row.AmountText.Content = $"{amount}/{capacity}";
}
```

### Unmounting Rows

```csharp
private void UnmountRows()
{
    foreach (var mounted in _mountedRows)
        guiElementService.UnregisterElementAndChildren(mounted.RegistrationId);
    _mountedRows.Clear();
}
```

Always unmount rows before closing the panel.

## Triggering Panel Open

Panels are typically opened in response to game events in `Update()`:

```csharp
public Result Update()
{
    if (_clickedThisFrame && toolManagementService.ActiveToolId is null)
    {
        // Check if player clicked a relevant entity
        foreach (var hit in mouseRaycastService.Hits)
        {
            if (hit.Group != ColliderGroups.Building) continue;
            // ... resolve entity, open panel
            OpenPanel(entityId);
            break;
        }
    }

    if (_isOpen) UpdateContent();
    return Result.Success();
}
```

## Registration

Panel services are registered in `src/Olve.Trains/Scenes/GameUI/UISceneServiceRegistration.cs`:

```csharp
services.AddSceneService<MyPanelService>(sceneId);
```
