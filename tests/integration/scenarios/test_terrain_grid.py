"""Tests for terrain grid highlight toggling with tool selection."""

from __future__ import annotations

from conftest import ScreenshotComparer
from game import Game


def test_terrain_grid_with_track_tool(
    game: Game,
    screenshots: ScreenshotComparer,
) -> None:
    """Selecting the track tool and aiming at screen center should render the grid overlay."""
    game.set_camera(target="25,0,25", zoom=3)
    game.step(2)

    # Select track placement tool (enables grid highlight)
    game.select_tool("Place Tracks")
    game.step(2)

    # Move mouse to screen center (triggers terrain raycast → grid appears)
    game.set_mouse(0, 0)
    game.step(2)

    path = game.screenshot(game.temp_dir / "terrain-grid-with-tool.png")
    screenshots.compare(path, "terrain-grid-with-tool")
