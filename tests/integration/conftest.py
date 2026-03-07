from __future__ import annotations

import json
import logging
import platform
import queue
import subprocess
import tempfile
from datetime import date
from pathlib import Path

import pytest

import game as game_module
from game import Game
from screenshot import compare_screenshots, update_reference

PROJECT_DIR = Path(__file__).resolve().parent.parent.parent
_PLATFORM = "windows" if platform.system() == "Windows" else "linux"
REFERENCE_DIR = Path(__file__).parent / "reference" / _PLATFORM
DIFF_DIR = Path("/tmp/screenshot-diffs")
LOG_DIR = PROJECT_DIR / "src" / "Olve.Trains" / "logs"


def pytest_configure(config: pytest.Config) -> None:
    LOG_DIR.mkdir(parents=True, exist_ok=True)
    log_file = LOG_DIR / f"integration-test-{date.today()}.log"

    handler = logging.FileHandler(log_file, mode="a")
    handler.setFormatter(logging.Formatter("%(asctime)s %(levelname)s %(name)s: %(message)s"))

    root = logging.getLogger()
    root.addHandler(handler)
    root.setLevel(logging.DEBUG)


def pytest_addoption(parser: pytest.Parser) -> None:
    parser.addoption(
        "--update-references",
        action="store_true",
        default=False,
        help="Save screenshots as new references instead of comparing",
    )
    parser.addoption(
        "--windowing",
        default="xvfb",
        choices=["native", "xvfb"],
        help="Windowing mode (default: xvfb)",
    )
    parser.addoption(
        "--skip-build",
        action="store_true",
        default=False,
        help="Skip asset pipeline and dotnet build",
    )
    parser.addoption(
        "--resolution",
        default="1920x1080",
        help="Screen resolution (default: 1920x1080)",
    )
    parser.addoption(
        "--s3",
        action="store_true",
        default=False,
        help="Upload screenshots to S3 after test run",
    )
    parser.addoption(
        "--pool-size",
        type=int,
        default=1,
        help="Number of game instances to run in parallel (default: 1)",
    )


class GamePool:
    def __init__(self, games: list[Game]):
        self._all = games
        self._available: queue.Queue[Game] = queue.Queue()
        for g in games:
            self._available.put(g)

    def acquire(self) -> Game:
        return self._available.get()

    def release(self, game: Game) -> None:
        self._available.put(game)

    def stop_all(self) -> None:
        for g in self._all:
            g.stop()


@pytest.fixture(scope="session")
def _game_pool(request: pytest.FixtureRequest) -> GamePool:
    resolution = request.config.getoption("--resolution")
    windowing = request.config.getoption("--windowing")
    skip_build = request.config.getoption("--skip-build")
    pool_size = request.config.getoption("--pool-size")

    games: list[Game] = []
    try:
        for i in range(pool_size):
            g = Game(
                instance_id=f"integration-test-{i}",
                resolution=resolution,
                windowing=windowing,
                skip_build=skip_build or i > 0,  # only build once
                scene="game",
                kill_stale=i == 0,  # only kill stale processes on first instance
            )
            g.start()
            games.append(g)
    except Exception:
        for g in games:
            g.stop()
        raise

    pool = GamePool(games)
    yield pool
    pool.stop_all()


@pytest.fixture
def game(_game_pool: GamePool) -> Game:
    g = _game_pool.acquire()
    try:
        g.load_scene("game")
        g.step(2)
    except game_module.GameCrashedError:
        # Game died — restart it before handing to the test
        g._stop_game()
        g._launch_game()
        g._wait_for_pipe()
        g.load_scene("game")
        g.step(2)
    yield g
    _game_pool.release(g)


@pytest.fixture(scope="session")
def reference_dir() -> Path:
    REFERENCE_DIR.mkdir(parents=True, exist_ok=True)
    return REFERENCE_DIR



@pytest.fixture(scope="session")
def update_references(request: pytest.FixtureRequest) -> bool:
    return request.config.getoption("--update-references")


class ScreenshotAsserter:
    """Session-level screenshot tracker for S3 uploads."""

    def __init__(self, reference_dir: Path, updating: bool):
        self.reference_dir = reference_dir
        self.updating = updating
        self.screenshots: list[tuple[str, Path]] = []
        self.diffs: list[tuple[str, Path]] = []


class ScreenshotComparer:
    """Per-test screenshot comparison with deferred assertion."""

    def __init__(self, asserter: ScreenshotAsserter, diff_dir: Path):
        self._asserter = asserter
        self._diff_dir = diff_dir
        self._results: list[tuple[str, object]] = []

    def compare(
        self,
        actual: Path,
        name: str,
        threshold: float = 0.995,
        pixel_tolerance: int = 2,
    ) -> None:
        ref = self._asserter.reference_dir / f"{name}.png"
        self._asserter.screenshots.append((name, actual))

        if self._asserter.updating:
            update_reference(actual, ref)
            return

        if not ref.exists():
            self._results.append((name, FileNotFoundError(
                f"Reference screenshot not found: {ref}\n"
                f"Run with --update-references to generate it."
            )))
            return

        diff_path = self._diff_dir / f"{name}-diff.png"
        result = compare_screenshots(
            actual, ref,
            threshold=threshold,
            pixel_tolerance=pixel_tolerance,
            diff_output=diff_path,
        )
        if not result.passed and result.diff_image_path and result.diff_image_path.exists():
            self._asserter.diffs.append((f"{name}-diff", result.diff_image_path))
            # Write similarity value for CI to pick up
            sim_path = result.diff_image_path.parent / f"{name}-similarity.txt"
            sim_path.write_text(f"{result.similarity:.6f}")
        self._results.append((name, result))

    def assert_all(self) -> None:
        failures: list[str] = []
        for name, result in self._results:
            if isinstance(result, Exception):
                failures.append(f"  {name}: {result}")
            elif not result.passed:
                failures.append(f"  {name}: {result.summary()}")
                if result.diff_image_path:
                    failures.append(f"    diff: {result.diff_image_path}")
        if failures:
            raise AssertionError("Screenshot mismatches:\n" + "\n".join(failures))


_active_asserter: ScreenshotAsserter | None = None


@pytest.fixture(scope="session")
def _screenshot_session(
    reference_dir: Path,
    update_references: bool,
) -> ScreenshotAsserter:
    global _active_asserter
    _active_asserter = ScreenshotAsserter(reference_dir, update_references)
    return _active_asserter


@pytest.fixture
def screenshots(_screenshot_session: ScreenshotAsserter, request: pytest.FixtureRequest) -> ScreenshotComparer:
    diff_dir = DIFF_DIR / request.node.name
    ts = ScreenshotComparer(_screenshot_session, diff_dir)
    yield ts
    ts.assert_all()


S3_BUCKET = "olve.trains"
S3_KEY_PREFIX = "screenshots/integration-test"
S3_REGION = "ap-southeast-2"


def pytest_sessionfinish(session: pytest.Session, exitstatus: int) -> None:
    if not session.config.getoption("--s3"):
        return

    if _active_asserter is None:
        return

    uploads = _active_asserter.screenshots + _active_asserter.diffs
    if not uploads:
        return

    _upload_to_s3(uploads)


def _upload_to_s3(screenshots: list[tuple[str, Path]]) -> None:
    import os

    shlink_api_key = os.environ.get("SHLINK_API_KEY", "")
    shlink_url = "https://s.ovhome.online"

    print("\n\nUploading to S3...")
    print("=" * 44)
    print("Screenshots uploaded successfully!")

    for name, path in screenshots:
        s3_key = f"{S3_KEY_PREFIX}-{name}.png"
        subprocess.run(
            ["aws", "s3", "cp", str(path), f"s3://{S3_BUCKET}/{s3_key}", "--region", S3_REGION],
            check=True,
        )

        result = subprocess.run(
            ["aws", "s3", "presign", f"s3://{S3_BUCKET}/{s3_key}", "--region", S3_REGION, "--expires-in", "3600"],
            capture_output=True,
            text=True,
            check=True,
        )
        url = result.stdout.strip()

        if shlink_api_key:
            try:
                import urllib.request

                req = urllib.request.Request(
                    f"{shlink_url}/rest/v3/short-urls",
                    data=json.dumps({"longUrl": url}).encode(),
                    headers={
                        "X-Api-Key": shlink_api_key,
                        "Content-Type": "application/json",
                    },
                    method="POST",
                )
                with urllib.request.urlopen(req) as resp:
                    url = json.loads(resp.read())["shortUrl"]
            except Exception:
                pass

        print(f"  {name}: {url}")

    print("=" * 44)
