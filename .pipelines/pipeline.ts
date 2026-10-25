// Every push builds, tests and publishes to itch.io. config.yaml beside it is the old v1 config.
import { pipeline, secret, step, Source, z } from "@olivervea/pipelines";
import * as M from "./models";

export const GITHUB_TOKEN = secret("GITHUB_TOKEN", "GitHub read token for the clone (raises the rate limit).");
export const ITCH_API_KEY = secret("ITCH_API_KEY", "butler API key for pushing builds to itch.io.");

export const source = step({
  name: "source",
  description: "Clones the pushed commit and pulls its git LFS files (the source art and reference screenshots).",
  image: "debian:stable-slim",
  input: { repo: z.string(), sha: z.string() },
  output: Source,
  secrets: [GITHUB_TOKEN],
  timeoutSeconds: 600,
});

export const build = step({
  name: "build",
  description: "Runs the asset pipeline and publishes self-contained linux-x64 (tar.gz) and win-x64 (zip) builds.",
  image: "mcr.microsoft.com/dotnet/sdk:10.0",
  input: { source: Source },
  output: M.Release,
  timeoutSeconds: 1800,
});

export const test = step({
  name: "test",
  description: "Runs the screenshot integration suite under xvfb against the published linux build. On failure its files hold the diff, actual and baseline PNGs.",
  image: "mcr.microsoft.com/dotnet/runtime-deps:10.0",
  input: { source: Source, release: M.Release },
  timeoutSeconds: 1800,
});

export const publishItch = step({
  name: "publish-itch",
  description: "Pushes the linux and windows builds to itch.io with butler.",
  image: "debian:stable-slim",
  input: { release: M.Release },
  secrets: [ITCH_API_KEY],
  timeoutSeconds: 900,
});

export default pipeline({
  name: "Olve.Trains",
  description: "Build On Track To Grow for Linux and Windows, run the screenshot tests, then publish to itch.io. publish=false stops before itch.io.",
  input: M.DeployRequest,
})
  .onPush(e => ({ repo: e.repository.full_name, sha: e.after, publish: true }))
  .stage([source, from => ({ repo: from.input.repo, sha: from.input.sha })])
  .stage(build)
  .stage(test)
  .when({ publish: true }, p => p.stage(publishItch));
