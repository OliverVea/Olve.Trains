---
name: run-game
description: Reference for running the game, detached mode (command pipe), replay system, command reference, and the Game Python class. Use when launching the game for the user, sending commands, writing replays, or setting up test scenarios.
user-invocable: false
---

# Running the Game

## Quick Start

```bash
# Initialize the game with the industry-loop scenario (builds + launches)
bash .claude/skills/run-game/scripts/initialize-game.sh

# On Windows with native windowing
bash .claude/skills/run-game/scripts/initialize-game.sh --windowing native

# Normal interactive mode (no scenario)
dotnet run --project src/Olve.Trains/Olve.Trains.csproj

# With command pipe (for replays, testing, AI agents)
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --listen
```

## Scripts

| Script | Description |
|---|---|
| `scripts/initialize-game.sh` | Build Release and launch the industry-loop replay for interactive play |

## Detached Mode (Command Pipe)

The game supports a named pipe interface for external tooling. Listening is off by default.

**Start with listening:**
```bash
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --listen
# With a specific instance ID:
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --listen --instance my-game
```

**Send commands to a running instance:**
```bash
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --send "echo message=hello"
# Target a specific instance:
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --send "help" --instance my-game
```

**Enable via config** (`appsettings.json`):
```json
{ "Detached": { "Listen": true } }
```

Commands are processed on the main game thread via `CommandProcessingService`.

## Command Reference

Use `help` to see all available commands. Key commands:

**place-track** — Place track segments between two points with optional direction control.
```bash
# Straight track (directions auto-calculated)
--send "place-track start=0,0.125,0 end=4,0.125,0"

# Curved track (explicit directions)
--send "place-track start=0,0.125,0 end=4,0.125,4 start-dir=east end-dir=north"
```
Directions: `north`, `south`, `east`, `west` (or `n`, `s`, `e`, `w`)

**place-train** — Place a train on a track.
```bash
--send "place-train track=<track-id>"
```

**screenshot** — Take a screenshot.
```bash
--send "screenshot path=~/screenshot.png"
```

**Other useful commands:** `step`, `set-camera`, `set-time`, `set-speed`, `select-tool`, `place-building`, `list-trains`, `query-train`, `list-junctions`, `query-junction`, `load-scene`, `exit`

## Example: Creating a 4x4 Circle Track with Train

```bash
# Start game in listening mode
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --listen &
sleep 3

# Build 4x4 square loop (Y=0.125 is track height)
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --send "place-track start=0,0.125,0 end=4,0.125,0 start-dir=east end-dir=north"
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --send "place-track start=4,0.125,0 end=4,0.125,4 start-dir=north end-dir=west"
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --send "place-track start=4,0.125,4 end=0,0.125,4 start-dir=west end-dir=south"
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --send "place-track start=0,0.125,4 end=0,0.125,0 start-dir=south end-dir=east"

# Place train (use track ID from output) and screenshot
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --send "place-train track=<track-id>"
dotnet run --project src/Olve.Trains/Olve.Trains.csproj -- --send "screenshot path=~/circle-track.png"
```

## Replay System

Use replays to set up game scenarios for testing or user interaction.

### Python replays (recommended for complex scenarios)

The canonical replay is `scripts/replays/industry-loop.py` — an industry loop with depot, stations, forest, sawmill, tracks, and trains with wagons.

```bash
# Build Release first (if not already built)
dotnet build src/Olve.Trains/Olve.Trains.csproj --configuration Release

# Run the replay (launches game, sets up scenario, keeps game running)
tests/integration/.venv/Scripts/python.exe scripts/replays/industry-loop.py
```

Python replays use the `Game` class from `tests/integration/game.py`. They start the game with `--listen`, send commands via the pipe, and keep the game alive for interactive use.

### Text-file replays (simple, no ID chaining)

```bash
tests/integration/.venv/Scripts/python.exe scripts/replay.py scripts/replays/some-file.txt --skip-build
```

### Existing replays

| File | Description |
|---|---|
| `scripts/replays/industry-loop.py` | Full scenario: depot, stations, forest, sawmill, tracks, trains |
| `scripts/replays/industry-loop.txt` | Text-file version of industry loop |
| `scripts/replays/depot-test.txt` | Depot placement test |
| `scripts/replays/observe.txt` | Observation/camera setup |

New replays go in `scripts/replays/`.

## Game Python Class (`tests/integration/game.py`)

The `Game` class manages game lifecycle and provides typed command wrappers.

### Constructor

```python
game = Game(
    instance_id="replay",       # pipe instance name
    resolution="1280x720",      # window resolution
    windowing="native",         # "native" (Windows) or "xvfb" (Linux headless)
    skip_build=True,            # skip asset pipeline + build
    scene="game",               # initial scene to load
    manual=True,                # manual mode (user controls time)
    kill_stale=True,            # kill existing game processes
)
```

### Lifecycle

```python
game.start()   # build (if needed) → kill stale → start Xvfb (Linux) → launch game → wait for pipe
game.stop()    # send exit → terminate process
```

### Typed command wrappers

| Method | Returns | Description |
|---|---|---|
| `game.send(cmd)` | `CommandResult` | Raw command string |
| `game.place_track(start, end, start_dir?, end_dir?)` | `list[str]` | Track IDs |
| `game.place_train(track?, depot?, speed?)` | `str` | Train ID |
| `game.place_building(pos, type, dir)` | `str` | Building ID |
| `game.screenshot(path, target?, debug?)` | `Path` | Screenshot file path |
| `game.step(frames=1)` | `CommandResult` | Advance N frames |
| `game.set_camera(target, zoom)` | `CommandResult` | Set camera position |
| `game.set_time(time)` | `CommandResult` | Set game time |
| `game.set_speed(scale)` | `CommandResult` | Set time scale |
| `game.query_train(train_id)` | `TrainState` | Train state |
| `game.list_trains()` | `list[TrainState]` | All trains |
| `game.list_junctions()` | `list[JunctionInfo]` | All junctions |
| `game.query_junction(junction_id)` | `JunctionDetail` | Junction detail |
| `game.list_wagons(train_id)` | `list[WagonInfo]` | Train's wagons |
| `game.add_wagon(train_id, blueprint_id)` | `str` | Wagon ID |
| `game.load_scene(scene)` | `CommandResult` | Load a scene |
| `game.select_tool(name)` | `CommandResult` | Select a tool |
| `game.click(x, y)` | `CommandResult` | Simulate click at screen position |
| `game.raycast(x, y)` | `list[RaycastHit]` | Raycast from screen position |

### Log assertions (for testing)

```python
game.mark_log_position()       # mark current log position
game.assert_no_errors()        # assert no 'fail:' lines since mark
game.assert_no_warnings()      # assert no 'warn:' lines since mark
```

### Error types

- `GameCrashedError` — game process exited unexpectedly (includes stderr)
- `CommandError` — command returned non-zero exit code
