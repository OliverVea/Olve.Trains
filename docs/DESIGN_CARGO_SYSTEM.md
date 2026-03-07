# Cargo System

## Design

### Core Principles

- **Stations are access points, not storage.** Stations connect trains to nearby buildings and cities. They hold no cargo themselves.
- **Buildings have inventories.** Industries store their inputs and outputs locally.
- **Cities are the consumption endpoint.** Cities consume goods delivered to stations in range and pay out money. Cities determine demand and pricing.
- **Trains have wagons with inventories.** Each wagon has a fixed capacity and an optional cargo type filter.
- **No category abstraction.** Cargo filtering is done with explicit sets of allowed `Id<CargoType>`. An empty/null filter means "accept all."
- **Cargo flow:** Industries → Trains → Stations → Cities (for consumption) or → Industries (for secondary processing).

### CargoInventory

Shared data model for both building and wagon inventories.

- Fixed capacity (total units across all types)
- Optional filter: `ImmutableHashSet<Id<CargoType>>?` (null = accept all)
- Stores `Id<CargoType>` -> amount mapping
- Operations: `CanAccept(type, amount)`, `TryAdd(type, amount)`, `TryRemove(type, amount)`, `GetAmount(type)`

### Building Inventories

Each building with a recipe gets an inventory. The filter is derived from the recipe's inputs + outputs. Primary industries (forest, mine) have no inputs — they produce from nothing. Secondary industries (sawmill) require inputs.

### Wagons and Train Inventories

A wagon has a cargo filter and a `CargoInventory`. A train is composed of an engine + wagons.

For the demo, each train has one "goods wagon" with no filter (accepts any cargo type). The full game adds specialized wagon types where the player can set filters (e.g., only iron ore and coal).

### Loading/Unloading

When a train stops at a station:
1. Find all buildings within station range
2. **Unload:** For each wagon cargo, check if any building accepts it (has matching recipe input with available capacity). Transfer cargo from wagon to building.
3. **Load:** For each building with output cargo, check if any wagon accepts it (passes filter, has capacity). Transfer cargo from building to wagon.

### Production Ticking

An `IndustryProductionService` runs each frame/tick:
- **Primary industries** (no inputs): Produce output unconditionally, up to inventory capacity.
- **Secondary industries** (has inputs): If all required inputs are available, consume inputs and produce outputs.

Production rate is configurable per recipe.

### Economics

Money is earned on cargo transfers. Payout scales with chain complexity:

- **Small payout** for industry → industry transfers (raw materials to secondary industries)
- **Big payout** for delivering finished goods to cities

Cities are the primary economic driver. All entity types (industries, cities) participate in the same station-based cargo system — loading/unloading logic doesn't distinguish between them.

### City Leveling

Cities have levels (1–5). Leveling requires cumulative delivery of specific goods:

- **Level 1 → 2:** Simple requirements (e.g., 100 wood, 100 wheat, 100 cows)
- **Level 2 → 3, 3 → 4, 4 → 5:** Rapidly growing complexity — more cargo types, higher quantities, processed goods

Higher city level provides:
- More population → more consumption → more money from deliveries
- More passengers (for City → City transport, future)
- Potentially unlocks demand for new cargo types

### Future Cargo Routes

The system should be generic enough to support:
- **Industry → Industry** (wood → sawmill)
- **Industry → City** (goods → consumption, main money source)
- **City → Industry** (workers to factories, future)
- **City → City** (passengers, future)

## Implementation Order

1. `CargoInventory` data model
2. Building inventories (managed by `BuildingInventoryService`)
3. Wagons and train inventories
4. Loading/unloading at stations
5. Industry production ticking
