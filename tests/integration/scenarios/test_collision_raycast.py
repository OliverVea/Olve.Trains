from __future__ import annotations

from game import Game

Y = "0.125"


def _build_y_junction(game: Game) -> tuple[list[str], list[str], list[str]]:
    """Build a Y-junction: Track A (input from west), Track B (output north), Track C (output east).

    Returns (track_a_ids, track_b_ids, track_c_ids).
    """
    track_a = game.place_track(
        start=f"0,{Y},0",
        end=f"4,{Y},0",
        start_dir="east",
        end_dir="east",
    )

    track_b = game.place_track(
        start=f"4,{Y},0",
        end=f"4,{Y},4",
        start_dir="north",
        end_dir="north",
    )

    track_c = game.place_track(
        start=f"4,{Y},0",
        end=f"8,{Y},0",
        start_dir="east",
        end_dir="east",
    )

    # Step to let event chain propagate (track → junction → signal → collider registration)
    game.step(10)

    return track_a, track_b, track_c


def _find_signal_junction(game: Game) -> str:
    """Find the junction with a signal (≥3 connections)."""
    junctions = game.list_junctions()
    signal_junctions = [j for j in junctions if j.has_signal]
    assert len(signal_junctions) == 1, (
        f"Expected exactly 1 signal junction, found {len(signal_junctions)}"
    )
    return signal_junctions[0]


def test_signal_raycast(game: Game) -> None:
    """Raycast from projected signal screen position should detect a signal collider."""
    _build_y_junction(game)
    junction_info = _find_signal_junction(game)
    tile_x, tile_z = junction_info.position

    # Signal world position: tile center + signal mesh offset (0.3, 0, 0.3)
    signal_x = tile_x + 0.5 + 0.3
    signal_y = float(Y)
    signal_z = tile_z + 0.5 + 0.3

    # Zoom camera onto the junction area
    game.set_camera(target=f"{tile_x + 0.5},0,{tile_z + 0.5}", zoom=5)
    game.step(2)

    # Project signal 3D position to normalized screen coordinates
    screen_x, screen_y = game.project_to_screen(signal_x, signal_y, signal_z)

    # Sanity: screen coords should be within the viewport
    assert -1 <= screen_x <= 1, f"Signal screen X {screen_x} out of viewport"
    assert -1 <= screen_y <= 1, f"Signal screen Y {screen_y} out of viewport"

    # Raycast at the signal's screen position
    hits = game.raycast(screen_x, screen_y)
    signal_hits = [h for h in hits if h.group == "Signal"]
    assert len(signal_hits) > 0, (
        f"Expected a signal hit at screen ({screen_x:.4f}, {screen_y:.4f}) "
        f"for world pos ({signal_x}, {signal_y}, {signal_z}), "
        f"but got hits: {hits}"
    )


def test_raycast_miss(game: Game) -> None:
    """Raycast at an empty area should not hit any signal colliders."""
    _build_y_junction(game)

    # Point camera at an area far from any signal (origin, no tracks)
    game.set_camera(target="-10,0,-10", zoom=5)
    game.step(2)

    # Raycast at screen center — should be empty terrain
    hits = game.raycast(0, 0)
    signal_hits = [h for h in hits if h.group == "Signal"]
    assert len(signal_hits) == 0, (
        f"Expected no signal hits at screen center away from junction, "
        f"but got: {signal_hits}"
    )
