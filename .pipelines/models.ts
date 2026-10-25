import { fileRef, model, z } from "@olivervea/pipelines";

export const DeployRequest = model("DeployRequest", z.object({
  repo: z.string().default("OliverVea/Olve.Trains"),
  sha: z.string().regex(/^[0-9a-f]{40}$/),
  publish: z.boolean().default(true),
}));

/** Self-contained linux-x64 and win-x64 builds of one commit. */
export const Release = model("Release", z.object({ version: z.string(), linux: fileRef(), windows: fileRef() }));
