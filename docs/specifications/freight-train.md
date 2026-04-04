# Freight Train Specifications

Reference dimensions and physics for a typical North American freight train (EMD SD70ACe locomotive, standard 50 ft cars). All values are real-world measurements from AAR standards and manufacturer data sheets.

## Locomotive (EMD SD70ACe)

| Parameter | Value |
|---|---|
| Length (over couplers) | 22.4 m |
| Width | 3.2 m |
| Height (rail to roof) | 4.7 m |
| Weight | 188 t |
| Gross power | 3,200 kW (4,300 hp) |
| Effective traction power | 2,750 kW (~86% drivetrain efficiency) |
| Starting tractive effort | 698 kN (adhesion-limited) |
| Continuous tractive effort | 489 kN at ~18 km/h |
| Corner speed | ~3.9 m/s (14 km/h) |
| Wheel arrangement | Co-Co (6 axles) |
| Bogie spacing (center to center) | 13.0 m |
| Truck wheelbase (per bogie) | 2.1 m |

### Tractive Effort Curve

Two regions:

1. **Below corner speed (~3.9 m/s)**: constant force at 698 kN (adhesion-limited)
2. **Above corner speed**: constant power, `F = 2,750,000 / v` (force in N, v in m/s)

Acceleration at standstill: `698,000 / 188,000 = 3.71 m/s^2`

## Freight Cars

### Boxcar (standard 50 ft)

| Parameter | Value |
|---|---|
| Length (over couplers) | 16.2 m |
| Body length (interior) | 15.2 m |
| Width | 3.2 m |
| Height (rail to roof) | 4.8 m |
| Max loaded weight | 120 t |
| Bogie spacing | 12.5 m |

### Gondola (open-top, 53 ft)

| Parameter | Value |
|---|---|
| Length (over couplers) | 16.8 m |
| Width | 3.1 m |
| Height (rail to top of sides) | 3.4 m |

### Flatcar (60 ft)

| Parameter | Value |
|---|---|
| Length (over couplers) | 18.9 m |
| Width | 3.1 m |
| Deck height above rail | 1.2 m |

### Hopper (covered, 3-bay grain)

| Parameter | Value |
|---|---|
| Length (over couplers) | 16.2 m |
| Width | 3.2 m |
| Height (rail to roof) | 4.7 m |
| Max loaded weight | 120 t |

## Track and Wheels

| Parameter | Value |
|---|---|
| Standard gauge | 1,435 mm |
| Wheel diameter (new) | 914 mm |
| Truck wheelbase (2-axle freight truck) | 1,778 mm |

## Coupling

| Parameter | Value |
|---|---|
| Coupler type | AAR Type E/F knuckle |
| Gap between car ends (coupled) | 100-150 mm |
| Total distance between car bodies | ~1.0-1.2 m |
| Coupler height above rail | 876 mm |
| Slack per coupling | 25-50 mm |

## Speed

| Parameter | Value |
|---|---|
| Max speed (intermodal freight) | 112 km/h (31 m/s) |
| Max speed (bulk freight) | 80-97 km/h (22-27 m/s) |
| Typical operating speed | 100 km/h (28 m/s) |

## Braking

| Parameter | Value |
|---|---|
| Service braking deceleration (loaded) | 0.2 m/s^2 |
| Service braking deceleration (empty) | 0.5 m/s^2 |
| Emergency braking deceleration (loaded) | 0.5 m/s^2 |
| Emergency braking deceleration (empty) | 0.7-1.0 m/s^2 |

### Derived stopping distances (service braking, loaded)

Using `d = v^2 / (2a)` with `a = 0.2 m/s^2`:

| Speed | Stopping distance |
|---|---|
| 28 m/s (100 km/h) | 1,960 m |
| 22 m/s (80 km/h) | 1,210 m |
| 14 m/s (50 km/h) | 490 m |

### Derived stopping distances (emergency braking, loaded)

Using `d = v^2 / (2a)` with `a = 0.5 m/s^2`:

| Speed | Stopping distance |
|---|---|
| 28 m/s (100 km/h) | 784 m |
| 22 m/s (80 km/h) | 484 m |
| 14 m/s (50 km/h) | 196 m |

## Key Ratios

These ratios are useful for ensuring game proportions feel correct regardless of absolute scale.

| Ratio | Value |
|---|---|
| Locomotive length : Boxcar length | 1.38 : 1 |
| Locomotive length : Width | 7.0 : 1 |
| Boxcar length : Width | 5.1 : 1 |
| Coupling gap : Car length | ~1 : 135 |
| Gauge : Wheel diameter | 1.57 : 1 |
| Gauge : Locomotive width | 0.45 : 1 |
| Stopping distance (100 km/h, service) : Train length (~20 cars) | ~5.6 : 1 |

## Sources

- AAR Manual of Standards and Recommended Practices (Sections E, G)
- EMD/Progress Rail SD70ACe product specifications
- UIC leaflet 544 (braking performance)
- AREMA standards (track gauge)
