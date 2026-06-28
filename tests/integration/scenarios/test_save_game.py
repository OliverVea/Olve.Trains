"""Tests that the save-game command writes a faithful snapshot of live game state to disk."""
from __future__ import annotations

import json
from pathlib import Path

from game import Game

SAVE_NAME = "integration-test-save"


def test_save_game_writes_faithful_snapshot(game: Game) -> None:
    """Mutate money and time, save, and assert the written file matches the live state."""

    # Mutate state away from the new-game defaults.
    game.place_track(
        start="15.5,0.125,12.5",
        end="19.5,0.125,12.5",
        start_dir="east",
        end_dir="east",
    )  # spends money
    game.set_time("08:30")
    game.step(2)  # flush commands; ManualStepper only advances time on step()

    # Read the authoritative live state. No step() between these and the save,
    # so the clock does not advance in between.
    balance = json.loads(game.send("query-money").output)["balance"]
    current_time = game.query_time()["value"]

    path = Path(game.save_game("manual", SAVE_NAME))
    try:
        assert path.exists(), f"Save file was not written to {path}"

        data = json.loads(path.read_text())

        assert data["version"] == 1
        assert data["money"] == balance

        # currentGameHours is total elapsed; its time-of-day component must match the live clock.
        assert abs((data["time"]["currentGameHours"] % 24) - current_time) < 0.01

        # Terrain and environmental objects are materialized in full, not regenerated from a seed.
        assert data["terrain"]["width"] == 50
        assert data["terrain"]["length"] == 50
        assert len(data["terrain"]["heights"]) == 50 * 50
        assert len(data["environment"]["objects"]) > 0

        assert data["camera"]["orthographicSize"] > 0
    finally:
        path.unlink(missing_ok=True)

    game.assert_no_errors()
