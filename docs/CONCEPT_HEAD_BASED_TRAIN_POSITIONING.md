# Concept: Head-based train positioning with conservative lead distance

**Success:** Yes/No

**Findings:**
- (none yet)

## Core concept

The **train position** (the value stored in `TrainTrackPosition`) becomes the position of an invisible **head point** that runs ahead of the visible locomotive. The locomotive and wagons trail behind the head.

## Physics model

Given:
- `v` = current speed
- `a_brake` = maximum braking deceleration
- `k_lead = 0.5` = lead safety factor (use only 50% of braking capability for lead calculation)

The lead distance (head ahead of locomotive center):

```
a_effective = a_brake * k_lead
t_brake = v / a_effective
d_lead = v^2 / (2 * a_effective)
```

At rest (`v = 0`), `d_lead = 0` — the head sits at the locomotive.

## Body positioning

From the head position, working backwards along the track:

| Entity | Offset behind head |
|--------|-------------------|
| Locomotive center | `d_lead` |
| Wagon 0 center | `d_lead + loco_half + gap + wagon_0_half` |
| Wagon i center | `d_lead + loco_half + gap + sum(wagon_j_length + gap) + wagon_i_half` |

This replaces the current `WagonPositioningService` logic which offsets from the locomotive position. Instead, everything offsets from the head.

## Braking to stop

When approaching a stop target:
1. `TrainStopTargetService` computes distance from **head** to stop point
2. Since the lead was computed with `k_lead = 0.5` (only 50% of real braking), the train has **2x the room it needs** to stop
3. Actual braking uses full `a_brake` (or a modulated fraction) — plenty of margin
4. Can PWM the braking: apply `a_brake * factor` where `factor` varies to get smooth deceleration without needing a perfect continuous curve
5. The head stops at the station point; the locomotive naturally ends up `d_lead` behind it — but since `d_lead -> 0` as `v -> 0`, the locomotive arrives at the station point

## Key benefit

The stop logic becomes trivial: just stop the head at the target point. No need for precise braking curves — the 2x safety margin means any reasonable braking strategy works. The locomotive visually arrives at the station because `d_lead -> 0` as speed drops.
