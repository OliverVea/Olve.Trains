"""Tests the save/load UI: the burger menu's Save/Load buttons drive the quicksave slot end-to-end."""
from __future__ import annotations

import json
from pathlib import Path

from game import Game


def _balance(game: Game) -> int:
    return json.loads(game.send("query-money").output)["balance"]


def test_burger_menu_save_and_load_quicksave(game: Game) -> None:
    """Save via the burger menu, mutate further, then load via the burger menu and assert the
    quicksave snapshot (not the later state) is restored."""

    # Learn the quicksave path, then remove it so the Save button must recreate it.
    quicksave_path = Path(game.save_game("quicksave"))
    quicksave_path.unlink(missing_ok=True)

    try:
        # Mutate state away from the new-game defaults, then snapshot the live balance.
        game.place_track(
            start="15.5,0.125,12.5",
            end="19.5,0.125,12.5",
            start_dir="east",
            end_dir="east",
        )  # spends money
        game.set_time("08:30")
        game.step(2)  # flush commands; ManualStepper only advances time on step()
        saved_balance = _balance(game)

        # Save via the burger menu (kind=quicksave). The menu closes and stays in game.
        game.activate_gui("InfoBar/Box/MenuButton")
        game.step(2)
        game.activate_gui("BurgerMenu/Box/SaveGameButton")
        game.step(2)
        assert quicksave_path.exists(), f"Save button did not write quicksave to {quicksave_path}"

        # Mutate further so live state diverges from the quicksave.
        game.place_track(
            start="15.5,0.125,16.5",
            end="19.5,0.125,16.5",
            start_dir="east",
            end_dir="east",
        )  # spends more money
        game.step(2)
        assert _balance(game) < saved_balance, "Second track should have lowered the balance"

        # Load via the burger menu (kind=quicksave). This tears down the game and reloads it.
        game.activate_gui("InfoBar/Box/MenuButton")
        game.step(2)
        game.activate_gui("BurgerMenu/Box/LoadGameButton")
        game.wait_for_game_scene()  # the reload is async — never poll game-logic commands here

        # The restored balance matches the quicksave, not the post-save mutation.
        assert _balance(game) == saved_balance
    finally:
        quicksave_path.unlink(missing_ok=True)

    game.assert_no_errors()


def test_main_menu_load_button_loads_quicksave(game: Game) -> None:
    """The main menu's Load button restores the quicksave through the loading scene.

    The GameLogicScene's command handlers are not loaded at the main menu, so the button transitions
    to the loading scene with the resolved quicksave path (the same path Start Game takes, plus a save)."""

    # Mutate state, then write the quicksave the menu button will load.
    game.place_track(
        start="15.5,0.125,12.5",
        end="19.5,0.125,12.5",
        start_dir="east",
        end_dir="east",
    )  # spends money
    game.set_time("08:30")
    game.step(2)
    saved_balance = _balance(game)
    quicksave_path = Path(game.save_game("quicksave"))

    try:
        # Go to the main menu, then load via its Load button.
        game.load_scene("menu")
        game.step(2)
        game.activate_gui("MainMenu/Box/LoadGameButton")
        game.wait_for_game_scene()  # the reload is async — never poll game-logic commands here

        assert _balance(game) == saved_balance
    finally:
        quicksave_path.unlink(missing_ok=True)

    game.assert_no_errors()
