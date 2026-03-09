from __future__ import annotations

from game import Game

Y = "0.125"


def test_wagon_lifecycle(game: Game) -> None:
    # Place a track and a train
    track_ids = game.place_track(
        start=f"0,{Y},0",
        end=f"4,{Y},0",
        start_dir="east",
        end_dir="east",
    )
    train_id = game.place_train(track=track_ids[0])
    game.step(2)

    # 1. Auto-attached wagons: should have 2 goods wagons
    wagons = game.list_wagons(train_id)
    assert len(wagons) == 2, f"Expected 2 auto-attached wagons, got {len(wagons)}"

    # All wagons should have the same blueprint
    blueprint_id = wagons[0].blueprint_id
    assert all(w.blueprint_id == blueprint_id for w in wagons)

    # 2. Each wagon should have an inventory ID
    for w in wagons:
        assert w.inventory_id, f"Wagon {w.wagon_id} has no inventory"

    # 3. query-train should include wagons
    state = game.send(f"query-train train={train_id}")
    import json
    data = json.loads(state.output)
    assert "wagons" in data, "query-train response missing 'wagons'"
    assert len(data["wagons"]) == 2

    # 4. Remove one wagon
    game.remove_wagon(train_id, index=0)
    wagons = game.list_wagons(train_id)
    assert len(wagons) == 1, f"Expected 1 wagon after removal, got {len(wagons)}"

    # 5. Add a wagon back
    new_wagon_id = game.add_wagon(train_id, blueprint_id)
    assert new_wagon_id
    wagons = game.list_wagons(train_id)
    assert len(wagons) == 2, f"Expected 2 wagons after add, got {len(wagons)}"

    # 6. Delete train and place a new one — verify fresh auto-attach
    game.send(f"delete-train train={train_id}")

    track_ids2 = game.place_track(
        start=f"0,{Y},4",
        end=f"4,{Y},4",
        start_dir="east",
        end_dir="east",
    )
    new_train_id = game.place_train(track=track_ids2[0])
    game.step(2)

    wagons = game.list_wagons(new_train_id)
    assert len(wagons) == 2, f"Expected 2 wagons on new train, got {len(wagons)}"
    for w in wagons:
        assert w.inventory_id, f"New wagon {w.wagon_id} has no inventory"
