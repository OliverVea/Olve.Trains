using Olve.Engine3D.Camera.Projections;
using Olve.Engine3D.Camera.Views;

namespace Olve.Engine3D.Camera.Cameras;

public class PerspectiveCamera(FirstPersonView view, PerspectiveProjection projection) : Camera<FirstPersonView, PerspectiveProjection>(view, projection);