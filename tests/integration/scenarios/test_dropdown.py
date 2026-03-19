"""Tests for dropdown widget expand/collapse."""

from __future__ import annotations

from game import Game


DROPDOWN_ID = "DayTimePanel/Dropdown/SkyColorDropdown"


def test_dropdown_toggle_via_button(game: Game) -> None:
    """Expand, collapse via button click, expand again."""
    game.step(2)

    game.activate_gui(DROPDOWN_ID)
    game.step(2)

    game.activate_gui(DROPDOWN_ID)
    game.step(2)

    game.activate_gui(DROPDOWN_ID)
    game.step(2)

    game.assert_no_errors()


def test_dropdown_collapse_via_click_outside(game: Game) -> None:
    """Expand, collapse by clicking outside, expand again."""
    game.step(2)

    game.activate_gui(DROPDOWN_ID)
    game.step(2)

    # Click center of screen — far from the dropdown, hits the overlay
    game.click(0, 0)
    game.step(5)

    game.activate_gui(DROPDOWN_ID)
    game.step(5)

    game.assert_no_errors()
