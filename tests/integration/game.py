from __future__ import annotations

import atexit
import json
import logging
import os
import re
import subprocess
import tempfile
import time
import uuid
import weakref
from dataclasses import dataclass, field
from pathlib import Path

_live_games: weakref.WeakSet = weakref.WeakSet()


def _cleanup_all() -> None:
    for g in list(_live_games):
        try:
            g.stop()
        except Exception:
            pass


atexit.register(_cleanup_all)

logger = logging.getLogger(__name__)

PROJECT_DIR = Path(__file__).resolve().parent.parent.parent
BIN_DIR = PROJECT_DIR / "src" / "Olve.Trains" / "bin" / "Release" / "net10.0"
ASSET_PIPELINE_PROJECT = (
    PROJECT_DIR
    / "src"
    / "Olve.Trains.AssetPipeline"
    / "Olve.Trains.AssetPipeline.csproj"
)
GAME_PROJECT = PROJECT_DIR / "src" / "Olve.Trains" / "Olve.Trains.csproj"
GAME_DLL = BIN_DIR / "On Track To Grow.dll"

UUID_RE = re.compile(r"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}")


@dataclass
class CommandResult:
    success: bool
    output: str
    errors: list[str]


@dataclass
class TrainState:
    train_id: str
    track_id: str
    time: float
    velocity: float
    position: tuple[float, float, float] | None = None


@dataclass
class JunctionInfo:
    junction_id: str
    position: tuple[int, int]
    connection_count: int
    has_signal: bool


@dataclass
class JunctionConnection:
    track_id: str
    direction: str


@dataclass
class JunctionDetail:
    junction_id: str
    position: tuple[int, int]
    has_signal: bool
    connections: list[JunctionConnection] = field(default_factory=list)
    rules: list[dict] = field(default_factory=list)


@dataclass
class WagonCargo:
    cargo_type: str
    amount: int


@dataclass
class WagonInfo:
    wagon_id: str
    blueprint_id: str
    inventory_id: str
    cargo: list[WagonCargo] = field(default_factory=list)


class GameCrashedError(Exception):
    def __init__(self, exit_code: int | None, stderr: str = ""):
        self.exit_code = exit_code
        self.stderr = stderr
        msg = f"Game process exited unexpectedly (exit code: {exit_code})"
        if stderr:
            msg += f"\n--- game stderr ---\n{stderr}\n---"
        super().__init__(msg)


class CommandError(Exception):
    def __init__(self, command: str, result: CommandResult):
        self.command = command
        self.result = result
        super().__init__(
            f"Command '{command}' failed: {'; '.join(result.errors)}"
        )


class Game:
    _next_display = 99

    def __init__(
        self,
        instance_id: str = "integration-test",
        resolution: str = "1920x1080",
        windowing: str = "native",
        skip_build: bool = False,
        scene: str | None = None,
        kill_stale: bool = True,
    ):
        self.instance_id = instance_id
        self.resolution = resolution
        self.windowing = windowing
        self.skip_build = skip_build
        self.scene = scene
        self.kill_stale = kill_stale

        self._display: int = Game._next_display
        Game._next_display += 1
        self._game_env: dict[str, str] | None = None
        self._game_proc: subprocess.Popen | None = None
        self._xvfb_proc: subprocess.Popen | None = None
        self._temp_dir = tempfile.mkdtemp(prefix="integration-test-")
        self._pipe_suffix: str = uuid.uuid4().hex[:8]

    @property
    def _pipe_id(self) -> str:
        return f"{self.instance_id}-{self._pipe_suffix}"

    def start(self) -> None:
        _live_games.add(self)
        self._build()
        if self.kill_stale:
            self._kill_stale()
        self._start_xvfb()
        self._launch_game()
        self._wait_for_pipe()

    def stop(self) -> None:
        self._stop_game()
        self._stop_xvfb()

    def _stop_game(self) -> None:
        if self._game_proc is None:
            return

        try:
            self.send("exit", check=False)
            self._game_proc.wait(timeout=5)
        except Exception:
            pass

        if self._game_proc.poll() is None:
            self._game_proc.terminate()
            try:
                self._game_proc.wait(timeout=5)
            except subprocess.TimeoutExpired:
                self._game_proc.kill()
                self._game_proc.wait(timeout=5)

        if hasattr(self, "_game_stderr_file") and self._game_stderr_file:
            self._game_stderr_file.close()

        # Generate a new pipe name so the next launch doesn't collide
        # with a stale pipe handle (Windows holds pipe names briefly after close)
        self._pipe_suffix = uuid.uuid4().hex[:8]

    def _stop_xvfb(self) -> None:
        if self._xvfb_proc is None:
            return

        self._xvfb_proc.terminate()
        try:
            self._xvfb_proc.wait(timeout=5)
        except subprocess.TimeoutExpired:
            self._xvfb_proc.kill()
            self._xvfb_proc.wait(timeout=5)

        lock = Path(f"/tmp/.X{self._display}-lock")
        if lock.exists():
            try:
                lock.unlink()
            except OSError:
                pass

    def _check_alive(self) -> None:
        if self._game_proc is not None and self._game_proc.poll() is not None:
            stderr = ""
            try:
                self._game_stderr_file.flush()
                stderr = self._game_stderr_path.read_text(errors="replace").strip()
                # Keep last 50 lines to avoid huge tracebacks
                lines = stderr.splitlines()
                if len(lines) > 50:
                    stderr = "\n".join(lines[-50:])
            except Exception:
                pass
            raise GameCrashedError(self._game_proc.returncode, stderr)

    def send(self, command: str, *, check: bool = True) -> CommandResult:
        self._check_alive()
        logger.debug("send: %s", command)
        try:
            proc = subprocess.run(
                ["dotnet", str(GAME_DLL), "--send", command, "--instance", self._pipe_id],
                capture_output=True,
                text=True,
                timeout=10,
            )
        except subprocess.TimeoutExpired:
            # Pipe timed out — check if game crashed
            self._check_alive()
            raise

        result = CommandResult(
            success=proc.returncode == 0,
            output=proc.stdout.strip(),
            errors=[line for line in proc.stderr.strip().splitlines() if line],
        )

        if not result.success:
            logger.warning("command failed: %s — %s", command, "; ".join(result.errors))
            # Check if failure is due to game crash
            self._check_alive()
            if check:
                raise CommandError(command, result)
        elif result.output:
            logger.debug("response: %s", result.output)

        return result

    def mark_log_position(self) -> None:
        """Mark the current end of the stderr log. Subsequent assert_no_errors/assert_no_warnings
        will only check lines after this mark."""
        self._game_stderr_file.flush()
        self._log_mark = self._game_stderr_path.stat().st_size

    def _read_log_since_mark(self) -> str:
        # SimpleConsole may buffer when stdout is redirected to a file;
        # give the runtime a moment to flush.
        time.sleep(0.5)
        mark = getattr(self, "_log_mark", 0)
        with open(self._game_stderr_path, "r", errors="replace") as f:
            f.seek(mark)
            return f.read()

    def assert_no_errors(self) -> None:
        """Assert that the game stderr log contains no 'fail:' lines since the last mark."""
        text = self._read_log_since_mark()
        fail_lines = [line for line in text.splitlines() if " fail: " in line]
        assert not fail_lines, (
            f"Game produced {len(fail_lines)} error(s):\n" + "\n".join(fail_lines)
        )

    def assert_no_warnings(self, ignore: list[str] | None = None) -> None:
        """Assert that the game stderr log contains no 'warn:' lines since the last mark."""
        text = self._read_log_since_mark()
        warn_lines = [line for line in text.splitlines() if " warn: " in line]
        if ignore:
            warn_lines = [l for l in warn_lines if not any(p in l for p in ignore)]
        assert not warn_lines, (
            f"Game produced {len(warn_lines)} warning(s):\n" + "\n".join(warn_lines)
        )

    # -- Typed command wrappers --

    def place_track(
        self,
        start: str,
        end: str,
        start_dir: str | None = None,
        end_dir: str | None = None,
    ) -> list[str]:
        parts = [f"place-track start={start} end={end}"]
        if start_dir:
            parts.append(f"start-dir={start_dir}")
        if end_dir:
            parts.append(f"end-dir={end_dir}")
        result = self.send(" ".join(parts))
        data = json.loads(result.output)
        return data["trackIds"]

    def place_train(
        self, track: str, speed: float | None = None
    ) -> str:
        cmd = f"place-train track={track}"
        if speed is not None:
            cmd += f" speed={speed}"
        result = self.send(cmd)
        data = json.loads(result.output)
        return data["trainId"]

    def place_building(
        self, pos: str, type: str, dir: str
    ) -> str:
        result = self.send(f"place-building pos={pos} type={type} dir={dir}")
        # Output format: "Placed <type> building: <id>"
        return result.output.rsplit(": ", 1)[-1]

    def query_building(self, building_id: str) -> dict:
        result = self.send(f"query-building building={building_id}")
        return json.loads(result.output)

    def screenshot(self, path: str | Path, *, target: str | None = None, debug: bool = False) -> Path:
        path = Path(path)
        path.parent.mkdir(parents=True, exist_ok=True)
        cmd = f"screenshot path={path}"
        if target is not None:
            cmd += f" target={target}"
        if debug:
            cmd += " debug=true"
        self.send(cmd)
        self.step(2)
        if not path.exists():
            raise FileNotFoundError(f"Screenshot not created: {path}")
        size = path.stat().st_size
        if size < 1000:
            raise ValueError(
                f"Screenshot too small ({size} bytes), likely corrupt: {path}"
            )
        return path

    def step(self, frames: int = 1) -> CommandResult:
        return self.send(f"step frames={frames}")

    def set_camera(self, target: str, zoom: float) -> CommandResult:
        return self.send(f"set-camera target={target} zoom={zoom}")

    def set_time(self, time: str) -> CommandResult:
        return self.send(f"set-time time={time}")

    def set_speed(self, scale: float) -> CommandResult:
        return self.send(f"set-speed scale={scale}")

    def query_time(self) -> dict:
        result = self.send("query-time")
        data = json.loads(result.output)
        return {
            "value": float(data["value"]),
            "hours": int(data["hours"]),
            "minutes": int(data["minutes"]),
            "timeScale": float(data["timeScale"]),
        }

    def activate_gui(self, id: str) -> CommandResult:
        return self.send(f"activate-gui id={id}")

    def query_gui(self, id: str) -> dict:
        result = self.send(f"query-gui id={id}")
        return json.loads(result.output)

    def select_tool(self, name: str) -> CommandResult:
        return self.send(f"select-tool name='{name}'")

    def set_mouse(self, x: float, y: float) -> CommandResult:
        return self.send(f"set-mouse pos={x},{y}")

    def load_scene(self, scene: str) -> CommandResult:
        return self.send(f"load-scene scene={scene}")

    def click(self, x: float, y: float) -> CommandResult:
        return self.send(f"click pos={x},{y}")

    # -- Query wrappers --

    def query_train(self, train_id: str) -> TrainState:
        result = self.send(f"query-train train={train_id}")
        data = json.loads(result.output)
        pos = data.get("position")
        return TrainState(
            train_id=data["trainId"],
            track_id=data["trackId"],
            time=data["time"],
            velocity=data["velocity"],
            position=(float(pos["x"]), float(pos["y"]), float(pos["z"])) if pos else None,
        )

    def query_train_cargo(self, train_id: str) -> dict[str, int]:
        """Query train and return aggregated cargo across all wagons as {cargoType: totalAmount}."""
        result = self.send(f"query-train train={train_id}")
        data = json.loads(result.output)
        totals: dict[str, int] = {}
        for w in data.get("wagons", []):
            for c in w.get("cargo", []):
                totals[c["cargoType"]] = totals.get(c["cargoType"], 0) + c["amount"]
        return totals

    def list_trains(self) -> list[TrainState]:
        result = self.send("list-trains")
        data = json.loads(result.output)
        return [
            TrainState(
                train_id=v["trainId"],
                track_id=v["trackId"],
                time=v["time"],
                velocity=v["velocity"],
            )
            for v in data["trains"]
        ]

    def list_junctions(self) -> list[JunctionInfo]:
        result = self.send("list-junctions")
        data = json.loads(result.output)
        return [
            JunctionInfo(
                junction_id=j["junctionId"],
                position=(j["position"]["x"], j["position"]["z"]),
                connection_count=j["connectionCount"],
                has_signal=j["hasSignal"],
            )
            for j in data["junctions"]
        ]

    def query_junction(self, junction_id: str) -> JunctionDetail:
        result = self.send(f"query-junction junction={junction_id}")
        data = json.loads(result.output)
        return JunctionDetail(
            junction_id=data["junctionId"],
            position=(data["position"]["x"], data["position"]["z"]),
            has_signal=data["hasSignal"],
            connections=[
                JunctionConnection(track_id=c["trackId"], direction=c["direction"])
                for c in data["connections"]
            ],
            rules=data["rules"],
        )

    def clear_signal_rules(self, junction_id: str) -> CommandResult:
        return self.send(f"clear-signal-rules junction={junction_id}")

    def add_signal_rule(self, junction_id: str, rule: str) -> CommandResult:
        return self.send(f"add-signal-rule junction={junction_id} rule='{rule}'")

    # -- Wagon wrappers --

    def list_wagons(self, train_id: str) -> list[WagonInfo]:
        result = self.send(f"list-wagons train={train_id}")
        data = json.loads(result.output)
        return [
            WagonInfo(
                wagon_id=w["wagonId"],
                blueprint_id=w["blueprintId"],
                inventory_id=w["inventoryId"],
            )
            for w in data["wagons"]
        ]

    def add_wagon(self, train_id: str, blueprint_id: str) -> str:
        result = self.send(f"add-wagon train={train_id} blueprint={blueprint_id}")
        data = json.loads(result.output)
        return data["wagonId"]

    def remove_wagon(self, train_id: str, index: int) -> CommandResult:
        return self.send(f"remove-wagon train={train_id} index={index}")

    # -- Collision / projection wrappers --

    @dataclass
    class RaycastHit:
        collider_id: str
        group: str
        distance: float

    def raycast(self, x: float, y: float) -> list[RaycastHit]:
        result = self.send(f"raycast pos={x},{y}")
        data = json.loads(result.output)
        return [
            Game.RaycastHit(
                collider_id=h["colliderId"],
                group=h["group"],
                distance=float(h["distance"]),
            )
            for h in data["hits"]
        ]

    def project_to_screen(self, x: float, y: float, z: float) -> tuple[float, float]:
        result = self.send(f"project-to-screen pos={x},{y},{z}")
        data = json.loads(result.output)
        return (float(data["x"]), float(data["y"]))

    # -- Internal helpers --

    def _build(self) -> None:
        if self.skip_build:
            return

        logger.info("Running asset pipeline")
        subprocess.run(
            ["dotnet", "run", "--project", str(ASSET_PIPELINE_PROJECT)],
            check=True,
        )

        logger.info("Building game (Release)")
        subprocess.run(
            ["dotnet", "build", str(GAME_PROJECT), "--configuration", "Release"],
            check=True,
        )

    def _kill_stale(self) -> None:
        if os.name == "nt":
            subprocess.run(
                ["taskkill", "/F", "/FI", "IMAGENAME eq dotnet.exe", "/FI", "WINDOWTITLE eq On Track*"],
                capture_output=True,
            )
        else:
            subprocess.run(
                ["pkill", "-f", "On Track To Grow"],
                capture_output=True,
            )
        time.sleep(1)

    def _start_xvfb(self) -> None:
        if self.windowing != "xvfb":
            return

        display = f":{self._display}"

        if os.name != "nt":
            subprocess.run(["pkill", "-f", f"Xvfb {display}"], capture_output=True)
        lock = Path(f"/tmp/.X{self._display}-lock")
        if lock.exists():
            lock.unlink()

        self._xvfb_proc = subprocess.Popen(
            ["Xvfb", display, "-screen", "0", f"{self.resolution}x24"],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )

        self._game_env = {**os.environ, "DISPLAY": display, "LIBGL_ALWAYS_SOFTWARE": "1"}
        time.sleep(2)

    def _launch_game(self) -> None:
        logger.info("Launching game (manual mode, scene=%s, pipe=%s)", self.scene or "default", self._pipe_id)
        cmd = [
            "dotnet",
            str(GAME_DLL),
            "--manual",
            "--listen",
            "--instance",
            self._pipe_id,
            "--resolution",
            self.resolution,
        ]
        if self.scene:
            cmd.extend(["--scene", self.scene])
        self._game_stderr_path = Path(self._temp_dir) / "game-stderr.log"
        self._game_stderr_file = open(self._game_stderr_path, "w")
        env = {**(self._game_env or os.environ), "Logging__File__LogLevel__Default": "Information"}
        self._game_proc = subprocess.Popen(
            cmd,
            stdout=self._game_stderr_file,
            stderr=subprocess.STDOUT,
            env=env,
        )

    def _wait_for_pipe(self, timeout: int = 10) -> None:
        logger.info("Waiting for game to start...")
        for i in range(timeout):
            result = self.send("echo message=ping", check=False)
            if result.success:
                logger.info("Game ready")
                return
            time.sleep(1)
        raise TimeoutError(f"Game did not start within {timeout} seconds")

    @property
    def temp_dir(self) -> Path:
        return Path(self._temp_dir)
