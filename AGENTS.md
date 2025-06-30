# Repository Overview

This repository contains a 3D game named **Olve.Trains** and related tooling.

## Structure

- `src/Olve.Trains` – main game written in C# using Silk.NET.
- `src/Olve.Engine3D` – reusable engine code.
- `src/Olve.Trains.AssetPipeline` – asset pipeline project with a small README.
- `tests` – C# test projects for the engine and asset pipeline.
- `infrastructure/minio` – Docker Compose and Python utilities for running a MinIO server. Python code requires version 3.12 and uses a `pyproject.toml` for dependencies.
- `docs` – design documents and SVG diagrams (terrain etc.).

The solution file `Olve.Trains.sln` references the main projects and tests.

## Build and Test

- Build all projects:

  ```bash
  dotnet build Olve.Trains.sln
  ```

- Run all tests:

  ```bash
  dotnet test Olve.Trains.sln
  ```

Python utilities in `infrastructure/minio` can be installed with `pip install -e .` and executed with `python main.py`.

## Notes for LLM Agents

Keep this file updated whenever repository structure or build instructions change. Use it to understand where major components live.
