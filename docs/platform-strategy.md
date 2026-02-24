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

### Graphics Backend — WebGPU Migration

OpenGL is not available on consoles, deprecated on macOS (stuck at 4.1), and lacks multi-threaded command recording. A modern graphics backend is the single biggest porting cost. The current stack uses OpenGL via Silk.NET.

**Recommendation: Migrate to WebGPU (native, via wgpu or Dawn).**

WebGPU is a modern graphics API designed by GPU vendors (Apple, Google, Mozilla, Microsoft) as the lowest common denominator of Vulkan, D3D12, and Metal. Native implementations (not browser-bound) exist today:

- **wgpu** (Rust-based, used by Firefox) — also works as a standalone native library
- **Dawn** (C++, used by Chrome) — also works standalone

The migration path preserves the current windowing setup:

```
Today:     Silk.NET.Windowing (GLFW) → Silk.NET.OpenGL → GPU
After:     Silk.NET.Windowing (GLFW) → Silk.NET.WebGPU (backed by wgpu/Dawn) → GPU
```

On each platform, WebGPU translates to the native API automatically:

| Platform | WebGPU backend |
|---|---|
| Windows | Vulkan or D3D12 |
| Linux | Vulkan |
| macOS/iOS | Metal (native, no translation layer) |
| Android | Vulkan |
| Web (browser) | Browser's WebGPU implementation |
| Xbox | D3D12 (in theory, untested) |

**Why WebGPU over raw Vulkan:**

- Vulkan alone doesn't cover Mac (requires MoltenVK translation layer) or Xbox (D3D12 only). WebGPU covers both natively.
- WebGPU's API complexity is moderate (between OpenGL and Vulkan). Vulkan requires ~800-1500 lines of boilerplate for a first triangle; WebGPU requires ~200.
- Performance overhead vs raw Vulkan is 5-15% for typical workloads — negligible for this project's rendering complexity.
- The project's rendering features (instanced draws, 2D textures, basic blend/depth state, no compute, no custom framebuffers) are well within WebGPU's capabilities.

**What WebGPU does NOT support (as of 2026):**

| Feature | Status | Impact |
|---|---|---|
| Tessellation shaders | No (spec deadlocked) | Not needed — use LOD or compute |
| Geometry shaders | No | Not needed — already removed from pipeline |
| Mesh shaders | No | Not needed |
| Ray tracing | No (proposal only) | Nice-to-have; can fake with screen-space techniques or add a Vulkan-specific backend later |
| Bindless textures | No (spec in progress) | Not needed at current material count |
| Sparse/virtual textures | No | Not needed |

None of these are blockers for this project, even at 5-10x current rendering complexity.

**Migration scope:**

| Layer | Effort |
|---|---|
| `Rendering/OpenGL/` classes (~12 files) | Full rewrite as `Rendering/WebGPU/` equivalents |
| `IVertexData`/`IInstanceData` interfaces | Redesign — `ConfigureAttributes(GL gl)` is OpenGL-specific; WebGPU declares vertex layout at pipeline creation |
| Uniform binding (`RenderingParameterHelper`) | Rewrite — `glUniform*` becomes bind groups/push constants |
| Asset pipeline (shader codegen) | Update — GLSL → WGSL, codegen templates need rework |
| Game-level rendering services | Minimal changes — already decoupled from GL |
| Windowing, input, math | No changes — Silk.NET shared across backends |

**Shaders:** Current shaders are GLSL `#version 330 core`, compiled at runtime. WebGPU uses WGSL (different syntax, same concepts). The asset pipeline would need to either compile GLSL → WGSL or rewrite shaders in WGSL directly.

### Web Distribution

A browser-based web demo is possible in the future but depends on the .NET → WASM toolchain maturing:

- **Silk.NET + Blazor WebAssembly + WebGPU** is technically possible but experimental (multiple false starts, large bundle sizes, GC pauses causing frame hitches).
- **Rust/C++ → WASM + WebGPU** works well today but requires rewriting the rendering layer in another language.
- The native WebGPU API is identical in browser and native contexts, so the rendering code is portable — only the windowing/hosting differs.

For now, the demo ships as a native download. A web version can be revisited when the .NET WASM pipeline matures.

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
