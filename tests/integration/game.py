from __future__ import annotations

import logging
import os
import re
import signal
import subprocess
import tempfile
import time
from dataclasses import dataclass, field
from pathlib import Path

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

    def start(self) -> None:
        self._build()
        if self.kill_stale:
            self._kill_stale()
        self._start_xvfb()
        self._launch_game()
        self._wait_for_pipe()

    def stop(self) -> None:
        try:
            self.send("exit", check=False)
        except Exception:
            pass

        if self._game_proc and self._game_proc.poll() is None:
            self._game_proc.terminate()
            try:
                self._game_proc.wait(timeout=5)
            except subprocess.TimeoutExpired:
                self._game_proc.kill()

        if self._xvfb_proc and self._xvfb_proc.poll() is None:
            self._xvfb_proc.terminate()
            try:
                lock = Path(f"/tmp/.X{self._display}-lock")
                if lock.exists():
                    lock.unlink()
            except Exception:
                pass

    def send(self, command: str, *, check: bool = True) -> CommandResult:
        logger.debug("send: %s", command)
        proc = subprocess.run(
            ["dotnet", str(GAME_DLL), "--send", command, "--instance", self.instance_id],
            capture_output=True,
            text=True,
            timeout=30,
        )

        result = CommandResult(
            success=proc.returncode == 0,
            output=proc.stdout.strip(),
            errors=[line for line in proc.stderr.strip().splitlines() if line],
        )

        if not result.success:
            logger.warning("command failed: %s — %s", command, "; ".join(result.errors))
            if check:
                raise CommandError(command, result)
        elif result.output:
            logger.debug("response: %s", result.output)

        return result

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
        return UUID_RE.findall(result.output)

    def place_vehicle(
        self, track: str, speed: float | None = None
    ) -> CommandResult:
        cmd = f"place-vehicle track={track}"
        if speed is not None:
            cmd += f" speed={speed}"
        return self.send(cmd)

    def place_building(
        self, pos: str, type: str, dir: str
    ) -> CommandResult:
        return self.send(f"place-building pos={pos} type={type} dir={dir}")

    def screenshot(self, path: str | Path) -> Path:
        path = Path(path)
        path.parent.mkdir(parents=True, exist_ok=True)
        self.send(f"screenshot path={path}")
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

    def activate_gui(self, id: str) -> CommandResult:
        return self.send(f"activate-gui id={id}")

    def select_tool(self, name: str) -> CommandResult:
        return self.send(f"select-tool name='{name}'")

    def set_mouse(self, x: float, y: float) -> CommandResult:
        return self.send(f"set-mouse pos={x},{y}")

    def load_scene(self, scene: str) -> CommandResult:
        return self.send(f"load-scene scene={scene}")

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
        logger.info("Launching game (manual mode, scene=%s)", self.scene or "default")
        cmd = [
            "dotnet",
            str(GAME_DLL),
            "--manual",
            "--listen",
            "--instance",
            self.instance_id,
            "--resolution",
            self.resolution,
        ]
        if self.scene:
            cmd.extend(["--scene", self.scene])
        self._game_proc = subprocess.Popen(
            cmd,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
            env=self._game_env,
        )

    def _wait_for_pipe(self, timeout: int = 30) -> None:
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
