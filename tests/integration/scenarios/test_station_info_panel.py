"""Tests for the station info panel showing nearby industry inventories."""

from __future__ import annotations

from conftest import ScreenshotComparer
from game import Game

Y = "0.125"


def test_station_info_panel(
    game: Game,
    screenshots: ScreenshotComparer,
) -> None:
    """Place a station with 2 in-range and 1 out-of-range industry, click station to open panel."""
    # Station range is 5 tiles (Chebyshev distance)
    # Station at (5,0,5)
    game.place_building(pos="5,0,5", type="station", dir="north")

    # In-range industries (within 5 tiles of station)
    game.place_building(pos="3,0,3", type="forest", dir="north")  # distance ~2
    game.place_building(pos="8,0,5", type="sawmill", dir="east")  # distance ~3

    # Out-of-range industry (beyond 5 tiles)
    game.place_building(pos="0,0,12", type="mine", dir="north")  # distance ~7 from station

    game.step(5)

    # Zoom camera onto the station
    # Station at (5,0,5) with 4x2 footprint facing north, center roughly (7,0,6)
    game.set_camera(target="7,0,6", zoom=5)
    game.set_time("11:30")
    game.step(2)

    # Project station building center to screen coordinates and click it
    screen_x, screen_y = game.project_to_screen(7, 0.5, 6)
    game.click(screen_x, screen_y)
    game.step(5)

    path = game.screenshot(game.temp_dir / "station-info-panel.png")
    screenshots.compare(path, "station-info-panel")

    # Dismiss by clicking overlay
    game.click(-0.5, 0.0)
    game.step(5)
