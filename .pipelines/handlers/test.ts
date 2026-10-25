import { existsSync, readdirSync } from "node:fs";
import { implement } from "@olivervea/pipelines";
import { test } from "../pipeline";
import screenshotTest from "../scripts/screenshot-test.sh" with { type: "file" };
import { WORK } from "./lib/dotnet";

const DIFFS = "/work/screenshot-diffs";

export default implement(test, async ({ source, release }, ctx) => {
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
