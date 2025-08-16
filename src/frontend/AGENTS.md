# Front-end Guidelines

This directory contains the Svelte + TypeScript web application bundled with **Vite**.
All build scripts assume you run them from the repository root using **bun**.

## Layout

- `src/` – TypeScript and `.svelte` source files.
- `src/api/` – API resource specifications shared with the front-end.
- `public/` – static assets including `index.html` and `style.css`.
- `public/dist/` – bundled output from Vite.
- `index.html` – entry HTML file, loads `src/main.ts` via Vite.
- `public/style.css` – global styles used by `index.html`.
- `svelte.config.js` and `vite.config.ts` – build configuration.
- `tsconfig.json` – TypeScript configuration with strict mode enabled.
- `@microsoft/kiota` is installed at the repo root for generating a TypeScript
  API client when needed.
- `src/generated/api/` – Kiota generated TypeScript client from the backend
  spec. Run `bun run apigen` at the repository root to update these files.
- `src/api/client.ts` initializes the generated client and sets the backend
  base URL. This client is the only mechanism for calling the server API.
- `src/App.svelte` exposes an optional `initialLogs` prop so tests can inject
  starting log data without hitting the network.
- `src/services/healthService.ts` defines `checkHealth()` which calls the
  `/health` endpoint.

## Coding style
- Prefer modern ES6 syntax.
- Use explicit types where possible.
- Keep TypeScript `strict` mode enabled.
- UI code uses Svelte components written in `.svelte` files.

### API resource specs
- Files in `src/api/` define TypeScript interfaces mirroring backend C# records.
- The `ApiError` structure describes failed operations.
- A `Success` marker response is used for endpoints without body data.
- `LogMessage` maps to the server's `LogMessage` struct. Enum `LogLevel` is numeric (0 = Debug, 1 = Info, …).
- All exported interfaces and enums in `src/api/` include documentation comments for clarity.

## Development
 - Install dependencies with `bun install` from the repository root or run
   `./scripts/setup-bun.sh` to install Bun and install packages in one step.
 - Start the dev server with `bun run dev`.
 - Build once with `bun run build`.
 - Run `bun run lint` to lint front-end sources and config files via **Biome**.

## Testing
- Tests are written with **Vitest** using the `jsdom` environment.
- Mock Service Worker (MSW) intercepts `fetch` calls; handlers live in `tests/handlers.ts`.
- Fixtures reside in `tests/fixtures/` and are loaded by MSW.
- Run tests with `bun run test`. No backend services are required.
- Sample tests can be found under `tests/` and use Testing Library helpers.
 - End-to-end specs live in `e2e-tests/` at the repo root and run with `bun run e2e`.

Always update this `AGENTS.md` when the front-end structure or tooling changes.
