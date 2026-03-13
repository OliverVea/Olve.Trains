using Olve.Utilities.Ids;
using Silk.NET.Maths;

namespace Olve.Engine3D.Rendering;

public record RenderPassInfo(Id PassId, Id FramebufferId, int Priority, ClearFlags Clear, Vector4D<float>? ClearColor = null);
