#!/usr/bin/env python3
"""Replay a command file against a windowed game instance.

Usage:
    python scripts/replay.py <command-file> [--scene game] [--skip-build] [--resolution 1280x720]

Command file format:
    - One command per line
    - Lines starting with # are comments
    - Blank lines are ignored
    - Lines starting with 'wait ' pause for N seconds (e.g. 'wait 2')
"""
from __future__ import annotations

import argparse
import logging
import sys
import time
from pathlib import Path

# Add tests/integration to path so we can import the Game helper
sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "tests" / "integration"))

from game import Game

logging.basicConfig(level=logging.INFO, format="%(levelname)s: %(message)s")
logger = logging.getLogger(__name__)


def parse_commands(path: Path) -> list[str]:
    lines = path.read_text().splitlines()
    commands = []
    for line in lines:
        stripped = line.strip()
        if not stripped or stripped.startswith("#"):
            continue
        commands.append(stripped)
    return commands


def main() -> int:
    parser = argparse.ArgumentParser(description="Replay commands against a windowed game")
    parser.add_argument("command_file", type=Path, help="Path to command file")
    parser.add_argument("--scene", default="game", help="Starting scene (default: game)")
    parser.add_argument("--skip-build", action="store_true", help="Skip asset pipeline and build")
    parser.add_argument("--resolution", default="1280x720", help="Window resolution (default: 1280x720)")
    args = parser.parse_args()

    commands = parse_commands(args.command_file)
    if not commands:
        logger.error("No commands found in %s", args.command_file)
        return 1

    logger.info("Loaded %d commands from %s", len(commands), args.command_file)

    game = Game(
        instance_id="replay",
        resolution=args.resolution,
        skip_build=args.skip_build,
        scene=args.scene,
        manual=False,
    )

    try:
        game.start()
        logger.info("Game log: %s", game._game_stderr_path)

        for i, cmd in enumerate(commands):
            if cmd.startswith("wait "):
                seconds = float(cmd.split(None, 1)[1])
                logger.info("[%d/%d] wait %.1fs", i + 1, len(commands), seconds)
                time.sleep(seconds)
                continue

            logger.info("[%d/%d] %s", i + 1, len(commands), cmd)
            result = game.send(cmd, check=False)
            if not result.success:
                logger.warning("  failed: %s", "; ".join(result.errors))
            elif result.output:
                logger.info("  -> %s", result.output)

        logger.info("Replay complete. Game is still running — close the window to exit.")

        # Keep the script alive until the game exits
        while game._game_proc and game._game_proc.poll() is None:
            time.sleep(1)

    except KeyboardInterrupt:
        logger.info("Interrupted")
    finally:
        game.stop()

    return 0


if __name__ == "__main__":
    sys.exit(main())
