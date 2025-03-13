using Engine.Camera.Projections;
using Engine.Camera.Views;

namespace Engine.Camera.Cameras;

public class PerspectiveCamera(FirstPersonView view, PerspectiveProjection projection) : Camera<FirstPersonView, PerspectiveProjection>(view, projection);