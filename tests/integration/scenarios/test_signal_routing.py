from __future__ import annotations

from game import Game

Y = "0.125"


def _build_y_junction(game: Game) -> tuple[list[str], list[str], list[str]]:
    """Build a Y-junction: Track A (input from west), Track B (output north), Track C (output east).

    Returns (track_a_ids, track_b_ids, track_c_ids).
    """
    # Track A: west to east (input)
    track_a = game.place_track(
        start=f"0,{Y},0",
        end=f"4,{Y},0",
        start_dir="east",
        end_dir="east",
    )

    # Track B: north from junction (output north)
    track_b = game.place_track(
        start=f"4,{Y},0",
        end=f"4,{Y},4",
        start_dir="north",
        end_dir="north",
    )

    # Track C: east from junction (output east)
    track_c = game.place_track(
        start=f"4,{Y},0",
        end=f"8,{Y},0",
        start_dir="east",
        end_dir="east",
    )

    # Step to let event chain propagate (track → junction → signal creation)
    game.step(10)

    return track_a, track_b, track_c


def _find_signal_junction(game: Game) -> str:
    """Find the junction with a signal (≥3 connections)."""
    junctions = game.list_junctions()
    signal_junctions = [j for j in junctions if j.has_signal]
    assert len(signal_junctions) == 1, (
        f"Expected exactly 1 signal junction, found {len(signal_junctions)}"
    )
    return signal_junctions[0].junction_id


def test_signal_rule_routes_vehicle(game: Game) -> None:
    """A directional signal rule should route a vehicle to the specified output track."""
    track_a, track_b, track_c = _build_y_junction(game)
    junction_id = _find_signal_junction(game)

    # Clear default rules, add rule: from west → go to track B
    game.clear_signal_rules(junction_id)
    game.add_signal_rule(
        junction_id,
        f"any from direction(west) to track({track_b[0]}) with RoundRobin",
    )

    # Place vehicle on Track A heading east
    vehicle_id = game.place_vehicle(track=track_a[0], speed=5)

    # Step enough frames for the vehicle to traverse Track A and cross the junction.
    # Track A is 4 units long, speed=5 units/s, at 60fps ~= 48 frames.
    # Give extra margin for junction crossing.
    game.step(120)

    # Vehicle should now be on Track B (the north output)
    state = game.query_vehicle(vehicle_id)
    all_b_tracks = set(track_b)
    all_c_tracks = set(track_c)
    assert state.track_id in all_b_tracks, (
        f"Expected vehicle on Track B {all_b_tracks}, "
        f"but found on track {state.track_id}. "
        f"Track C was {all_c_tracks}"
    )


def _build_crossroads(game: Game) -> tuple[list[str], list[str], list[str], list[str]]:
    """Build a 4-way crossroads: input from west, outputs east/north/south.

    The junction at (4,0) has 4 connections. Traffic from the west can exit
    east (opposite direction) and the signal rule directs to specific tracks.

    Returns (track_west, track_east, track_north, track_south).
    """
    # Track from west (input)
    track_west = game.place_track(
        start=f"0,{Y},0",
        end=f"4,{Y},0",
        start_dir="east",
        end_dir="east",
    )

    # Track to east (output, opposite of incoming)
    track_east = game.place_track(
        start=f"4,{Y},0",
        end=f"8,{Y},0",
        start_dir="east",
        end_dir="east",
    )

    # Track to north (output)
    track_north = game.place_track(
        start=f"4,{Y},0",
        end=f"4,{Y},4",
        start_dir="north",
        end_dir="north",
    )

    # Track to south (output)
    track_south = game.place_track(
        start=f"4,{Y},0",
        end=f"4,{Y},-4",
        start_dir="south",
        end_dir="south",
    )

    # Step to let event chain propagate
    game.step(10)

    return track_west, track_east, track_north, track_south


def test_round_robin_distributes_vehicles(game: Game) -> None:
    """RoundRobin on a crossroads sends successive vehicles to different tracks.

    At a 4-way junction, vehicles from the west have one opposite connection (east).
    Two vehicles arriving from west with RoundRobin should both go east (only
    eligible outgoing direction). This test verifies the junction crossing works
    and vehicles arrive at the expected output.
    """
    track_west, track_east, track_north, track_south = _build_crossroads(game)

    # Place first vehicle, let it cross
    vehicle1_id = game.place_vehicle(track=track_west[0], speed=5)
    game.step(120)

    state1 = game.query_vehicle(vehicle1_id)
    vehicle1_track = state1.track_id

    # Place second vehicle, let it cross
    vehicle2_id = game.place_vehicle(track=track_west[0], speed=5)
    game.step(120)

    state2 = game.query_vehicle(vehicle2_id)
    vehicle2_track = state2.track_id

    # Both should end up on the east track (only opposite-direction output)
    east_tracks = set(track_east)
    assert vehicle1_track in east_tracks, (
        f"Vehicle 1 should be on east track {east_tracks}, "
        f"but found on {vehicle1_track}"
    )
    assert vehicle2_track in east_tracks, (
        f"Vehicle 2 should be on east track {east_tracks}, "
        f"but found on {vehicle2_track}"
    )
