"""Tests that load-game rebuilds the game from a saved snapshot, not from a regenerated seed."""
from __future__ import annotations

import json
from pathlib import Path

from game import Game

SAVE_NAME = "integration-test-load-save"


def _environment_ids(game: Game) -> set[str]:
    """The set of environmental-object ids currently in the world (whole-map query)."""
    data = json.loads(game.send("list-entities type=environment").output)
    return {entity["entityId"] for entity in data["entities"]}


def test_load_game_restores_snapshot(game: Game) -> None:
    """Mutate state, save, then load and assert money, time, and environment are restored."""

    # Mutate state away from the new-game defaults.
    game.place_track(
        start="15.5,0.125,12.5",
        end="19.5,0.125,12.5",
        start_dir="east",
        end_dir="east",
    )  # spends money
    game.set_time("08:30")
    game.step(2)  # flush commands; ManualStepper only advances time on step()

    # Authoritative live state, captured immediately before saving (no step in between).
    saved_balance = json.loads(game.send("query-money").output)["balance"]
    saved_time = game.query_time()["value"]
    saved_env_ids = _environment_ids(game)
    assert saved_env_ids, "Expected the new game to contain environmental objects"

    path = Path(game.save_game("manual", SAVE_NAME))
    try:
        assert path.exists(), f"Save file was not written to {path}"

        # Tear down the running game and rebuild it from the save, through the loading scene.
        game.load_game("manual", SAVE_NAME)

        # Money is exact — it does not advance with the clock.
        loaded_balance = json.loads(game.send("query-money").output)["balance"]
        assert loaded_balance == saved_balance

        # The environment must be the *same* objects, by id. Restoring the saved list preserves ids;
        # regenerating from a seed would mint new ones. This is the load path's central guarantee.
        assert _environment_ids(game) == saved_env_ids

        # Time of day is restored, drifting forward only by the handful of frames stepped during load.
        loaded_time = game.query_time()["value"]
        assert 0.0 <= (loaded_time - saved_time) < 0.5, (
            f"loaded time {loaded_time} was not restored from saved {saved_time}"
        )
    finally:
        path.unlink(missing_ok=True)

    game.assert_no_errors()
