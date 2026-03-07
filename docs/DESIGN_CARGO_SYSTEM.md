# Cargo System

## Problem

Industries exist as buildings with recipes (inputs/outputs), but there's no runtime cargo flow. No inventories, no train loading/unloading, no production. Cargo types and recipes are defined but inert.

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

## Implementation Order

1. `CargoInventory` data model
2. Building inventories (managed by `BuildingInventoryService`)
3. Wagons and train inventories
4. Loading/unloading at stations
5. Industry production ticking
