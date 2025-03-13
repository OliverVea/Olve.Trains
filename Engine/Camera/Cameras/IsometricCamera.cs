using Engine.Camera.Projections;
using Engine.Camera.Views;

namespace Engine.Camera.Cameras;

public class IsometricCamera(IsometricView view, OrthographicProjection projection) : Camera<IsometricView, OrthographicProjection>(view, projection);