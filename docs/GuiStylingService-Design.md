# GuiStylingService Design Specification

## Overview

A new service that subscribes to `GuiElementService.OnAdded` and applies styling from `GuiElementStyling<T>` to elements based on a `Style` attribute specified in XML.

## Components

### 1. StyleKey Value Type (in Engine3D)

```csharp
// In Olve.Engine3D/GUI/Styling/StyleKey.cs
public readonly record struct StyleKey(string Value);
```

### 2. GuiElement Changes

Add a style key property to the base class:

```csharp
// In GuiElement.cs
public abstract class GuiElement : IHasId<Id<GuiElement>>
{
    // existing properties...
    public StyleKey? StyleKey { get; set; }
}
```

### 2. XML Syntax

Elements reference styles by key:

```xml
<Box id="PlaceTrack" Style="MenuButtonStyle" Height="40" ...>
    <Image Texture="Textures.Textures.icons_tracks" />
</Box>
```

### 3. Code Generation Changes

The asset pipeline extracts `Style` attribute and assigns it to `StyleKey`:

```csharp
// Generated code
var PlaceTrack = new Box()
{
    Name = "PlaceTrack",
    Id = Id.FromName<GuiElement>("ToolBar/Box/PlaceTrack"),
    StyleKey = "MenuButtonStyle",  // <-- new
    Height = 40,
    // ...
};
```

### 4. GuiStyleRegistry (in Engine3D)

Generic registry with type as part of the key:

```csharp
public class GuiStyleRegistry
{
    private readonly Dictionary<(StyleKey Key, Type ElementType), object> _styles = new();

    public void Register<T>(StyleKey key, GuiElementStyling<T> styling) where T : GuiElement
    {
        _styles[(key, typeof(T))] = styling;
    }

    public bool TryGet<T>(StyleKey key, out GuiElementStyling<T>? styling) where T : GuiElement
    {
        if (_styles.TryGetValue((key, typeof(T)), out var value))
        {
            styling = (GuiElementStyling<T>)value;
            return true;
        }
        styling = null;
        return false;
    }
}
```

### 5. GuiStylingService (in Engine3D)

The service applies styles when elements are added:

```csharp
public class GuiStylingService(
    ILoggingManager loggingManager,
    GuiElementService guiElementService,
    GuiStyleRegistry styleRegistry) : SceneService(loggingManager)
{
    private readonly EventQueue<GuiElementArgs> _elementAddedQueue = new(guiElementService.OnAdded);

    protected override Result OnLoad()
    {
        _elementAddedQueue.SetHandler(OnGuiElementAdded).Init();
        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        return _elementAddedQueue.Update();
    }

    private Result OnGuiElementAdded(GuiElementArgs args)
    {
        if (!guiElementService.TryGetElement(args.NodeId, out var element))
            return Result.Success();

        if (element.StyleKey is not { } styleKey)
            return Result.Success();

        // TODO: How to call TryGet<T> when we only have GuiElement at runtime?
        // Options:
        // 1. Switch on element type (Box, Text, Image)
        // 2. Add non-generic TryGet that returns object
        // 3. Have element types register themselves

        return Result.Success();
    }
}
```

**Note:** We need to decide how to bridge from runtime `GuiElement` to generic `TryGet<T>`. See open questions.

### 6. GameStyleService (in Olve.Trains)

A game-level service that registers all styles:

```csharp
public class GameStyleService(
    ILoggingManager loggingManager,
    IGuiStyleRegistry styleRegistry) : SceneService(loggingManager)
{
    protected override Result OnLoad()
    {
        styleRegistry.Register("MenuButtonStyle", Styles.MenuButtonStyle);
        // ... more styles

        return Result.Success();
    }
}
```

## Data Flow

```
XML: Style="MenuButtonStyle"
         |
         v
Asset Pipeline (extracts Style -> StyleKey property)
         |
         v
Generated Code: StyleKey = new StyleKey("MenuButtonStyle")
         |
         v
GameStyleService.OnLoad() calls registry.Register<Box>("MenuButtonStyle", ...)
         |
         v
GuiElementService.RegisterElementAndChildren()
         |
         v
OnAdded event fires
         |
         v
GuiStylingService receives event
         |
         v
Gets element, checks StyleKey
         |
         v
GuiStyleRegistry.TryGet<Box>(styleKey, out var styling)
         |
         v
styling.Setup?.Invoke(element)
```

## Open Questions

### 1. Runtime Type to Generic Bridge
How should `GuiStylingService` call `TryGet<T>` when it only has a `GuiElement` at runtime?

- **Option A: Switch on element type** - Explicit switch for Box, Text, Image
- **Option B: Non-generic overload** - Add `TryGet(StyleKey, Type, out object?)` alongside generic version
- **Option C: Element self-applies** - Element types know how to apply their own styles

### 2. Hover Event Wiring
Should the service also wire up `OnHoverEnter`/`OnHoverExit`?

- **Option A: Include hover wiring** - Service handles all styling lifecycle
- **Option B: Separate concern** - Keep hover handling in a different service (for now)

## Files to Create/Modify

### New Files
1. `src/Olve.Engine3D/GUI/Styling/StyleKey.cs` - Value type for style keys
2. `src/Olve.Engine3D/GUI/Styling/GuiStyleRegistry.cs` - Registry with generic Register/TryGet
3. `src/Olve.Engine3D/GUI/Styling/GuiStylingService.cs` - Applies styles on element added
4. `src/Olve.Trains/Scenes/UI/GUI/GameStyleService.cs` - Registers game styles

### Modified Files
1. `src/Olve.Engine3D/GUI/Elements/GuiElement.cs` - Add `StyleKey` property
2. `src/Olve.Trains.AssetPipeline/Layouts/ProcessLayouts.cs` - Extract `Style` attribute
3. `src/Olve.Trains.AssetPipeline/templates/LayoutClass.scriban` - Generate `StyleKey` assignment
