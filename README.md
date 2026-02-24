

## Platform Support

| Platform | Status |
|---|---|
| Windows | Supported |
| Linux | Supported |
| macOS | Planned |
| Nintendo Switch | Planned |
| Xbox Series | Planned |
| PlayStation 5 | Planned |
| iOS / Android | Planned |

See [docs/platform-strategy.md](docs/platform-strategy.md) for priority analysis and porting notes.

## Requirements

- MSVC for AssImp in Olve.Trains.AssetPipeline


## Validation

An integration test script verifies the full pipeline end-to-end: asset compilation, build, headless game launch, entity placement, and screenshot capture.

```bash
# Full integration test (native window)
bash scripts/integration-test.sh --file ~/test-screenshots/test.png

# Full integration test (headless)
bash scripts/integration-test.sh --windowing xvfb --file ~/test-screenshots/test.png
```

This runs automatically in CI as the `validate-screenshot` job after the build completes. The screenshot is uploaded as a build artifact for visual inspection.


## TODO

See [TODO.md](TODO.md).