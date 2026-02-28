"""Tests for GUI menu interactions."""

from __future__ import annotations

from conftest import ScreenshotComparer
from game import Game


def test_burger_menu(
    game: Game,
    screenshots: ScreenshotComparer,
) -> None:
    """Open burger menu overlay and verify it renders correctly."""
    game.set_camera(target="12.5,0,10", zoom=2.5)
    game.step(2)

    game.activate_gui("InfoBar/Box/MenuButton")
    game.step(2)

    path = game.screenshot(game.temp_dir / "burger-menu.png")
    screenshots.compare(path, "burger-menu")

    # Dismiss burger menu
    game.activate_gui("BurgerMenu/Box/ResumeButton")
    game.step(2)
