// The build happens on the container's own filesystem: /olve is a ~1 GiB emptyDir.
export const WORK = "/work/src";

export const dotnetEnv = (tree: string): Record<string, string> => ({
  TREE: tree,
  WORK,
  // Leftover MSBuild nodes and the compiler server would hold the step's output pipe open after dotnet exits.
  MSBUILDDISABLENODEREUSE: "1",
  UseSharedCompilation: "false",
  Build__OutputDirectory: `${WORK}/src/Olve.Trains/assets`,
  Build__BuildDirectory: `${WORK}/src/Olve.Trains.AssetPipeline/temp`,
  Asset__SourceDirectory: `${WORK}/src/Olve.Trains/resources/assets`,
  Shader__ShadersDirectory: `${WORK}/src/Olve.Trains/resources/shaders`,
  Layout__LayoutsDirectory: `${WORK}/src/Olve.Trains/resources/layouts`,
});
