
using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;

namespace Olve.Engine3D.Camera.Cameras;

public class IsometricPerspectiveCamera(IsometricView view, PerspectiveProjection projection) : Camera<IsometricView, PerspectiveProjection>(view, projection);