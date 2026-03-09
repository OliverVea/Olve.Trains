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


def test_signal_rule_routes_train(game: Game) -> None:
    """A directional signal rule should route a train to the specified output track."""
    track_a, track_b, track_c = _build_y_junction(game)
    junction_id = _find_signal_junction(game)

    # Clear default rules, add rule: from west → go to track B
    game.clear_signal_rules(junction_id)
    game.add_signal_rule(
        junction_id,
        f"any from direction(west) to track({track_b[0]}) with RoundRobin",
    )

    # Place train on Track A heading east
    train_id = game.place_train(track=track_a[0], speed=5)

    # Step enough frames for the train to traverse Track A and cross the junction.
    # Track A is 4 units long, speed=5 units/s, at 60fps ~= 48 frames.
    # Give extra margin for junction crossing.
    game.step(120)

    # Train should now be on Track B (the north output)
    state = game.query_train(train_id)
    all_b_tracks = set(track_b)
    all_c_tracks = set(track_c)
    assert state.track_id in all_b_tracks, (
        f"Expected train on Track B {all_b_tracks}, "
        f"but found on track {state.track_id}. "
        f"Track C was {all_c_tracks}"
    )


def _build_fan_junction(game: Game) -> tuple[list[str], list[str], list[str], list[str]]:
    """Build a fan junction: one input from west, three outputs all starting west at center.

    All outgoing tracks start with start_dir=west at the center point, which is
    the 180° opposite of the incoming east tangent. This makes all three valid
    exits for GetConnections, allowing RoundRobin to distribute across them.

    Layout:
        west ——→ center (east)
                 center (west) ——→ north
                 center (west) ——→ east
                 center (west) ——→ south

    Returns (track_west, track_north, track_east, track_south).
    """
    # Input track: west to center, arriving facing east
    track_west = game.place_track(
        start=f"0,{Y},0",
        end=f"4,{Y},0",
        start_dir="east",
        end_dir="east",
    )

    # Output north: leaves center facing west, curves to north
    track_north = game.place_track(
        start=f"4,{Y},0",
        end=f"4,{Y},4",
        start_dir="west",
        end_dir="north",
    )

    # Output east: leaves center facing west, curves to east
    track_east = game.place_track(
        start=f"4,{Y},0",
        end=f"8,{Y},0",
        start_dir="west",
        end_dir="east",
    )

    # Output south: leaves center facing west, curves to south
    track_south = game.place_track(
        start=f"4,{Y},0",
        end=f"4,{Y},-4",
        start_dir="west",
        end_dir="south",
    )

    # Step to let event chain propagate (track → junction → signal creation)
    game.step(10)

    return track_west, track_north, track_east, track_south


def test_round_robin_distributes_trains(game: Game) -> None:
    """RoundRobin should distribute successive trains across different output tracks.

    Three trains enter from the west. The junction has three valid exits (north,
    east, south) — all with start_dir=west (opposite of the incoming east tangent).
    With RoundRobin, each train should be routed to a different output track.
    """
    track_west, track_north, track_east, track_south = _build_fan_junction(game)
    junction_id = _find_signal_junction(game)

    # Set up RoundRobin rule from west to all three outputs
    game.clear_signal_rules(junction_id)
    game.add_signal_rule(
        junction_id,
        f"any from direction(west) to "
        f"[track({track_north[0]}),track({track_east[0]}),track({track_south[0]})] "
        f"with RoundRobin",
    )

    # Send three trains through, one at a time
    train_ids = []
    for _ in range(3):
        vid = game.place_train(track=track_west[0], speed=5)
        train_ids.append(vid)
        game.step(120)

    # Query where each train ended up
    output_tracks = []
    for vid in train_ids:
        state = game.query_train(vid)
        output_tracks.append(state.track_id)

    all_north = set(track_north)
    all_east = set(track_east)
    all_south = set(track_south)
    all_outputs = all_north | all_east | all_south

    # Each train should be on one of the output tracks
    for i, track_id in enumerate(output_tracks):
        assert track_id in all_outputs, (
            f"Train {i+1} should be on an output track, "
            f"but found on {track_id}"
        )

    # RoundRobin: all three should be on different output branches
    branches = []
    for track_id in output_tracks:
        if track_id in all_north:
            branches.append("north")
        elif track_id in all_east:
            branches.append("east")
        elif track_id in all_south:
            branches.append("south")

    assert len(set(branches)) == 3, (
        f"Expected trains on 3 different branches, "
        f"but got {branches}"
    )
