"""Verify the 'cannot afford track' crash fix (Result-handling epic, Step 2).

Drives the REAL tool placement flow (select-tool + click, the fatal path — NOT
the place-track command, which is non-fatal). Confirms that placing a track you
can't afford via the tool logs at info and does NOT shut the game down.

The two click positions were found empirically: with the default isometric
camera they map to world tiles (14,4) and (14,44) — a straight North-South
track (same X), which the placement tool accepts. Each click is preceded by a
set-mouse + settle steps so the terrain raycast resolves to the clicked tile
(a click alone reads a stale ray in headless mode).

Flow:
  1. Place the track via the tool WITH money  -> proves the click pair is valid.
  2. Drain the balance below the track cost via the (non-fatal) command path.
  3. Replay the SAME tool placement while broke. Pre-fix this propagated the
     'Cannot afford track' problem into GameManager.Update -> Stop() (crash).
     Post-fix it logs at info and the game keeps running.
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "tests" / "integration"))
from game import Game  # noqa: E402

A = (-0.1, -0.25)   # -> tile (14, 4)
B = (0.3, -0.75)    # -> tile (14, 44)


def money(game: Game) -> int:
    return json.loads(game.send("query-money").output)["balance"]


def tool_place(game: Game):
    """Full two-click tool placement; returns (money_delta, log_since_start)."""
    game.send("select-tool name=none")
    game.step(1)
    game.send("select-tool name='Place Tracks'")
    game.step(1)
    game.mark_log_position()
    before = money(game)
    for (x, y) in (A, B):
        game.send(f"set-mouse pos={x},{y}")
        game.step(3)          # let the terrain raycast settle on this tile
        game.send(f"click pos={x},{y}")
        game.step(3)
    return before - money(game), game._read_log_since_mark()


def main() -> int:
    game = Game(
        instance_id="crashfix-verify",
        windowing="xvfb",
        skip_build=True,
        scene="game",
        manual=True,
    )
    game.start()
    try:
        game.step(2)
        print(f"[1] starting balance: {money(game)}")

        # 1. Prove the click pair places a real track while we have money.
        cost, log = tool_place(game)
        placed = cost > 0 and "Created track" in log
        print(f"[2] tool placement WITH money: cost {cost}, created track: {placed}")
        if not placed:
            print("[!] click pair did not place a track — cannot verify.")
            return 2

        # 2. Drain below one track's cost via the non-fatal command path.
        drain_x = 2
        while money(game) >= cost:
            game.place_track(f"{drain_x},0.125,2", f"{drain_x},0.125,48")
            drain_x += 2
        print(f"[3] drained to balance {money(game)} (< track cost {cost})")

        # 3. Replay the SAME tool placement while broke — the fatal path pre-fix.
        broke_before = money(game)
        delta, log = tool_place(game)
        broke_after = money(game)   # raises GameCrashedError if the game shut down
        print(f"[4] tool placement WHILE BROKE: {broke_before} -> {broke_after} "
              f"(game ALIVE, delta {delta})")

        cant_afford = "Could not place track" in log and "Cannot afford" in log
        print(f"[5] 'Could not place track' + 'Cannot afford' logged at info: {cant_afford}")

        game.assert_no_errors()
        print("[6] no 'fail:' lines — the can't-afford problem did NOT reach the fatal boundary")

        ok = broke_after == broke_before and cant_afford
        print("\nRESULT:", "PASS — crash fixed, game survives can't-afford via the tool"
              if ok else "INCONCLUSIVE")
        return 0 if ok else 3
    finally:
        game.stop()


if __name__ == "__main__":
    sys.exit(main())
