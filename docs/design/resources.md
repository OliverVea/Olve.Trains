# Resource System Design

## Overview

Resources are world entities (trees, ore deposits, etc.) that extractive industries need nearby to produce cargo. Resources are never depleted — they act as placement constraints and productivity modifiers.

## Resource Ownership

Each resource entity is owned by the **nearest consumer (producer) within range**. A resource with no consumer in range is unowned and contributes nothing.

## Productivity Model

Total productivity for a producer P with N owned resources:

```
total_P = sum over owned resources of: E(P, resource)
```

Per-resource efficiency:

```
E = N^(k-1) * (1 - (d / range)^2)
```

Where:
- **N** = number of resources owned by producer P
- **k** = diminishing returns exponent (5/6)
- **d** = distance from producer to resource
- **range** = harvest range (per building blueprint, e.g. 10 tiles for a forester)

### Distance efficiency: `1 - (d / range)^2`

- 1.0 at distance 0, 0.0 at max range
- Quadratic falloff — gentle near the producer, steep near the edge

### Count efficiency: `N^(k-1)` per resource

- With k = 5/6, this is `N^(-1/6)` — each additional resource adds less per-resource efficiency
- But total productivity `N^(k-1) * sum(...)` always increases with more resources
- A producer with 0 resources produces nothing

## Design Goals

- **More producers always helps**: Total system productivity must always increase when adding a producer, regardless of placement. Validated via Monte Carlo simulation (0 violations in 1000 trials).
- **Spreading out is rewarded**: Two well-spaced producers significantly outperform two producers on the same spot (~153% vs ~113% of a single producer).
- **Two producers ~150% of one**: On average, a second producer should yield roughly 150% of a single producer's output (observed range: 130%-185%).
- **Diminishing returns without a cap**: Productivity always grows with more resources/producers, just slower and slower.

## Simulation

See `scripts/resource_simulation.py` for the Monte Carlo validation of these properties.
