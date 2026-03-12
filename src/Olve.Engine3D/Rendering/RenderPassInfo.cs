using Olve.Utilities.Ids;

namespace Olve.Engine3D.Rendering;

public record RenderPassInfo(Id PassId, Id FramebufferId, int Priority, ClearFlags Clear);
