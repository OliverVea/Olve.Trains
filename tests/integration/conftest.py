from __future__ import annotations

import json
import logging
import subprocess
import tempfile
from datetime import date
from pathlib import Path

import pytest

from game import Game
from screenshot import assert_screenshot_matches, update_reference

REFERENCE_DIR = Path(__file__).parent / "reference"
LOG_DIR = Path.home() / "projects" / "Olve.Trains" / "src" / "Olve.Trains" / "logs"


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


@pytest.fixture(scope="session")
def game(request: pytest.FixtureRequest) -> Game:
    instance_id = f"integration-test-{id(request.session)}"
    g = Game(
        instance_id=instance_id,
        resolution=request.config.getoption("--resolution"),
        windowing=request.config.getoption("--windowing"),
        skip_build=request.config.getoption("--skip-build"),
    )
    g.start()
    yield g
    g.stop()


@pytest.fixture(scope="session")
def reference_dir() -> Path:
    REFERENCE_DIR.mkdir(parents=True, exist_ok=True)
    return REFERENCE_DIR


@pytest.fixture(scope="session")
def output_dir() -> Path:
    d = Path(tempfile.mkdtemp(prefix="integration-output-"))
    return d


@pytest.fixture(scope="session")
def update_references(request: pytest.FixtureRequest) -> bool:
    return request.config.getoption("--update-references")


class ScreenshotAsserter:
    def __init__(
        self,
        reference_dir: Path,
        output_dir: Path,
        updating: bool,
    ):
        self.reference_dir = reference_dir
        self.output_dir = output_dir
        self.updating = updating
        self.screenshots: list[tuple[str, Path]] = []

    def assert_matches(
        self,
        actual: Path,
        name: str,
        threshold: float = 0.995,
        pixel_tolerance: int = 2,
    ) -> None:
        ref = self.reference_dir / f"{name}.png"
        self.screenshots.append((name, actual))

        if self.updating:
            update_reference(actual, ref)
        else:
            assert_screenshot_matches(
                actual, ref, threshold=threshold, pixel_tolerance=pixel_tolerance
            )


_active_asserter: ScreenshotAsserter | None = None


@pytest.fixture(scope="session")
def screenshots(
    reference_dir: Path,
    output_dir: Path,
    update_references: bool,
) -> ScreenshotAsserter:
    global _active_asserter
    _active_asserter = ScreenshotAsserter(reference_dir, output_dir, update_references)
    return _active_asserter


S3_BUCKET = "olve.trains"
S3_KEY_PREFIX = "screenshots/integration-test"
S3_REGION = "ap-southeast-2"


def pytest_sessionfinish(session: pytest.Session, exitstatus: int) -> None:
    if not session.config.getoption("--s3"):
        return

    if _active_asserter is None or not _active_asserter.screenshots:
        return

    _upload_to_s3(_active_asserter.screenshots)


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
