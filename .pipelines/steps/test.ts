import { existsSync, readdirSync } from "node:fs";
import { implement, step, Source } from "@olivervea/pipelines";
import * as M from "../models";
import screenshotTest from "../scripts/screenshot-test.sh" with { type: "file" };
import { WORK } from "../lib/dotnet";

const DIFFS = "/work/screenshot-diffs";

export const test = implement(step({
  name: "test",
  description: "Runs the screenshot integration suite under xvfb against the published linux build. On failure its files hold the diff, actual and baseline PNGs.",
  image: "mcr.microsoft.com/dotnet/runtime-deps:10.0",
  input: { source: Source, release: M.Release },
  timeoutSeconds: 1800,
}), async ({ source, release }, ctx) => {
  const env = {
    TREE: await ctx.files.fetch(source.tree),
    LINUX_TGZ: await ctx.files.fetch(release.linux),
    WORK,
    DIFFS,
  };
  try {
    await ctx.sh(screenshotTest, { env });
  } catch (e) {
    if (existsSync(DIFFS) && readdirSync(DIFFS).length > 0) await ctx.output.dir(DIFFS, { name: "screenshot-diffs" });
    throw e;
  }
});
