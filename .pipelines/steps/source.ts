import { implement, step, Source, z } from "@olivervea/pipelines";
import { GITHUB_TOKEN } from "../shared";
import checkout from "../scripts/checkout.sh" with { type: "file" };

const DEST = "/work/src";

export const source = implement(step({
  name: "source",
  description: "Clones the pushed commit and pulls its git LFS files (the source art and reference screenshots).",
  image: "debian:stable-slim",
  input: { repo: z.string(), sha: z.string() },
  output: Source,
  secrets: [GITHUB_TOKEN],
  timeoutSeconds: 600,
}), async ({ repo, sha }, ctx) => {
  await ctx.sh(checkout, { env: { REPO: repo, SHA: sha, DEST } });
  return { repo, sha, tree: await ctx.output.dir(DEST, { name: "tree" }) };
});
