# Repo Notes
- Use conventional commit messages.
- Run `dotnet test` before committing.
- Update this file when repository structure or features change.
- Added VehicleService and tests.
- `Olve.Trains` project contains track and vehicle services used by tests.
- `Olve.Trains.Tests` runs standalone: use `dotnet run --project tests/Olve.Trains.Tests` when verifying tests since other test projects depend on missing assets.
