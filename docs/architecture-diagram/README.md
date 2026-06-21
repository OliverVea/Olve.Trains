# Interactive Architecture Diagram

A self-contained, interactive map of the Olve.Trains codebase — **284 components / 488 relationships** spanning the engine, game, and tooling layers.

## Opening it

Just open `index.html` in any modern browser (no server needed — data is embedded):

```bash
xdg-open docs/architecture-diagram/index.html   # Linux
```

## How to read it (layered layout)

The diagram is a **map with two axes** (following the C4 "diagrams as maps" idea):

- **Vertical (Y) = level of abstraction / control.** Higher up = higher-level. Six labeled tier bands, top to bottom:
  - **T5 Application & Composition** — entrypoint, DI/scene bootstrap, assemblies
  - **T4 Orchestration & Lifecycle** — scenes, tool/command control, pipeline & CI
  - **T3 Feature & Domain Services** — game-logic, rendering, GUI, tools, commands (the bulk of the code)
  - **T2 Engine Managers & Stores** — render/asset managers, entity stores, systems
  - **T1 Data, Types & Contracts** — records, entities, interfaces, shaders, primitives
  - **T0 Infrastructure, GPU & External** — OpenGL, GL handles, formats, NuGet packages
  - Band height is proportional to how many components live in that tier. Most edges flow *downward* (higher-level code depends on lower-level code).
- **Horizontal (X) = subsystem.** Each subsystem is a column with a colored header: Solution Overview · Engine Core · GUI System · Rendering Pipeline · Game Logic · Presentation & Shell · Commands/Pipeline/CI.
- **Nodes** are services, managers, data types, scenes, shaders, commands, etc., colored by subsystem and sized by connection count.
- **Edges**: solid = internal structure within a subsystem; dashed = cross-subsystem "story" flows (a rendering service subscribing to a game-logic event, a command mutating a service, the asset pipeline generating engine types).

## Interactions

- **Pan**: drag the background (or one-finger drag on touch). **Zoom**: scroll, the `+`/`−` buttons, or **pinch** on touch. **Move a node**: drag it (pins it); double-click to un-pin.
- **Click / tap a node** → detail panel with: subsystem + tier, **fan-in / fan-out** and **instability** (`I = fan-out / (fan-in+fan-out)` — high = application/control level, low = foundational), summary, key source files, tags, and a clickable list of connections (jump to any neighbor).
- **Hover / select** highlights the node and its direct neighbors and dims the rest (neighborhood focus).
- **Search** (`/` to focus) matches labels, summaries, tags, and file paths, and frames the matches.
- **Filter** by clicking legend rows: **Abstraction tiers** (hide/show a whole level) and **Subsystems** (hide/show, or `solo` to isolate one) — the two filters compose.
- **Metrics overlay** (sidebar): switch node encoding between **Off**, **Hotspot** (size = code lines, color = complexity × churn), **Maintainability** (color = Maintainability Index), and **Coupling**. The detail panel always shows a node's **Roslyn code metrics** (MI, cyclomatic complexity, class coupling, inheritance depth, LOC, git commits, churn, hotspot). Grey nodes have no code (templates, external deps). Numbers come from `code-metrics.json` — see [CODE_METRICS.md](CODE_METRICS.md) for the full file/domain/module/overall report.
- **Bars** collapse for small screens (`☰` toggle). Buttons: **Reset view**, **Re-run layout**, zoom controls.

## Regenerating

The diagram is data-driven. `data.json` holds the graph; `index.html` embeds a copy of it inline (so the file works via `file://`).

The data was produced by exploring the codebase subsystem-by-subsystem and emitting JSON fragments (`nodes` + `edges` per subsystem), then merging them and adding curated cross-subsystem edges and subsystem display metadata.

To refresh after significant structural changes, re-run that mapping pass to rebuild `data.json`, then re-embed it into `index.html` by replacing the contents of the `<script id="graph-data">` block with the new `data.json`.

### Schema (`data.json`)

```jsonc
{
  "meta":       { "title", "generated", "summary" },
  "layers":     { "<layer>": { "label", "order" } },
  "subsystems": { "<id>": { "id", "label", "layer", "color", "summary" } },
  "tiers":      { "0".."5": { "label", "hint" } },          // Y-axis abstraction bands
  "nodes": [ { "id", "label", "type", "summary", "keyFiles":[], "tags":[],
              "subsystem", "layer", "degree", "tier": 0..5 } ],  // tier = Y position
  "edges": [ { "from", "to", "type", "label", "scope": "intra" | "cross" } ]
}
```

`tier` is assigned by `type` + a small keyword override map (registrations/scenes → high, OpenGL/external → low); fan-in/out and instability are derived in the renderer from the edge list. The X position comes from `subsystem`, the Y position from `tier`.
