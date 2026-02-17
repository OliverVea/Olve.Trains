# Platform Strategy

## Priority Ranking

| Priority | Platform | Status | Effort | Notes |
|---|---|---|---|---|
| 1 | Windows | Supported | — | Core audience (~70-80% of genre sales) |
| 2 | Linux | Supported | — | Small but loyal audience |
| 3 | macOS | Planned | Low | OpenGL via Silk.NET works; Metal ideal long-term |
| 4 | Nintendo Switch | Planned | High | Strong tycoon audience; needs NativeAOT + new graphics backend |
| 5 | Xbox Series | Planned | High | Best .NET console support (Microsoft ecosystem) |
| 6 | PlayStation 5 | Planned | High | Similar to Xbox but less .NET ecosystem support |
| 7 | iOS / Android | Planned | Very High | Requires full UI rework for touch; different market (F2P dominated) |

## Genre Context

The tycoon/simulation genre is heavily PC-dominant:

- **Cities: Skylines** sold 5M on PC before reaching 6M across all platforms — roughly 83% PC, 17% console.
- **Transport Fever 2** went from ~500K (PC-only) to 1M after console launch — a similar split.

## Technical Considerations

### C# on Consoles

C# games can target consoles via NativeAOT or BRUTE (IL-to-C++ transpiler). FNA and MonoGame have shipped hundreds of titles this way, including Stardew Valley, Celeste, and TowerFall. Alchemic Cutie was the first NativeAOT-based game to pass certification on Nintendo Switch.

### Graphics Backend

OpenGL is not available on consoles. A Vulkan, Metal, or platform-native graphics backend would be needed, which is the single biggest porting cost. The current stack uses OpenGL via Silk.NET.

### Console Certification

All console platforms (Switch, Xbox, PlayStation) require devkit access and certification, adding fixed overhead regardless of game complexity.

### Mobile

Mobile tycoon games exist but the UI paradigm is completely different (touch controls, screen size). The mobile tycoon market is dominated by F2P monetization. A mobile port would essentially be a separate product.

## Recommended Approach

1. **Ship on Steam (Windows/Linux)** first — this is where the audience lives.
2. **macOS** is low-hanging fruit given the current tech stack.
3. **Nintendo Switch** is the most compelling console target for this genre.
4. **Xbox/PlayStation** are worth pursuing if the game gains traction on PC.
5. **iOS/Android** only if there's appetite for a separate mobile-focused version.

Each additional platform beyond PC typically costs 20-40% of base development effort.
