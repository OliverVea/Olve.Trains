"""
Resource productivity simulation.

Model:
  - Each resource entity is owned by the nearest consumer (producer) within range.
  - Per-resource efficiency: E = N_P^(k-1) * (1 - (d / range)^2)
    where N_P = number of resources owned by producer P,
          d = distance from producer to resource,
          k = diminishing returns exponent (e.g. 5/6)
  - Total productivity for a producer = sum of E over all owned resources.
  - Total system productivity = sum over all producers.
"""

import math
import random

RANGE = 10.0
K = 5 / 6


def distance(a, b):
    return math.sqrt((a[0] - b[0]) ** 2 + (a[1] - b[1]) ** 2)


def assign_resources(producers, resources, max_range):
    """Assign each resource to nearest producer within range. Returns dict: producer_idx -> list of distances."""
    ownership = {i: [] for i in range(len(producers))}
    for res in resources:
        best_dist = float("inf")
        best_producer = None
        for i, prod in enumerate(producers):
            d = distance(prod, res)
            if d <= max_range and d < best_dist:
                best_dist = d
                best_producer = i
        if best_producer is not None:
            ownership[best_producer].append(best_dist)
    return ownership


def productivity(ownership, k=K, max_range=RANGE):
    """Compute per-producer and total productivity."""
    per_producer = {}
    for idx, distances in ownership.items():
        n = len(distances)
        if n == 0:
            per_producer[idx] = 0.0
            continue
        count_factor = n ** (k - 1)  # N^(k-1)
        dist_sum = sum(1 - (d / max_range) ** 2 for d in distances)
        per_producer[idx] = count_factor * dist_sum
    return per_producer


def scatter_resources(n, area_size=20.0, seed=42):
    rng = random.Random(seed)
    return [(rng.uniform(-area_size / 2, area_size / 2), rng.uniform(-area_size / 2, area_size / 2)) for _ in range(n)]


def run_scenario(name, producers, resources):
    ownership = assign_resources(producers, resources, RANGE)
    prod = productivity(ownership)
    total = sum(prod.values())
    print(f"\n=== {name} ===")
    for i, p in prod.items():
        n_owned = len(ownership[i])
        print(f"  Producer {i} at {producers[i]}: owns {n_owned} resources, productivity = {p:.3f}")
    print(f"  TOTAL productivity: {total:.3f}")
    return total


def main():
    print(f"Parameters: range={RANGE}, k={K}")

    # Fixed resource field
    resources = scatter_resources(50, area_size=18.0)
    print(f"\n{len(resources)} resources scattered in 18x18 area")

    # --- Scenario 1: Single producer at center ---
    t1 = run_scenario("1 producer at center", [(0, 0)], resources)

    # --- Scenario 2: Two producers, spread out ---
    t2 = run_scenario("2 producers spread", [(-4, 0), (4, 0)], resources)

    # --- Scenario 3: Two producers, same spot ---
    t2_same = run_scenario("2 producers same spot", [(0, 0), (0, 0.1)], resources)

    # --- Scenario 4: Three producers ---
    t3 = run_scenario("3 producers spread", [(-5, 0), (0, 0), (5, 0)], resources)

    # --- Summary ---
    print("\n=== SUMMARY ===")
    print(f"1 producer:              {t1:.3f}")
    print(f"2 producers (spread):    {t2:.3f} ({t2/t1*100:.1f}% of 1 producer)")
    print(f"2 producers (same spot): {t2_same:.3f} ({t2_same/t1*100:.1f}% of 1 producer)")
    print(f"3 producers (spread):    {t3:.3f} ({t3/t1*100:.1f}% of 1 producer)")

    # --- Monte Carlo: 2 producers always >= 1 producer? ---
    print("\n=== MONTE CARLO: Is 2 always >= 1? ===")
    violations = 0
    ratios = []
    trials = 1000
    for seed in range(trials):
        res = scatter_resources(50, area_size=18.0, seed=seed)

        own1 = assign_resources([(0, 0)], res, RANGE)
        t_one = sum(productivity(own1).values())

        own2 = assign_resources([(-4, 0), (4, 0)], res, RANGE)
        t_two = sum(productivity(own2).values())

        if t_two < t_one:
            violations += 1
        if t_one > 0:
            ratios.append(t_two / t_one)

    avg_ratio = sum(ratios) / len(ratios)
    min_ratio = min(ratios)
    max_ratio = max(ratios)
    print(f"  Trials: {trials}")
    print(f"  Violations (2 < 1): {violations}")
    print(f"  Ratio 2/1: avg={avg_ratio:.3f}, min={min_ratio:.3f}, max={max_ratio:.3f}")


if __name__ == "__main__":
    main()
