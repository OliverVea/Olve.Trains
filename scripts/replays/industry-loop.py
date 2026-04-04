#!/usr/bin/env python3
"""Replay: Industry loop — forest + sawmill connected by rail with trains."""
from __future__ import annotations

import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent.parent / "tests" / "integration"))

from game import Game

Y = "0.125"


def main() -> None:
    game = Game(
        instance_id="replay",
        resolution="1280x720",
        skip_build=True,
        scene="game",
        manual=False,
    )

    try:
        game.start()

        # Camera
        game.set_camera(target="10,0,17", zoom=3.5)

        # === Buildings ===
        depot_id = game.place_building(pos="9,1,24", type="depot", dir="north")
        game.place_building(pos="9,1,20", type="station", dir="north")
        game.place_building(pos="10,1,14", type="forest", dir="north")
        game.place_building(pos="9,1,11", type="station", dir="north")
        game.place_building(pos="10,1,9", type="sawmill", dir="north")

        # === Tracks ===

        # Top-right curve: depot area east, curving south
        game.place_track(start=f"13.5,{Y},24.5", end=f"15.5,{Y},22.5", start_dir="east", end_dir="south")

        # Right side straight: south from 22.5 to 18.5
        game.place_track(start=f"15.5,{Y},22.5", end=f"15.5,{Y},18.5", start_dir="south", end_dir="south")

        # Mid-right curve: top station east, curving south
        game.place_track(start=f"13.5,{Y},20.5", end=f"15.5,{Y},18.5", start_dir="east", end_dir="south")

        # Return curve: top station east, curving north
        game.place_track(start=f"13.5,{Y},20.5", end=f"15.5,{Y},22.5", start_dir="east", end_dir="north")

        # Left from top station: west
        game.place_track(start=f"8.5,{Y},20.5", end=f"7.5,{Y},20.5", start_dir="west", end_dir="west")
        game.place_track(start=f"7.5,{Y},20.5", end=f"6.5,{Y},20.5", start_dir="west", end_dir="west")

        # Bottom-left curve: west curving south
        game.place_track(start=f"6.5,{Y},20.5", end=f"4.5,{Y},22.5", start_dir="west", end_dir="south")

        # Left loop back up: south curving east
        game.place_track(start=f"4.5,{Y},22.5", end=f"8.5,{Y},24.5", start_dir="north", end_dir="east")

        # Left Y-branch: top station west curving south
        game.place_track(start=f"8.5,{Y},20.5", end=f"6.5,{Y},18.5", start_dir="west", end_dir="south")

        # Left loop closure: bottom-left to Y-branch
        game.place_track(start=f"4.5,{Y},22.5", end=f"6.5,{Y},18.5", start_dir="south", end_dir="south")

        # Bottom-right: bottom station east curving north
        game.place_track(start=f"13.5,{Y},11.5", end=f"15.5,{Y},13.5", start_dir="east", end_dir="north")

        # Right side straight: north from 13.5 to 18.5
        game.place_track(start=f"15.5,{Y},13.5", end=f"15.5,{Y},18.5", start_dir="north", end_dir="north")

        # Bottom-left: bottom station west curving north
        game.place_track(start=f"8.5,{Y},11.5", end=f"6.5,{Y},13.5", start_dir="west", end_dir="north")

        # Left side straight: north from 13.5 to 18.5
        game.place_track(start=f"6.5,{Y},13.5", end=f"6.5,{Y},18.5", start_dir="north", end_dir="north")

        # === Trains ===

        # Train 1: on depot with 4 goods wagons, speed 2
        train1_id = game.place_train(depot=depot_id, speed=2)
        for _ in range(4):
            game.add_wagon(train1_id, "goods")

        # Train 2: on depot with 2 goods wagons, speed 2
        train2_id = game.place_train(depot=depot_id, speed=2)
        for _ in range(2):
            game.add_wagon(train2_id, "goods")

        print("Replay complete. Close the window to exit.")

        # Keep alive until game exits
        while game._game_proc and game._game_proc.poll() is None:
            time.sleep(1)

    except KeyboardInterrupt:
        print("Interrupted")
    finally:
        game.stop()


if __name__ == "__main__":
    main()
