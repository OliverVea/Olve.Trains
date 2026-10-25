import { implement } from "@olivervea/pipelines";
import { build } from "../pipeline";
import buildRelease from "../scripts/build-release.sh" with { type: "file" };
import { dotnetEnv } from "./lib/dotnet";

const OUT = "/work/out";

export default implement(build, async ({ source }, ctx) => {
  const tree = await ctx.files.fetch(source.tree);
  await ctx.sh(buildRelease, { env: { ...dotnetEnv(tree), SHA: source.sha, OUT } });
  const version = (await ctx.readText(`${OUT}/version`)).trim();
  return {
    version,
    linux: await ctx.output.file(`${OUT}/linux-x64.tar.gz`, { name: `olve-trains-${version}-linux-x64.tar.gz` }),
    windows: await ctx.output.file(`${OUT}/win-x64.zip`, { name: `olve-trains-${version}-win-x64.zip` }),
  };
});
