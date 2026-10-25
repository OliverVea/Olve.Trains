import { implement } from "@olivervea/pipelines";
import { publishItch } from "../pipeline";
import pushItch from "../scripts/push-itch.sh" with { type: "file" };

export default implement(publishItch, async ({ release }, ctx) => {
  await ctx.sh(pushItch, {
    env: {
      VERSION: release.version,
      LINUX_TGZ: await ctx.files.fetch(release.linux),
      WIN_ZIP: await ctx.files.fetch(release.windows),
      ITCH_TARGET: "cookiscuit/on-track-to-grow",
    },
  });
});
