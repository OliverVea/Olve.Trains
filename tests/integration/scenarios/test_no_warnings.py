"""Test that the menu -> game -> exit flow produces no warnings."""

from __future__ import annotations

import pytest

from game import Game


def test_no_warnings_on_game_start(game: Game) -> None:
    """Load menu, then game scene, and assert no warnings in logs.

    Checks from the beginning of the log (before the fixture's mark)
    to cover startup and scene transitions.
    """
    game._log_mark = 0  # check from start of log, including startup

    game.load_scene("menu")
    game.step(2)

    game.load_scene("loading")
    game.step(10)  # loading scene auto-transitions to game after background task completes

    game.load_scene("game")
    game.step(2)

    game.assert_no_warnings(ignore=[
        "shrink logic is not implemented",  # TODO: implement layout shrink logic
    ])


def test_assert_no_warnings_catches_warning(game: Game) -> None:
    """Verify that assert_no_warnings actually detects emitted warnings."""
    game.send("log level=warn message=test-warning-sentinel")
    game.step(1)

    with pytest.raises(AssertionError, match="test-warning-sentinel"):
        game.assert_no_warnings()


def test_assert_no_errors_catches_error(game: Game) -> None:
    """Verify that assert_no_errors actually detects emitted errors."""
    game.send("log level=error message=test-error-sentinel")
    game.step(1)

    with pytest.raises(AssertionError, match="test-error-sentinel"):
        game.assert_no_errors()
