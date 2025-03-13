using Engine.Camera.Projections;
using Engine.Camera.Views;

namespace Engine.Camera.Cameras;

public class IsometricOrthographicCamera(IsometricView view, OrthographicProjection projection) : Camera<IsometricView, OrthographicProjection>(view, projection);

public class IsometricPerspectiveCamera(IsometricView view, PerspectiveProjection projection) : Camera<IsometricView, PerspectiveProjection>(view, projection);