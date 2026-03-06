from __future__ import annotations

from conftest import ScreenshotComparer
from game import Game


def test_industry_buildings(
    game: Game,
    screenshots: ScreenshotComparer,
) -> None:
    game.set_camera(target="5,0,5", zoom=1.5)
    game.set_time("11:30")
    game.step(2)

    # Place one of each industry type
    game.place_building(pos="2,0,2", type="forest", dir="north")
    game.place_building(pos="5,0,2", type="mine", dir="north")
    game.place_building(pos="8,0,2", type="sawmill", dir="east")

    # Place a station and residential for comparison
    game.place_building(pos="2,0,6", type="station", dir="north")
    game.place_building(pos="7,0,6", type="residential", dir="north")

    game.step(2)

    path = game.screenshot(game.temp_dir / "industry-buildings.png")
    screenshots.compare(path, "industry-buildings")
