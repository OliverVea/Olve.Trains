# Styles and Interactivity — Full Reference

## Styles

Location: `src/Olve.Trains/Scenes/GameUI/GUI/Styles.cs`

### Defining a Style

```csharp
public static readonly GuiElementStyling<Box> MyPanelStyle = new()
{
    StyleKey = new StyleKey(nameof(MyPanelStyle)),
    StateTransitions = ButtonTransitions,  // Optional: animation timings
    OnStateChanged = (box, weights) =>
    {
        var pressed = weights[GuiNodeState.Pressed];
        var focused = weights[GuiNodeState.Focused];

        box.BackgroundColor = (0.2f, 0.22f, 0.21f, 0.9f);
        box.Vertical = true;
        box.Padding = 15;
        box.Gap = 8;
        box.BorderColor = Lerp(DefaultBorder, FocusBorder, focused);
        box.BorderWidth = DefaultBorderWidth + 0.5f * pressed;
        box.BorderRadius = DefaultBorderRadius;
    }
};
```

The `OnStateChanged` callback is called whenever state weights change. It sets **all** visual properties for the element — this is the single source of truth for how the element looks.

### State Flags

`GuiNodeState` flags managed by `GuiNodeStateService`:

| Flag | Trigger | Weight Meaning |
|------|---------|----------------|
| `Show` | Element registered | 1.0 = visible |
| `Enabled` | Element registered | 1.0 = interactive |
| `Focused` | Mouse hovers over element | 0.0-1.0 animated hover |
| `Pressed` | Mouse button held on element | 0.0-1.0 animated press |
| `Active` | Set programmatically (e.g. selected tool) | 0.0-1.0 animated toggle |

### State Transitions (Animations)

```csharp
public static readonly Dictionary<GuiNodeState, StateTransition> ButtonTransitions = new()
{
    [GuiNodeState.Focused] = new StateTransition(
        In: new GuiTransition(new Ms(100), Easing.EaseOut),
        Out: new GuiTransition(new Ms(100), Easing.EaseIn)),
    [GuiNodeState.Pressed] = new StateTransition(
        In: new GuiTransition(new Ms(25), Easing.EaseIn),
        Out: new GuiTransition(new Ms(50), Easing.EaseOut)),
};
```

States without transitions snap immediately (weight jumps 0→1 or 1→0). With transitions, the weight interpolates smoothly over the specified duration and easing curve.

### Animation Flow

1. `GuiNodeStateService` sets state flag → fires `OnStateChanged`
2. `GuiAnimationService` detects flag change → creates animation with easing
3. `GuiAnimationService.Update()` advances animations → produces `StateWeights` (0-1 per flag)
4. `GuiStyleApplierService` calls `OnStateChanged(element, weights)` with interpolated values
5. Element properties updated → rendering services upload to GPU

### Style Constants

```csharp
Styles.DefaultBorderRadius  // 6
Styles.DefaultPadding       // 2
Styles.DefaultBorderWidth   // 1.5f
Styles.DefaultBorder        // (0.2f, 0.2f, 0.2f, 0.3f)
Styles.PanelBackground      // (0.27f, 0.3f, 0.28f, 0.65f)
Styles.FocusBorder           // (1, 1, 1, 1)
Styles.DimTextColor          // (0.75f, 0.75f, 0.75f, 1f)
Styles.ButtonTransitions     // Standard hover/press timings
```

Helper: `Lerp(a, b, t)` for float and RGBA interpolation.

### Style Conventions

- **Prefer shared styles.** Reuse styles across panels for visual cohesion. Only create panel-specific styles for unique look/behavior.
- **Register all styles** in the `GameStyles` array in `GameStyleService.cs` (`GameStyleRegistration.RegisterAllStyles()`).

### Existing Shared Styles (partial list)

| Style | Element | Purpose |
|-------|---------|---------|
| `ModalButtonStyle` | Box | Standard modal button |
| `ModalButtonTextStyle` | Text | Modal button label |
| `MenuBarBackground` | Box | Panel background |
| `InfoTextMedium` | Text | Dim 12px info text |
| `InventoryRowStyle` | Box | Row container in lists |
| `InventoryRowTextStyle` | Text | Row main text |
| `InventoryRowAmountStyle` | Text | Row amount (dim) |
| `InventoryRowDirectionStyle` | Text | Row direction indicator |

## Input System

### How Mouse Input Works

`GuiMouseInputService` (`src/Olve.Engine3D/GUI/Input/GuiMouseInputService.cs`) runs at **Priority -100** (before all other input services). It processes input in a single `Input()` call each frame:

1. **Hit testing:** Gets mouse position in Px, calls `GuiCollisionService.GetGuiCollisions(mousePos)` for AABB hit testing against all GUI nodes.
2. **Topmost selection:** Filters hits to `Interactive` elements only, sorts by depth (highest on top via `GuiDepthService`), takes the topmost.
3. **Focus:** Calls `GuiFocusService.SetFocus(topmost)`. This fires `OnFocusChanged` which sets the `Focused` state flag on the hovered element (and clears it from the previous).
4. **Press/Release/Click:** Handles left mouse button only:
   - **Left press** on interactive element → stores `_pressedNode`, fires `OnPressedNode` → sets `Pressed` state flag
   - **Left release** → if released on the same node that was pressed AND that node is `Enabled`, fires `GuiActivationService.Activate(nodeId)` which broadcasts `GuiElementActivated`; fires `OnReleasedNode` → clears `Pressed` flag
5. **Input blocking:** Returns `Pass.Block` if the mouse is over or interacting with an interactive GUI element. This prevents the click from reaching game services (tools, camera, world interaction). Returns `Pass.Pass` otherwise.

### What's Not Handled

- **Right-click** — Not processed by GUI. Falls through to game services.
- **Middle-click** — Not processed by GUI.
- **Keyboard input** — No built-in GUI keyboard handling. Game services handle keyboard directly.
- **Scroll** — Not processed by GUI (except by specific element services like Slider).

### Key Services

| Service | File | Role |
|---------|------|------|
| `GuiMouseInputService` | `GUI/Input/GuiMouseInputService.cs` | Hit testing, focus, press/release tracking, input blocking |
| `GuiFocusService` | `GUI/Input/GuiFocusService.cs` | Tracks focused (hovered) node, fires `OnFocusChanged` |
| `GuiActivationService` | `GUI/Input/GuiActivationService.cs` | Fires `GuiElementActivated` on click (press+release on same element) |
| `GuiCollisionService` | `GUI/Collision/GuiCollisionService.cs` | AABB hit testing for GUI nodes |
| `GuiNodeStateService` | `GUI/GuiNodeStateService.cs` | Manages state flags per node |

### Input Flow Diagram

```
Mouse move/click
    ↓
GuiMouseInputService.Input() [Priority -100]
    ↓
GuiCollisionService.GetGuiCollisions(mousePos) — AABB hit test
    ↓
Filter to Interactive elements, sort by depth
    ↓
GuiFocusService.SetFocus(topmost) → OnFocusChanged → Focused state flag
    ↓
Left press? → OnPressedNode → Pressed state flag
Left release on same node? → GuiActivationService.Activate() → GuiElementActivated
    ↓
Return Block (over GUI) or Pass (not over GUI)
    ↓
[If Pass] Game services receive input (tools, camera, MouseRaycastService, etc.)
```

### World Click Detection (Game Side)

Panel services that open on world-entity clicks use `MouseRaycastService` (`src/Olve.Trains/Scenes/GameUI/MouseRaycastService.cs`):

```csharp
// In Update(), check for clicks on world entities
foreach (var hit in mouseRaycastService.Hits)
{
    if (hit.Group != ColliderGroups.Building) continue;
    // Resolve entity from collider, open panel
}
```

`MouseRaycastService` casts a ray from camera through mouse position into the 3D scene and returns `RaycastHit` results with `ColliderId` and `Group`. This only fires when GUI doesn't block input.

### InheritParentState

When `InheritParentState = true` on a child element:
- Parent focuses → child also gets Focused flag
- Parent pressed → child also gets Pressed flag

Common use: Text inside a Button box inherits hover/press so the text style reacts together with the button.

## Anchors

Anchors position root-level GUI elements on screen.

```csharp
guiAnchorService.RegisterAnchor(AnchorPosition.TopRight, GrowthDirection.DownLeft, depth: 10)
```

### AnchorPosition

Where on screen the element's origin is placed: `TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`, `Center`, `MiddleLeft`, `MiddleRight`, etc.

### GrowthDirection

Which direction the element tree grows from the anchor: `DownRight`, `DownLeft`, `UpRight`, `UpLeft`, `Left`, `Right`, `Center`, etc.

### Depth

Z-ordering. Higher depth renders on top. Panels typically use `depth: 10`.

### Convention

Growth direction should grow towards the center of the screen:
- TopRight → DownLeft
- TopLeft → DownRight
- BottomLeft → UpRight
- Center → Center

## GUI Scene Service Wiring

All core GUI services are registered via `GuiSceneServiceRegistration.AddGuiSceneServices(sceneId)` in `src/Olve.Trains/Shared/GUI/GuiSceneServiceRegistration.cs`. This wires up:

- Layout services (context, update, computation)
- Rendering services (rectangles, text)
- Input services (mouse, focus, activation)
- State and animation services
- Event handlers connecting state changes → animation → style application
- Interactive element services (slider, checkbox, dropdown, radio button)
