"""Tests for industry production ticking."""

from __future__ import annotations

from game import Game


def _get_inventory_amount(game: Game, building_id: str, cargo_type: str) -> int:
    data = game.query_building(building_id)
    for item in data["industry"]["inventory"]:
        if item["cargoType"] == cargo_type:
            return item["amount"]
    raise KeyError(f"Cargo type '{cargo_type}' not found in building {building_id}")


def test_forest_production(game: Game) -> None:
    """Forest produces 1 Wood every 3s (~180 frames at 1/60s)."""
    forest_id = game.place_building(pos="5,0,5", type="forest", dir="north")

    # Production interval = 3s ≈ 180 frames. Use ~175 as "safely before" threshold.
    game.step(175)
    assert _get_inventory_amount(game, forest_id, "Wood") == 0

    # 10 more frames guarantees we cross the 180-frame boundary
    game.step(10)
    assert _get_inventory_amount(game, forest_id, "Wood") == 1

    # 3 more full cycles (~540 frames, use 545 for margin)
    game.step(545)
    assert _get_inventory_amount(game, forest_id, "Wood") == 4
