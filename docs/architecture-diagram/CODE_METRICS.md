# Code Metrics — Olve.Trains (Roslyn-certified)

_Computed with the **Microsoft.CodeAnalysis.Metrics** engine (`CodeAnalysisMetricData.ComputeAsync`, the same computation as Visual Studio's *Calculate Code Metrics*), run cross-platform via its public API over the real compilations. Maintainability Index 0–100 (≥20 good · 10–19 moderate · <10 low). Cyclomatic complexity & class coupling are IOperation-based. Churn from 508 commits of git history; **Hotspot = cyclomatic complexity × ln(1+commits)**. 722 types across 605 authored files._

## Overall

- **Maintainability Index (avg per file): 85** — distribution: **605 good** · 0 moderate · **0 low**
- **Types:** 722  ·  **Files:** 605  ·  **Source lines:** 31,889
- **Cyclomatic complexity:** 6,475 total  ·  **Max depth of inheritance:** 2  ·  **Max class coupling:** 71
- **Churn:** 53,482 lines over 605 touched files

## By module (assembly-level, straight from the metrics engine)

| Module | MI | Types | Source | Cyclo | Coupling | Churn | Hotspot |
|---|--:|--:|--:|--:|--:|--:|--:|
| Olve.Trains | **82** | 277 | 15,093 | 3,254 | 692 | 25,472 | 4177 |
| Olve.Engine3D | **87** | 376 | 14,040 | 3,260 | 511 | 22,772 | 4837 |
| Olve.Trains.AssetPipeline | **84** | 69 | 2,756 | 490 | 234 | 5,238 | 912 |

## By domain (top 22 by source size · MI = avg per file)

| Domain | MI | Files | Source | Cyclo | Cpl(max) | Commits | Hotspot |
|---|--:|--:|--:|--:|--:|--:|--:|
| Engine3D / GUI | 90 | 80 | 4,265 | 1,179 | 55 | 236 | 2028 |
| Trains / Scenes/GameUI | 79 | 29 | 3,356 | 561 | 66 | 192 | 988 |
| Trains / Scenes/GameRendering | 74 | 18 | 2,796 | 384 | 66 | 143 | 724 |
| Engine3D / Rendering | 89 | 71 | 2,640 | 528 | 43 | 184 | 737 |
| Trains / Commands | 79 | 34 | 1,712 | 448 | 47 | 72 | 478 |
| Trains / Shared | 73 | 14 | 1,410 | 195 | 71 | 48 | 307 |
| Engine3D / Assets | 87 | 20 | 1,235 | 258 | 22 | 37 | 247 |
| Trains.AssetPipeline / Assets | 75 | 15 | 1,081 | 147 | 43 | 108 | 296 |
| Engine3D / Commands | 80 | 21 | 1,008 | 212 | 29 | 35 | 199 |
| Trains / GameLogic/Trains | 86 | 26 | 958 | 211 | 37 | 74 | 310 |
| Trains / GameLogic/Buildings | 89 | 30 | 912 | 174 | 39 | 69 | 262 |
| Engine3D / Scenes | 89 | 13 | 871 | 163 | 41 | 46 | 364 |
| Engine3D / (root) | 87 | 18 | 868 | 140 | 39 | 71 | 278 |
| Engine3D / Math | 80 | 25 | 850 | 189 | 15 | 45 | 223 |
| Trains.AssetPipeline / Shaders | 91 | 9 | 819 | 139 | 46 | 66 | 318 |
| Trains / GameLogic/Tracks | 80 | 13 | 729 | 132 | 32 | 46 | 226 |
| Trains / GameLogic/Junctions | 86 | 15 | 684 | 142 | 37 | 46 | 229 |
| Engine3D / Input | 85 | 9 | 458 | 118 | 15 | 34 | 165 |
| Trains / GameLogic/Cargo | 89 | 15 | 403 | 111 | 26 | 28 | 146 |
| Trains / (root) | 74 | 4 | 392 | 54 | 63 | 67 | 140 |
| Engine3D / Physics3D | 85 | 10 | 389 | 65 | 29 | 23 | 106 |
| Engine3D / Camera | 87 | 17 | 388 | 105 | 19 | 49 | 153 |

## Top 15 hotspots — cyclomatic × churn (best refactor ROI)

| File | MI | Cyclo | Cpl | Commits | Hotspot |
|---|--:|--:|--:|--:|--:|
| `Olve.Engine3D/GUI/Layout/GuiLayoutService.cs` | 56 | 165 | 55 | 19 | 494 |
| `Olve.Engine3D/GUI/Elements/Box.cs` | 86 | 105 | 16 | 8 | 231 |
| `Olve.Engine3D/Scenes/SceneManager.cs` | 76 | 86 | 41 | 12 | 221 |
| `Olve.Trains.AssetPipeline/Shaders/ProcessShaders.cs` | 84 | 40 | 46 | 21 | 124 |
| `Olve.Trains/Scenes/GameUI/GUI/DepotPanelService.cs` | 89 | 66 | 66 | 5 | 118 |
| `Olve.Trains/Program.cs` | 78 | 31 | 63 | 39 | 114 |
| `Olve.Engine3D/Rendering/FramebufferManager.cs` | 93 | 80 | 39 | 3 | 111 |
| `Olve.Engine3D/GUI/GuiNodeService.cs` | 69 | 53 | 37 | 7 | 110 |
| `Olve.Engine3D/GameManager.cs` | 70 | 33 | 32 | 26 | 109 |
| `Olve.Trains/Scenes/GameRendering/ShadowMapService.cs` | 80 | 60 | 66 | 5 | 108 |
| `Olve.Engine3D/GUI/Elements/GuiDropdownService.cs` | 87 | 65 | 44 | 4 | 105 |
| `Olve.Engine3D/GUI/Elements/Text.cs` | 88 | 40 | 15 | 12 | 103 |
| `Olve.Trains/Shared/GUI/GuiTextRenderingService.cs` | 86 | 56 | 71 | 5 | 100 |
| `Olve.Trains.AssetPipeline/Shaders/ShaderHelper.cs` | 48 | 41 | 18 | 10 | 98 |
| `Olve.Engine3D/ScreenshotManager.cs` | 89 | 45 | 39 | 7 | 94 |

## Lowest 15 maintainability index (most strained files)

| File | MI | Cyclo | Cpl | DIT | Source |
|---|--:|--:|--:|--:|--:|
| `Olve.Trains/Scenes/GameLogic/GameLogicSceneServiceRegistration.cs` | **24** | 1 | 43 | 1 | 242 |
| `Olve.Trains.AssetPipeline/Program.cs` | **30** | 8 | 52 | 1 | 104 |
| `Olve.Engine3D/Commands/CommandLineParser.cs` | **38** | 30 | 10 | 1 | 84 |
| `Olve.Trains/GameServiceRegistration.cs` | **41** | 4 | 37 | 1 | 92 |
| `Olve.Trains.AssetPipeline/Shaders/ShaderHelper.cs` | **48** | 41 | 18 | 1 | 276 |
| `Olve.Trains.AssetPipeline/Assets/MeshNormalizer.cs` | **49** | 5 | 4 | 1 | 48 |
| `Olve.Trains.AssetPipeline/Assets/MeshFileReader.cs` | **50** | 9 | 27 | 1 | 93 |
| `Olve.Engine3D/Camera/CameraRayExtensions.cs` | **51** | 5 | 19 | 1 | 73 |
| `Olve.Engine3D/Commands/CommandPipeClient.cs` | **53** | 2 | 18 | 1 | 42 |
| `Olve.Trains/Scenes/GameUI/UISceneServiceRegistration.cs` | **53** | 1 | 7 | 1 | 42 |
| `Olve.Engine3D/Physics3D/Collisions/HeightmapRaycaster.cs` | **54** | 14 | 9 | 1 | 81 |
| `Olve.Trains/Scenes/GameLogic/Cargo/StationCargoTransferService.cs` | **54** | 26 | 26 | 1 | 109 |
| `Olve.Trains/Scenes/GameRendering/GameRenderingSceneServiceRegistration.cs` | **54** | 1 | 26 | 1 | 82 |
| `Olve.Engine3D/Rendering/RenderingManager.cs` | **55** | 22 | 43 | 1 | 154 |
| `Olve.Trains/Scenes/GameRendering/MeshDataMarshalHelper.cs` | **55** | 3 | 6 | 1 | 28 |

## Top 12 class coupling (most entangled)

| File | Cpl | MI | Cyclo | Source |
|---|--:|--:|--:|--:|
| `Olve.Trains/Shared/GUI/GuiTextRenderingService.cs` | 71 | 86 | 56 | 426 |
| `Olve.Trains/Shared/GUI/GuiRectangleRenderingService.cs` | 68 | 88 | 35 | 249 |
| `Olve.Trains/Scenes/GameRendering/ShadowMapService.cs` | 66 | 80 | 60 | 480 |
| `Olve.Trains/Scenes/GameUI/GUI/DepotPanelService.cs` | 66 | 89 | 66 | 358 |
| `Olve.Trains/Program.cs` | 63 | 78 | 31 | 213 |
| `Olve.Trains/Scenes/GameUI/GUI/IndustryInfoPanelService.cs` | 61 | 84 | 49 | 253 |
| `Olve.Trains/Scenes/GameRendering/MeshRenderingService.cs` | 60 | 89 | 36 | 239 |
| `Olve.Trains/Scenes/GameRendering/ColliderDebugRenderingService.cs` | 57 | 84 | 42 | 326 |
| `Olve.Trains/Scenes/GameUI/GUI/StationInfoPanelService.cs` | 57 | 84 | 46 | 242 |
| `Olve.Engine3D/GUI/Layout/GuiLayoutService.cs` | 55 | 56 | 165 | 932 |
| `Olve.Trains/Scenes/GameUI/Indicators/TrackArrowIndicatorService.cs` | 55 | 67 | 25 | 181 |
| `Olve.Trains/Scenes/GameUI/GUI/SignalRulesPanelService.cs` | 54 | 68 | 34 | 185 |
