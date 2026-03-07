# Station Info Panel

## Goal

Click a station to see nearby industry inventories and their transfer directions. Provides visual feedback for the cargo system during development.

## Approach: XML Layouts with Runtime Mounting

The GUI system already supports mounting layouts under existing nodes — `GuiElementService.RegisterElementAndChildren` accepts `UnionId<GuiAnchor, GuiNode>` as the parent, and each registration gets a unique `Id<GuiElementRegistrations>`. This means the same XML-defined layout can be instantiated multiple times with independent node IDs.

### Why this approach

- **Layouts stay in XML.** Both the panel shell and the repeating row are designed in XML, compiled via the asset pipeline, and get generated C# factory methods. No hand-built GUI elements in service code.
- **Multiple instances are already supported.** The bi-map key in `GuiElementService` is `(RegistrationId, ElementId)`, not just `ElementId`. Calling `BuildInventoryRow()` and registering it N times produces N independent node trees, each with its own registration ID.
- **Clean lifecycle.** Each mounted row has a registration ID. To refresh, unregister all rows and re-mount. To close the panel, unregister the panel itself (cascades to children via `GuiNodeService`).
- **No new GUI infrastructure needed.** This uses existing registration, node parenting, and unregistration APIs.

### Alternative approaches considered

- **Single layout with fixed slots.** Define N row elements in one XML file. Simpler but limits the number of visible industries and wastes elements when fewer are needed.
- **Fully programmatic elements.** Build `Box`/`Text` instances in C# code. Flexible but loses the XML design benefits and is harder to maintain visually.
- **Dynamic XML generation.** Generate XML at runtime and parse it. Over-engineered, slow, and the asset pipeline isn't designed for runtime use.

## Layouts

### `StationInfoPanel.xml`

Panel shell with a title and a container box that serves as the mount point for inventory rows.

```
+---------------------------+
| Station: <name>           |
|---------------------------|
| [mount point - empty box] |
|   (rows mounted here)     |
+---------------------------+
```

Key elements:
- `StationName` (Text) — updated at runtime with the station name
- `InventoryContainer` (Box, vertical layout) — mount point for child rows

### `InventoryRow.xml`

One row per industry cargo type. Instantiated and mounted into the container at runtime.

```
| [direction] <cargo name>  <amount>/<capacity> |
```

Key elements:
- `DirectionIndicator` (Text) — shows transfer direction (e.g., arrow in/out)
- `CargoName` (Text) — cargo type name
- `AmountText` (Text) — current amount / capacity

## Runtime Flow

### Opening the panel

1. Player clicks a station (detected via raycast + collision, like `SignalRulesPanelService`)
2. `StationInfoPanelService` receives the click, identifies the station
3. Register `StationInfoPanel` layout at an anchor (e.g., right side of screen)
4. Get the `InventoryContainer` element's node ID from the registration
5. Query industries within station range (see below)
6. For each industry:
   - Get its `CargoInventory` and transfer policies
   - For each cargo type in the inventory's allowed types:
     - Call `BuildInventoryRow()` to get a fresh element tree
     - Set text content: cargo name, amount, direction
     - Call `RegisterElementAndChildren(containerNodeId, row)` — mounts as child
     - Store the row's registration ID for later cleanup

### Updating (each frame or on change)

- Update `AmountText.Content` on each mounted row with current inventory amounts
- Alternatively, rebuild rows only when inventory changes (event-driven)

### Closing the panel

- Unregister all row registrations
- Unregister the panel registration
- Clear stored state

## Station-to-Industry Query

`StationService` already knows a station's building ID and track. We need to find industries within range:

1. Get station's building position + `StationProperties.Range`
2. Query all buildings within that range (grid-based proximity)
3. Filter to buildings that have an associated `Industry` (via `IndustryService.TryGetByBuilding`)
4. Return the list of `Industry` entities

This query can live on `StationService` or as a standalone utility. It will also be needed later for loading/unloading, so it should be reusable.
