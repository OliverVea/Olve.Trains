---
name: testing
description: Reference for integration tests (pytest, screenshot comparison, game pool), unit tests (TUnit), test commands, and the full end-to-end build & test workflow. Use when running tests, writing new test scenarios, debugging test failures, or updating reference screenshots.
user-invocable: false
---

# Testing

**Always run integration tests before committing.**

## Integration Tests

Screenshot-based integration tests using pytest. Tests launch game instances, send commands via the pipe, and compare screenshots against committed reference images.

### Running

```bash
# Linux (headless via Xvfb, default)
bash scripts/integration-test.sh

# Windows (must use native windowing)
bash scripts/integration-test.sh --windowing native

# Skip asset pipeline and build steps (if already built)
bash scripts/integration-test.sh --skip-build

# Save current screenshots as new reference images
bash scripts/integration-test.sh --update-references

# Custom resolution (default: 1920x1080)
bash scripts/integration-test.sh --resolution 1280x720

# Parallel execution
bash scripts/integration-test.sh --pool-size 4
```

The script ensures `uv` is installed, runs `uv sync` for Python deps, then forwards args to `uv run pytest -v` in `tests/integration/`.

### Full End-to-End Build & Test

```bash
# 1. Run asset pipeline with S3 enabled
cd src/Olve.Trains.AssetPipeline && dotnet run

# 2. Build Release
dotnet build src/Olve.Trains/Olve.Trains.csproj --configuration Release

# 3. Run tests
bash scripts/integration-test.sh --skip-build
```

If assets changed (new meshes/textures, S3 updates), regenerate references:

```bash
bash scripts/integration-test.sh --skip-build --update-references  # regenerate
bash scripts/integration-test.sh --skip-build                      # verify
# Commit updated reference images in tests/integration/reference/
```

### Test Structure

Tests live in `tests/integration/scenarios/`. Each test gets a `game` fixture (pooled `Game` instance) and optionally a `screenshots` fixture for comparisons.

```python
def test_feature_name(
    game: Game,
    screenshots: ScreenshotComparer,
) -> None:
    # Setup
    game.place_track(start="0,0.125,0", end="10,0.125,0")
    game.step(200)
    game.set_camera(target="7,0,10", zoom=5)
    game.set_time("11:30")

    # Capture & compare
    path = game.screenshot(game.temp_dir / "screenshot.png")
    screenshots.compare(path, "reference-name")
```

### Fixtures (`tests/integration/conftest.py`)

**GamePool** (session-scoped) — Thread-safe queue of `Game` instances for parallel execution. Configurable via `--pool-size`.

**game** (function-scoped):
- Acquires game from pool
- Resets to "game" scene, deselects tool
- Marks log position for `assert_no_errors()` / `assert_no_warnings()`
- Auto-restarts on `GameCrashedError`
- Released back to pool after test

**screenshots** (function-scoped):
- Deferred assertion: collects `compare()` results, validates all at end via `assert_all()`
- Generates diff images on failure

### Screenshot Comparison (`tests/integration/screenshot.py`)

```python
compare_screenshots(actual, reference, threshold=0.999, pixel_tolerance=2, diff_output=None)
```

- Computes per-pixel absolute differences (RGB)
- "Changed pixel" = any channel difference > `pixel_tolerance`
- Pass if similarity >= `threshold` (default: 99.9% pixels unchanged)
- Returns `DiffResult` with `similarity`, `mean_diff`, `max_diff`, `changed_pixels_pct`, `passed`

### Reference Screenshots

```
tests/integration/reference/
├── linux/          # Linux-specific references
│   ├── burger-menu.png
│   └── ...
└── windows/        # Windows-specific references
    ├── burger-menu.png
    └── ...
```

Platform auto-detected. References are committed to git (LFS-tracked) and are the source of truth. CI validates against them.

**If CI fails but local passes:** most likely cause is missing S3 assets — rerun the asset pipeline with `UseLocalAssets: false`.

### Screenshot Diff Artifacts

When comparison fails, a diff image is generated in `/tmp/screenshot-diffs/<test_name>/` with three panels:
- **Baseline** — committed reference
- **Actual** — test output
- **Diff** — greyscale delta (brighter = larger difference)

In CD (the Olve.Pipelines `test` step) a screenshot mismatch fails the step and blocks publishing; the diffs appear in the job logs. (Uploading diffs to the VR review app is a planned follow-up — see the Infrastructure epic in `TODO.md`.)

### Existing Test Scenarios

| Test file | Type | What it covers |
|---|---|---|
| `test_track_loop.py` | Screenshot | Track placement, trains, time-of-day, shadow maps |
| `test_cargo_transport.py` | Screenshot | Full supply chain: forest → station → train → sawmill |
| `test_industry_buildings.py` | Screenshot | Industry placement and animation |
| `test_industry_production.py` | Screenshot | Production chains |
| `test_menu.py` | Screenshot | GUI menu interactions |
| `test_signal_panel.py` | Screenshot | Signal rules panel |
| `test_station_info_panel.py` | Screenshot | Station info panel |
| `test_terrain_grid.py` | Screenshot | Terrain grid highlight with tools |
| `test_dropdown.py` | Screenshot | Dropdown GUI |
| `test_wagons.py` | Screenshot | Wagon attachment and cargo |
| `test_collision_raycast.py` | Functional | Raycast hit detection |
| `test_signal_routing.py` | Functional | Signal rule logic |
| `test_time_scale.py` | Functional | Day/time progression at various speeds |
| `test_no_warnings.py` | Functional | No warnings/errors during startup |

## Unit Tests

**Framework:** TUnit

```bash
dotnet run --project tests/Olve.Engine3D.Tests/Olve.Engine3D.Tests.csproj
```

Tests live in `tests/Olve.Engine3D.Tests/` organized by component (Animation, Checkbox, Layout, RadioButton, Slider). Uses standard TUnit assertions with DI (ServiceCollection).
