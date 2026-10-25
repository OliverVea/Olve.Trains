import { implement } from "@olivervea/pipelines";
import { source } from "../pipeline";
import checkout from "../scripts/checkout.sh" with { type: "file" };

const DEST = "/work/src";

export default implement(source, async ({ repo, sha }, ctx) => {
  await ctx.sh(checkout, { env: { REPO: repo, SHA: sha, DEST } });
  return { repo, sha, tree: await ctx.output.dir(DEST, { name: "tree" }) };
});
