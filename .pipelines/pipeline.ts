// Every push builds, tests and publishes to itch.io.
import { pipeline } from "@olivervea/pipelines";
import * as M from "./models";
import { build } from "./steps/build";
import { publishItch } from "./steps/publish-itch";
import { source } from "./steps/source";
import { test } from "./steps/test";

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
