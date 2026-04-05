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
    """Forest produces Wood over time, scaled by nearby tree productivity."""
    forest_id = game.place_building(pos="5,0,5", type="forest", dir="north")

    # No production immediately
    assert _get_inventory_amount(game, forest_id, "Wood") == 0

    # After enough time, the forest should have produced some wood.
    # Base interval is 3s (180 frames), but nearby trees scale the rate.
    game.step(360)
    amount = _get_inventory_amount(game, forest_id, "Wood")
    assert amount > 0, "Forest near trees should produce wood"
