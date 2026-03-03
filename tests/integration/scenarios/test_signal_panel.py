"""Tests for the signal rules GUI panel."""

from __future__ import annotations

from conftest import ScreenshotComparer
from game import Game

Y = "0.125"


def _build_y_junction(game: Game) -> None:
    """Build a Y-junction: three tracks meeting at tile (4,0)."""
    game.place_track(
        start=f"0,{Y},0",
        end=f"4,{Y},0",
        start_dir="east",
        end_dir="east",
    )
    game.place_track(
        start=f"4,{Y},0",
        end=f"4,{Y},4",
        start_dir="north",
        end_dir="north",
    )
    game.place_track(
        start=f"4,{Y},0",
        end=f"8,{Y},0",
        start_dir="east",
        end_dir="east",
    )
    game.step(10)


def _find_signal_junction(game: Game) -> tuple[str, int, int]:
    """Find the junction with a signal and return (junction_id, tile_x, tile_z)."""
    junctions = game.list_junctions()
    signal_junctions = [j for j in junctions if j.has_signal]
    assert len(signal_junctions) == 1, (
        f"Expected exactly 1 signal junction, found {len(signal_junctions)}"
    )
    j = signal_junctions[0]
    return j.junction_id, j.position[0], j.position[1]


def test_signal_panel(
    game: Game,
    screenshots: ScreenshotComparer,
) -> None:
    """Click a signal to open the signal rules panel and verify it renders."""
    _build_y_junction(game)
    junction_id, tile_x, tile_z = _find_signal_junction(game)

    # Signal world position: tile center + signal mesh offset (0.3, 0, 0.3)
    signal_x = tile_x + 0.5 + 0.3
    signal_y = float(Y)
    signal_z = tile_z + 0.5 + 0.3

    # Zoom camera onto the junction area
    game.set_camera(target=f"{tile_x + 0.5},0,{tile_z + 0.5}", zoom=5)
    game.step(2)

    # Project signal 3D position to normalized screen coordinates
    screen_x, screen_y = game.project_to_screen(signal_x, signal_y, signal_z)

    # Click at the signal's screen coordinates to open the panel
    game.click(screen_x, screen_y)
    game.step(5)

    path = game.screenshot(game.temp_dir / "signal-panel.png")
    screenshots.compare(path, "signal-panel")

    # Dismiss the panel by clicking the overlay (center-left of screen, away from panel)
    game.click(-0.5, 0.0)
    game.step(5)
