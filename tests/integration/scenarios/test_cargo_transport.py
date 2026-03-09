"""Tests for cargo loading/unloading at stations.

Layout:
    Forest (3,0,3)   Station A (5,0,5)    ----east connector----    Station B (5,0,15)   Sawmill (3,0,13)
                      track: 4.5→9.5 at z=5.5                      track: 4.5→9.5 at z=15.5
                              |                                              |
                          west connector (x=4.5)                    east connector (x=9.5)
                              |                                              |
                      --------+----------------------------------------------+--------

    Train circulates clockwise picking up wood at station A,
    delivering to the sawmill at station B, and picking up planks.
"""

from __future__ import annotations

from conftest import ScreenshotComparer
from game import Game


def _get_inventory(game: Game, building_id: str) -> dict[str, int]:
    data = game.query_building(building_id)
    return {
        item["cargoType"]: item["amount"]
        for item in data["industry"]["inventory"]
    }


def test_cargo_transport(game: Game, screenshots: ScreenshotComparer) -> None:
    """Wood flows: forest → station A → train → station B → sawmill → planks → train."""
    # Place stations and nearby industries (within range=5 tiles)
    # Tile Y=1 matches terrain height (heightmap defaults to 1, step=0.125 → world Y=0.125)
    game.place_building(pos="5,1,5", type="station", dir="north")
    forest_id = game.place_building(pos="3,1,3", type="forest", dir="north")

    game.place_building(pos="5,1,15", type="station", dir="north")
    sawmill_id = game.place_building(pos="3,1,13", type="sawmill", dir="north")

    # Connect station tracks into a rectangular loop.
    # Station tracks (auto-created, north-facing) run east at Y=0.125:
    #   Station A: (4.5, 0.125, 5.5) → (9.5, 0.125, 5.5)
    #   Station B: (4.5, 0.125, 15.5) → (9.5, 0.125, 15.5)
    east_tracks = game.place_track(
        start="9.5,0.125,5.5", end="9.5,0.125,15.5",
        start_dir="north", end_dir="north",
    )
    game.place_track(
        start="4.5,0.125,15.5", end="4.5,0.125,5.5",
        start_dir="south", end_dir="south",
    )
    game.step(10)  # let junctions propagate

    # Wait for forest to produce wood (3s interval = 180 frames)
    game.step(200)
    forest_inv = _get_inventory(game, forest_id)
    assert forest_inv["Wood"] >= 1, f"Forest should have produced wood, got {forest_inv}"

    # Place train on the east connector (not on a station track, so first
    # station entry triggers cargo transfer)
    train_id = game.place_train(track=east_tracks[0], speed=5)

    # Run for ~20 seconds of game time (1200 frames).
    # At speed=5 on a ~30-unit loop, the train completes ~3 full laps.
    # Forest produces ~6 more wood, sawmill processes wood→planks in 2s cycles.
    game.step(1200)

    # Screenshot the layout: center between the two stations, zoomed out
    game.set_camera(target="7,0,10", zoom=5)
    game.set_time("11:30")
    game.step(2)
    path = game.screenshot(game.temp_dir / "cargo-transport.png")
    screenshots.compare(path, "cargo-transport")

    # Verify the cargo chain worked:
    sawmill_inv = _get_inventory(game, sawmill_id)
    train_cargo = game.query_train_cargo(train_id)

    # The sawmill must have received wood (it either still has some or processed it all)
    wood_in_sawmill = sawmill_inv.get("Wood", 0)
    planks_in_sawmill = sawmill_inv.get("Planks", 0)
    planks_on_train = train_cargo.get("Planks", 0)

    # Sawmill having planks proves the full chain: forest→stationA→train→stationB→sawmill→planks
    total_planks = planks_in_sawmill + planks_on_train
    assert total_planks > 0, (
        f"Expected planks to exist somewhere. "
        f"Sawmill: {sawmill_inv}, Train cargo: {train_cargo}"
    )

    # Train should have picked up planks from station B
    assert planks_on_train > 0, (
        f"Expected train to carry planks. "
        f"Sawmill: {sawmill_inv}, Train cargo: {train_cargo}"
    )
