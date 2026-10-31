import { implement, step } from "@olivervea/pipelines";
import { ITCH_API_KEY } from "../shared";
import * as M from "../models";
import pushItch from "../scripts/push-itch.sh" with { type: "file" };

export const publishItch = implement(step({
  name: "publish-itch",
  description: "Pushes the linux and windows builds to itch.io with butler.",
  image: "debian:stable-slim",
  input: { release: M.Release },
  secrets: [ITCH_API_KEY],
  timeoutSeconds: 900,
}), async ({ release }, ctx) => {
  await ctx.sh(pushItch, {
    env: {
      VERSION: release.version,
      LINUX_TGZ: await ctx.files.fetch(release.linux),
      WIN_ZIP: await ctx.files.fetch(release.windows),
      ITCH_TARGET: "cookiscuit/on-track-to-grow",
    },
  });
});
