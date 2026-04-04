# World Scale

## Scale Factor

**1 game unit = 50 meters**

All game-world coordinates, distances, and speeds use this scale. When translating real-world values to game values, divide by 50.

A standard track segment is 4 game units = 200 meters real-world.

## Freight Train in Game Units

Derived from [freight-train.md](freight-train.md) using the 1:50 scale.

### Dimensions

| Parameter | Real-world | Game units |
|---|---|---|
| Locomotive length | 22.4 m | 0.448 |
| Locomotive width | 3.2 m | 0.064 |
| Locomotive height | 4.7 m | 0.094 |
| Boxcar length (over couplers) | 16.2 m | 0.324 |
| Boxcar width | 3.2 m | 0.064 |
| Boxcar height | 4.8 m | 0.096 |
| Coupling gap | ~0.12 m | 0.0024 |
| Standard gauge | 1.435 m | 0.0287 |
| Wheel diameter | 0.914 m | 0.0183 |

### Speed

| Parameter | Real-world | Game units/s |
|---|---|---|
| Max freight speed (100 km/h) | 28 m/s | 0.56 |
| Bulk freight speed (80 km/h) | 22 m/s | 0.44 |
| Slow freight (50 km/h) | 14 m/s | 0.28 |

### Braking

| Parameter | Real-world | Game units/s^2 |
|---|---|---|
| Service braking (loaded) | 0.2 m/s^2 | 0.004 |
| Emergency braking (loaded) | 0.5 m/s^2 | 0.01 |

### Stopping Distances

Using service braking (0.004 game units/s^2):

| Speed (game units/s) | Stopping distance (game units) | Track segments |
|---|---|---|
| 0.56 (100 km/h) | 39.2 | ~10 |
| 0.44 (80 km/h) | 24.2 | ~6 |
| 0.28 (50 km/h) | 9.8 | ~2.5 |

### Head-Based Positioning Lead Distance

Using `d_lead = v^2 / (2 * a_brake * k_lead)` with `k_lead = 0.5`:

With service braking (a_brake = 0.004):

| Speed (game units/s) | Lead distance (game units) | Track segments |
|---|---|---|
| 0.56 | 78.4 | ~20 |
| 0.44 | 48.4 | ~12 |
| 0.28 | 19.6 | ~5 |

With emergency braking (a_brake = 0.01):

| Speed (game units/s) | Lead distance (game units) | Track segments |
|---|---|---|
| 0.56 | 31.4 | ~8 |
| 0.44 | 19.4 | ~5 |
| 0.28 | 7.8 | ~2 |

## Fit Check

At 50m/unit, a 4-unit track segment holds:
- ~8.9 locomotives end-to-end (200m / 22.4m)
- ~12.3 boxcars end-to-end (200m / 16.2m)
- A typical 20-car train with locomotive: ~350m = **7 game units = ~1.75 track segments**

This feels right — a full train occupies a meaningful portion of a track segment without overflowing it for short consists.
