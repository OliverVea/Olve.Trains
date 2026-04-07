"""Test that the loading scene transitions correctly to the game scene."""

from __future__ import annotations

from game import Game


def test_loading_scene_transitions_to_game(game: Game) -> None:
    """Load the loading scene and verify it auto-transitions to the game scene.

    The loading scene starts a background task that builds GameSceneArguments.
    Since the task is trivial (returns defaults), it completes near-instantly
    and the loading scene transitions to the game scene within a few frames.

    Verifies the transition by running game-scene-only commands after stepping.
    """
    game.load_scene("loading")
    game.step(10)  # loading scene auto-transitions to game

    # These commands only work in the game scene — success proves the transition happened
    game.set_camera(target="5,0,5", zoom=5)
    game.step(1)

    game.assert_no_errors()
    game.assert_no_warnings(ignore=[
        "shrink logic is not implemented",
    ])


def test_loading_scene_round_trip(game: Game) -> None:
    """Verify menu -> loading -> game -> menu -> loading -> game works."""
    game.load_scene("menu")
    game.step(2)

    game.load_scene("loading")
    game.step(10)

    # Verify we're in the game scene
    game.set_camera(target="5,0,5", zoom=5)
    game.step(1)

    # Go back to menu
    game.load_scene("menu")
    game.step(2)

    # And through loading again
    game.load_scene("loading")
    game.step(10)

    game.set_camera(target="5,0,5", zoom=5)
    game.step(1)

    game.assert_no_errors()
    game.assert_no_warnings(ignore=[
        "shrink logic is not implemented",
    ])
