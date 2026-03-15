"""Tests that the time scale feature correctly affects day-time progression."""
from __future__ import annotations

import pytest

from game import Game


# DayTimeSteppingService sets DayDuration = 6 minutes (360 seconds).
# ManualStepper uses FrameTime = 1/60 seconds.
# Game hours per frame at 1x speed = (1/60) / 360 * 24 = 1/900 hours.
HOURS_PER_FRAME = 1.0 / 900.0
FRAMES = 100
TOLERANCE = 0.002  # ~1.7 frames of error margin


def _expected_advance(frames: int, scale: float) -> float:
    """Return the expected game-hour advance for the given frame count and time scale."""
    return frames * HOURS_PER_FRAME * scale


def test_time_scale(game: Game) -> None:
    """Step 100 frames at various speed settings and verify the day-time advances correctly."""

    cases: list[tuple[str, float]] = [
        ("0x (paused)", 0.0),
        ("1x (normal)", 1.0),
        ("0.5x (slow)", 0.5),
        ("2x (fast)", 2.0),
        ("3x (faster)", 3.0),
    ]

    for label, scale in cases:
        # Set the speed first, then reset time, so the setup steps run at the desired speed
        game.set_speed(scale)
        game.set_time("12:00")
        game.step(1)  # flush commands

        before = game.query_time()["value"]

        game.step(FRAMES)

        after = game.query_time()["value"]

        actual_advance = after - before
        # The step(1) above also runs at this speed, so total frames = FRAMES
        expected_advance = _expected_advance(FRAMES, scale)

        assert abs(actual_advance - expected_advance) < TOLERANCE, (
            f"[{label}] Expected advance {expected_advance:.4f}h, "
            f"got {actual_advance:.4f}h "
            f"(before={before:.4f}, after={after:.4f})"
        )

    # Reset speed to 1x so other tests aren't affected
    game.set_speed(1.0)
    game.step(1)
