from __future__ import annotations

from conftest import ScreenshotAsserter
from game import Game

Y = "0.125"


def test_track_loop(
    game: Game,
    screenshots: ScreenshotAsserter,
) -> None:
    # Set camera to center on the layout
    game.set_camera(target="12.5,0,10", zoom=2.5)

    # Place station at tile (11,1,12) facing north
    game.place_building(pos="11,1,12", type="station", dir="north")

    # Place residential building down-right of station
    game.place_building(pos="10,1,13", type="residential", dir="north")

    # -- Track loop --

    # Top-right curve: from station end going east, curving south
    track1_ids = game.place_track(
        start=f"15.5,{Y},12.5",
        end=f"17.5,{Y},10.5",
        start_dir="east",
        end_dir="south",
    )
    assert len(track1_ids) >= 1, "Expected at least 1 track segment"

    # Bottom-right curve: south, curving west
    track2_ids = game.place_track(
        start=f"17.5,{Y},10.5",
        end=f"15.5,{Y},8.5",
        start_dir="south",
        end_dir="west",
    )
    assert len(track2_ids) >= 1

    # Bottom straight: west
    game.place_track(
        start=f"15.5,{Y},8.5",
        end=f"10.5,{Y},8.5",
        start_dir="west",
        end_dir="west",
    )

    # Bottom-left curve: west, curving north
    track3_ids = game.place_track(
        start=f"10.5,{Y},8.5",
        end=f"8.5,{Y},10.5",
        start_dir="west",
        end_dir="north",
    )
    assert len(track3_ids) >= 1

    # Top-left curve: north, curving east (completing the loop)
    game.place_track(
        start=f"8.5,{Y},10.5",
        end=f"10.5,{Y},12.5",
        start_dir="north",
        end_dir="east",
    )

    # Y-junction branch off bottom-left
    game.place_track(
        start=f"10.5,{Y},8.5",
        end=f"8.5,{Y},8.5",
        start_dir="west",
        end_dir="west",
    )

    # -- Vehicles --

    game.place_vehicle(track=track1_ids[0], speed=3)
    game.place_vehicle(track=track2_ids[0], speed=3)
    game.place_vehicle(track=track3_ids[0], speed=3)

    # Simulate 5 seconds at 60fps
    game.step(300)

    # Select track placement tool (shows arrow indicator)
    game.select_tool("Place Tracks")
    game.step(2)

    # Move mouse to center (shows grid overlay)
    game.set_mouse(0, 0)
    game.step(2)

    # -- Time-of-day screenshots --

    for time_str, label in [("7:30", "0730"), ("11:30", "1130"), ("22:30", "2230")]:
        game.set_time(time_str)
        game.step(2)

        path = game.screenshot(game.temp_dir / f"{label}.png")
        screenshots.assert_matches(path, f"track-loop-{label}")
