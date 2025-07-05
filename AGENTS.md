# AGENTS Instructions

This file tracks automation-related instructions for the repository.

- When adding scripts or configuration files, briefly describe them here.
- Keep this file up to date with all changes.

## Scripts

### `backend/setup-dotnet.sh`
This script installs the .NET SDK (version 9.0), restores packages for `backend/backend.csproj`, then builds and tests the project. It can be used to prepare the container for offline development.
