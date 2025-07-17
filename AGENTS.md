# Repo Notes
- Use conventional commit messages.
- Run `dotnet test` before committing.
- Update this file when repository structure or features change.
- Added VehicleService and tests.
- `Olve.Trains.GameLogic` library contains track and vehicle services.
- `Olve.Trains.Tests` references this library. Run `dotnet run --project tests/Olve.Trains.Tests` when verifying tests since other test projects depend on missing assets.
