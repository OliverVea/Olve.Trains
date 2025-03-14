using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;

namespace Olve.Engine3D.Camera.Cameras;

public class IsometricOrthographicCamera(IsometricView view, OrthographicProjection projection) : Camera<IsometricView, OrthographicProjection>(view, projection);

public class IsometricPerspectiveCamera(IsometricView view, PerspectiveProjection projection) : Camera<IsometricView, PerspectiveProjection>(view, projection);